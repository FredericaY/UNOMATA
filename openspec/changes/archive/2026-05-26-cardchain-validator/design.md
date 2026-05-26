## Context

本 change 是 `Phase 1 — Core 层开发`第二步，对应 `Docs/TODO.md` 中 Change 2 (`cardchain-validator`)。在 Change 1 已落地的 `CardData` / 枚举之上，把"接龙合法性判定"和"会话状态更新"作为纯函数实现，是后续 `HackSession` 的语义内核。

约束：
- 必须严守 `Docs/INTERFACE.md` 第二节"`CardData`"段中"合法性判定（`HackSession` 内部，不暴露为 CardData 方法）"伪代码——它就是本 change 的"目标实现"
- 必须能完整重放 `Docs/GAME_DESIGN.md` 3.5.3 节的边界示例
- 严守 Core 项目零依赖：不引入 `System.Numerics`、`System.Linq` 之外的命名空间（`Linq` 仅测试代码可用）
- 严守 `Nullable=enable`：所有参数非空契约用编译期保证而非运行时 `null` 检查

## Goals / Non-Goals

**Goals:**
- 把 GAME_DESIGN 3.5 节的所有匹配规则用代码精确表达（**严格 ±1 升降序**版本）
- 把所有边界（开局/王牌后 lastColor=null 任意合法、反转后 lastColor!=null+lastNumber=null 异色全非法、连续两张同色 Reverse 合法、升序 9/降序 0 边界、Empty 防御）测到位
- 给后续 `HackSession`（Change 4）一个"傻瓜级"接口：`if (CardChainRules.IsValidNext(...)) CardChainRules.ApplyPrev(...);`
- 保证纯函数：无 IO、无静态可变状态、无随机源

**Non-Goals:**
- 不实现 `HackSession`（属于 Change 4）
- 不实现选项生成（属于 Change 3）
- 不实现 `OnDirectionChanged` 事件（属于 Change 5）——本 change 只做状态切换的纯函数
- 不实现 maxPot/latch/overflow（属于 Change 5）
- 不暴露任何 `public` API（除非 Change 4 真用到才升级，目前全部 `internal`）

## Decisions

### D1：`CardChainRules` 用 `static class` 而非依赖注入

**选择**：`internal static class CardChainRules { ... }`，两个公开静态方法 `IsValidNext` / `ApplyPrev`。

**理由**：
- 两个方法都是纯函数（输入 → 输出 / 输入 → 副作用到形参 state），没有"实例状态"
- 把它们装进类等于多一层 `new Validator()` 的样板代码，不划算
- C# 对 `static class` 编译期保证不能 `new`、不能继承，与"纯函数"语义最匹配
- 测试时直接 `CardChainRules.IsValidNext(...)`，简洁

**替代方案**：
- `interface IValidator` + `Validator : IValidator`：依赖注入风格，但现阶段没有"换实现"的需求；YAGNI
- 把方法挂在 `SessionState` 上：违反"一个类一职责"；`SessionState` 是数据载体不是规则引擎

### D2：`SessionState` 用 `internal class` + 可变字段

**选择**：

```csharp
internal sealed class SessionState
{
    public CardColor? LastColor;
    public int? LastNumber;
    public ChainDirection Direction = ChainDirection.Ascending;
}
```

**理由**：
- `ApplyPrev` 设计为原地修改 state（`out` 参数 / 返回新 struct 都会让上层 `HackSession` 写起来啰嗦）
- `internal` 不暴露给 Unity 端：会话状态是 Core 私有，外部只能通过 `HackSession` 的查询属性窥视（`CurrentDirection` 等）
- `sealed` 防扩展，避免 `class StateWithExtras : SessionState` 之类的诡异继承
- `Direction` 字段初始化器即"开局 Ascending"约定的代码层固化

**替代方案**：
- `record struct`：值类型 + 不可变。但 `ApplyPrev` 要么改签名为 `out` 要么返回新值，调用方麻烦
- 公开为 `public`：违反"Core 内部状态不外漏"；外部需要的只有 `Direction` 等查询属性，由 `HackSession` 投影出来即可

### D3：`ApplyPrev` 原地修改 + `Empty` 抛异常

**选择**：

```csharp
internal static void ApplyPrev(CardData prev, SessionState state)
{
    switch (prev.Type)
    {
        case CardType.Number:  ...; break;
        case CardType.Reverse: ...; break;
        case CardType.Wild:    ...; break;
        case CardType.Empty:
            throw new InvalidOperationException(
                "Empty card must not be applied as prev.");
        default:
            throw new ArgumentOutOfRangeException(nameof(prev));
    }
}
```

**理由**：
- `Empty` 是 `HackSession.CurrentCard` 的开局占位，玩家**永不可能**选中它（Change 3 发牌算法保证 `Empty` 不进选项；Change 4 `HackSession.SelectOption` 路径只接"玩家选的选项"）
- 出现 `ApplyPrev(Empty, ...)` 调用 = 上层 bug，必须立刻暴露
- `InvalidOperationException` 比 `ArgumentException` 更准确（"在错误的状态/上下文调用"）
- `default` 分支兜底未来加 `CardType` 时编译告警 + 运行时崩溃，双保险

**替代方案**：
- `Empty` 静默返回不修改 state：bug 不可见，反对
- `Debug.Assert`：项目用 `TreatWarningsAsErrors=true`，但 `Debug.Assert` 在 Release 下消失，反而隐藏问题

### D4：`IsValidNext` 用 `switch` 表达式而非长 `if-else`

**选择**：

```csharp
internal static bool IsValidNext(CardData next, SessionState state)
{
    return next.Type switch
    {
        CardType.Wild    => true,
        CardType.Empty   => false,
        CardType.Reverse => state.LastColor is null
                            || next.Color == state.LastColor,
        CardType.Number  => IsValidNumber(next, state),
        _ => throw new ArgumentOutOfRangeException(nameof(next)),
    };
}

private static bool IsValidNumber(CardData next, SessionState state)
{
    if (state.LastColor is null)        return true;     // 开局 / 王牌后任意合法
    if (next.Color == state.LastColor)  return true;     // 同色任意数字合法
    if (state.LastNumber is null)       return false;    // 反转后异色全非法
    return state.Direction == ChainDirection.Ascending
        ? next.Number == state.LastNumber + 1            // 严格 +1
        : next.Number == state.LastNumber - 1;           // 严格 -1
}
```

**理由**：
- `switch` 表达式让"按 Type 分支"在视觉上一目了然
- `IsValidNumber` 抽出来是因为数字判定的四层条件本身有结构，扁平进 switch 反而读不顺
- 四条 if 顺序固化"短路语义"：先 lastColor=null 任意合法（开局/Wild 后），再同色覆盖（任意数字），再 lastNumber=null 异色非法（反转后），最后严格 ±1
- `state.LastColor is null` 用 pattern matching 比 `state.LastColor == null` 在 nullable enum 上更地道（虽然两者等价）

**替代方案**：
- 长 `if-else if`：行数更多，分支并列性差
- 反转判定也抽成 `IsValidReverse`：单行表达足够，多抽函数反而碎片化

### D5：测试组织 — 一个 `[Theory]` 覆盖一类边界

**选择**：

```
CardChainRulesTests
├── [Fact]   Wild_AlwaysValid
├── [Theory] Reverse_SameColorOrLastNull_Valid (各种 state 排列)
├── [Theory] Reverse_DifferentColor_Invalid
├── [Fact]   Reverse_TwoSameColorReverses_BothValid   ← 连续两张同色 Reverse
├── [Theory] Number_SameColor_AlwaysValid
├── [Theory] Number_LastColorNull_AnyValid             ← 开局 / Wild 后任意合法
├── [Theory] Number_AscendingDirection (严格 +1 合法/非法对照)
├── [Theory] Number_DescendingDirection (严格 -1)
├── [Theory] Number_ReverseAfter_DiffColorInvalid      ← 反转后异色全非法
├── [Theory] Number_BoundaryNine_Ascending  (异色全非法理由：N'==10 不存在)
├── [Theory] Number_BoundaryZero_Descending (异色全非法理由：N'==-1 不存在)
├── [Fact]   Empty_AsNext_Invalid
├── [Fact]   Empty_AsPrev_Throws
├── [Fact]   ApplyPrev_Number_UpdatesColorAndNumber
├── [Fact]   ApplyPrev_Reverse_FlipsDirection
├── [Fact]   ApplyPrev_Wild_ResetsState
├── [Fact]   ApplyPrev_TwoReverses_DirectionRestored
├── [Fact]   IsValidNext_DoesNotMutateState
├── [Fact]   ApplyPrev_DoesNotMutatePrev
└── [Fact]   GameDesign_3_5_3_FullSequenceReplay  ← 文档示例完整重放（含连续 Reverse）

SessionStateTests
├── [Fact] DefaultConstructor_InitialValues
└── [Fact] InternalAccessibility
```

**理由**：
- `[Theory]` + `InlineData` 把"同类边界的不同输入"集中到一个测试方法，比写 N 个 `[Fact]` 紧凑
- "GAME_DESIGN 重放"作为单独 `[Fact]` 是因为它是连续动作链，用 inline data 表达不直观
- "连续两张同色 Reverse" 单独 `[Fact]`：每色 2 张 Reverse 的设计专属断言，与单张 Reverse 边界分开

### D6：`internal` API 的测试可见性

**选择**：在 `Unomata.Core.csproj`（或 `Directory.Build.props`）添加 `InternalsVisibleTo("Unomata.Core.Tests")`。

**理由**：
- 测试需要直接 new `SessionState` 并调用 `CardChainRules.IsValidNext`，但两者都是 `internal`
- `InternalsVisibleTo` 是 .NET 标准做法，不破坏发布版本对外可见性
- 替代方案"把测试目标提升到 public 仅为测试"违反"按需可见性"原则

考虑放在 `Directory.Build.props` 还是 `Unomata.Core.csproj`：

- 放 `csproj`：作用域精确，只影响 Core
- 放 `Directory.Build.props`：未来可能误暴露其它项目；不灵活
- **决定**：放 `Unomata.Core.csproj`，作用域精确

### D7：数字接龙改为"严格 ±1"且反转后异色全非法

**背景**：实施过程中发现旧规则"`lastNumber == null` 时方向约束失效（异色任意数字合法）"会导致两个问题：
1. 反转牌后玩家可任意切色，反转牌的策略价值被廉价化
2. 反转牌后非法池仅剩 ~3 张异色 Reverse，发牌算法（Change 3）的 `pick_random_illegal` 在 OptionCount=5 时不够填——非法池规模崩溃

**选择**：

1. **数字接龙规则**改为严格连续 ±1：
   - Asc 时异色合法 ⇔ `N' == lastNumber + 1`
   - Desc 时异色合法 ⇔ `N' == lastNumber - 1`
   - 旧规则"严格大于/小于"全部废除

2. **`lastColor == null` 任意合法**显式分支：
   - 开局 `(null, null, *)` 与 Wild 后 `(null, null, *)` 状态等价于"接 Wild 后"
   - 任意 Number / Reverse 合法

3. **反转后 `lastColor != null` 且 `lastNumber == null`** → **异色数字全非法**：
   - 反转牌仅打开"同色 Number / 同色 Reverse / Wild" 三条路径
   - 异色路径关闭，与"反转牌切方向但保留颜色基准"的设计意图对齐

4. **每色 2 张 Reverse + 连续两张同色 Reverse 合法**：
   - 卡池中每色 Reverse 数量 = 2（已记录于 GAME_DESIGN 3.3）
   - 第一张 Reverse 后 state = (C, null, *), 第二张同色 Reverse 仍合法（落在 "同色 Reverse" 分支）
   - 测试树新增 `Reverse_TwoSameColorReverses_BothValid`

**理由**：
- 修订让规则与"严格连续接龙 + 反转/王牌作为方向控制器"的设计直觉一致
- 反转后非法池规模回升至 ~36 张（异色 Number 30 + 异色 Reverse 6），发牌算法（Change 3）非法池稳健
- `(null, null, *)` 状态非法池仍为 0，由 Change 3 Option F"合法位扩展守卫"处理（本 change 仅承担规则定义，不承担发牌）
- 每色 2 张 Reverse 是连续切方向的策略空间，与 GAME_DESIGN 3.3 卡池规模一致

**替代方案**：
- 保留旧规则 + 在 Change 3 算法侧打补丁：把规则错误转嫁到发牌层，违反"规则在最便宜的层级表达"
- 反转后 `lastNumber` 保留：等价于"反转牌不切数字基准"，与 GAME_DESIGN 3.5.2 "反转牌只切方向、清数字"原则冲突；且对玩家而言连续切方向后的判定更难理解

**影响范围**：
- 修改 `IsValidNumber` 实现（D4）
- 修改 `cardchain-validator/specs/cardchain-validator/spec.md` Requirement "数字牌作为 next 的合法性"（多个 Scenario 重写）
- 删除 spec 中"无数字基准时方向约束失效" Scenario（语义已变）
- 新增 spec Requirement "连续两张同色 Reverse 合法"（基于每色 2 张设计）
- 重写 GAME_DESIGN 3.5.3 重放序列（覆盖新边界）
- 修订 `Docs/GAME_DESIGN.md` 3.3 卡池总数 50 → 48（Wild 不进 deck）、3.5 规则表格、3.5.3 示例
- 修订 `Docs/INTERFACE.md` 第二节伪代码、第五节发牌算法（增加 Option F 合法位扩展守卫）
- 修订 `Docs/TODO.md` Change 2 / Change 3 范围条目
- 修订 `Docs/DEVELOPMENT_PLAN.md` / `Docs/ARCHITECTURE.md` 措辞

## Risks / Trade-offs

- **[风险] `ApplyPrev` 原地修改让调用方易意外共享 state** → 缓解：`SessionState` 是 `internal sealed`，只有 `HackSession`（Change 4）持有；测试里每个测试方法 new 自己的 state，互不干扰
- **[风险] 异色同数字现在非法、且严格 ±1 让异色合法解唯一，可能与玩家直觉冲突** → 缓解：这是 GAME_DESIGN 的明确决策；UI 层（Phase 3）通过方向箭头视觉表达 + 高亮"下一张需要的数字"，玩家学完一遍就懂
- **[风险] 反转后异色全非法导致死局触发率上升** → 缓解：每色 2 张 Reverse 提供"连续切方向"策略；同色 Number/Reverse 路径仍开放；Change 3 Option F 合法位扩展守卫处理 `(C, null, *)` 极端情形
- **[风险] Empty 抛异常会让 console demo 崩溃** → 缓解：发牌算法（Change 3）保证 Empty 不进选项；`HackSession.SelectOption` 只接玩家选的选项；Empty 仅作 `CurrentCard` 显示，不进 `ApplyPrev` 调用路径
- **[Trade-off] 不暴露 public API**：Unity 端无法直接调 `CardChainRules.IsValidNext` 来 UI 预测"这张能不能接"。如果 Phase 3 UI 真需要"高亮可接选项"功能，Change 4 的 `HackSession` 加一个 `bool CanAccept(CardData)` 查询方法即可，不破坏当前设计

## Migration Plan

不涉及迁移：纯新增。`cardchain-types` 主 spec 不动，`cardchain-validator` 是独立新 capability。

## Open Questions

无。设计已固化于 D1~D7，规则细节已固化于修订后的 GAME_DESIGN 3.5（严格 ±1）+ INTERFACE 第二节伪代码。

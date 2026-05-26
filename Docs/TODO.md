# TODO.md — Phase 1 Change 拆分清单

> 本文档将 `DEVELOPMENT_PLAN.md` 中 **Phase 1 Core 层开发**任务清单，按"单测可独立验证 + 依赖逻辑链"拆分为 7 个 OpenSpec change。
> 每个 change 完成后通过 `/opsx:archive` 归档，将 delta spec 同步到主 specs，再开始下一个。

---

## 拆分原则

- 单 change 改动控制在 200~600 行级（≈ 1 人天）
- 每个 change 都能独立 `dotnet build` + `dotnet test` 通过
- 严格线性依赖，前置 change 未归档前不开新 change
- 每个 change 的测试覆盖必须落到归档 spec 的 Scenario

---

## 依赖图

```
        Change 1 ─→ Change 2 ─→ Change 3 ─→ Change 4 ─→ Change 5 ─→ Change 6 ─→ Change 7
        types       validator    deck-gen    skeleton     rewardpot   result-end   demo
```

---

## Change 1 — `cardchain-types` ✅ 已归档 (2026-05-26)

**职责**：所有 Core 层数据类型与枚举的纯定义，不含逻辑。
**归档位置**：`openspec/changes/archive/2026-05-26-cardchain-types/`
**主 spec**：`openspec/specs/cardchain-types/spec.md`

### 范围
- [x] `enum CardType { Number, Reverse, Wild, Empty }`
- [x] `enum CardColor { Red, Blue, Green, Yellow }`
- [x] `enum ChainDirection { Ascending, Descending }`
- [x] `enum EndReason { TimeUp, WrongCard, Surrender }`
- [x] `enum ComboType { None, SameColorTwice, SameDirectionTwice }`（预留，无逻辑）
- [x] `class CardData`：`Type` / `Color?` / `Number?` 三字段 + `static readonly CardData Empty`
- [x] xUnit：枚举值齐全、`CardData.Empty.Type == Empty`、不同 Type 的 nullable 字段约定（17 个测试用例全部通过）

### 验收数据
- `dotnet build` → 0 警告、0 错误
- `dotnet test` → 17 通过、0 失败、0 跳过
- `dotnet run` → 占位文本正常输出
- `grep UnityEngine` → 0 匹配
- csproj 依赖 → 仍为零

---

## Change 2 — `cardchain-validator`

**职责**：纯函数层的接龙合法性判定与状态更新（`IsValidNext` / `ApplyPrev`）。

### 范围
- [ ] `internal class SessionState`：`LastColor` / `LastNumber` / `Direction` 三字段
- [ ] `internal static class CardChainRules`（或同等命名）：
  - [ ] `IsValidNext(CardData next, SessionState state) → bool`
  - [ ] `ApplyPrev(CardData prev, SessionState state)`（in-place 修改 state）
- [ ] xUnit 覆盖：
  - [ ] 同色任意数字合法（含异色升降序边界）
  - [ ] 异色升序：N' > lastNumber 合法，反之非法
  - [ ] 异色降序：N' < lastNumber 合法，反之非法
  - [ ] 异色同数字非法（旧规则废除验证）
  - [ ] `lastNumber == null` 时方向约束失效（任意数字合法）
  - [ ] 王牌作为 next 永远合法（任何 state）
  - [ ] 反转牌：同色合法、异色非法、`lastColor == null` 时任意反转合法
  - [ ] `ApplyPrev` 数字牌：更新 lastColor/lastNumber，不切方向
  - [ ] `ApplyPrev` 反转牌：更新 lastColor、清 lastNumber、翻转方向
  - [ ] `ApplyPrev` 王牌：清 lastColor 和 lastNumber，不切方向
  - [ ] 开局起手：state=(null, null, Ascending)，任意牌合法

### 依赖
Change 1（CardData/枚举）

---

## Change 3 — `cardchain-deck-generator`

**职责**：选项生成器（按 `INTERFACE.md` 第五节"发牌算法"）+ 难度参数 config。

### 范围
- [ ] `class HackDifficultyConfig`：`OptionCount` / `TargetChainCount` / `TotalTime` / `SolvableRate` / `WildAppearRate` 五字段
- [ ] `internal class OptionGenerator`（或同等命名）：
  - [ ] `Generate(state, config, random) → (CardData[] options, bool isDeadlock)`
  - [ ] `SolvableRate` 决定是否抽 1 张合法牌
  - [ ] `WildAppearRate` 独立判定塞王牌
  - [ ] 剩余位填非法牌
  - [ ] 选项内不重复（同轮）
  - [ ] `Empty` 永不出现在选项中
  - [ ] 反转牌不强塞，仅作为合法牌候选自然出现
- [ ] 抽样池：50 张牌的逻辑代表（不必实例化 50 个对象，按 Type/Color/Number 笛卡尔积选取）
- [ ] 注入式随机源（`Random` / 可 mock 接口）便于测试
- [ ] xUnit 覆盖：
  - [ ] 选项数量始终 = `OptionCount`
  - [ ] 选项内不重复
  - [ ] `Empty` 不出现
  - [ ] `SolvableRate=1, WildAppearRate=0` 时永远有合法牌且无王牌
  - [ ] `SolvableRate=0, WildAppearRate=0` 时永远无合法牌（isDeadlock=true）
  - [ ] `WildAppearRate=1` 时永远塞 1 张王牌
  - [ ] 王牌不算入合法牌池（用 mock 验证 `pick_random_legal` 不返回 Wild）
  - [ ] 大样本统计：固定 seed 跑 N 次，验证概率收敛于配置值

### 依赖
Change 1（CardData）+ Change 2（IsValidNext 用于"合法/非法"判定）

---

## Change 4 — `hacksession-skeleton`

**职责**：`HackSession` 骨架——构造/计时/事件订阅、单轮选牌循环（不含 maxPot/latch/overflow）。

### 范围
- [ ] `class HackSession`：
  - [ ] 构造 `HackSession(HackDifficultyConfig config)`
  - [ ] 公开属性：`IsActive` / `ChainCount` / `TimeRemaining` / `CurrentCard` / `CurrentOptions` / `CurrentDirection` / `BasePot`（已能算）
  - [ ] 8 个事件签名声明（OnNewRound 含 isDeadlock 参数）
  - [ ] `Start()`：初始化 state，CurrentCard = Empty，触发首轮 OnNewRound
  - [ ] `Tick(float deltaTime)`：减少 TimeRemaining
  - [ ] `SelectOption(int)`：合法 → ApplyPrev + chain++ + OnChainSuccess + 下一轮 OnNewRound；非法 → OnChainFailed
  - [ ] `Surrender()`：方法签名声明（实现可放 Change 6，本期空实现也可）
  - [ ] `TargetId`、`OnComboTriggered` 占位（v1 不实现）
- [ ] xUnit 覆盖：
  - [ ] Start 触发首轮 OnNewRound，CurrentCard.Type=Empty
  - [ ] Start 后 IsActive=true
  - [ ] Tick 正确减少 TimeRemaining
  - [ ] SelectOption 合法：chain+1、OnChainSuccess 触发、下一轮 OnNewRound 触发
  - [ ] SelectOption 非法：OnChainFailed 触发、IsActive 处理（细节见 Change 6）
  - [ ] SelectOption 越界索引抛异常或忽略（约定其一）

### 不含
- maxPot / latch / overflow 逻辑（Change 5）
- HackResult 与 OnSessionEnd（Change 6）
- TimeUp 触发结束（Change 6）

### 依赖
Change 3（OptionGenerator 用于生成每轮选项）

---

## Change 5 — `hacksession-rewardpot`

**职责**：双层奖励池 + 满档单向 latch + 溢出计数 + 方向切换事件。

### 范围
- [ ] `MaxPot` 属性 + `IsMaxLatched` 属性 + `OverflowCount` 属性
- [ ] 反转牌 prev 时，`IsMaxLatched=false` 则 `MaxPot += 1`
- [ ] 王牌 prev 时，`IsMaxLatched=false` 则 `MaxPot += 4`
- [ ] 接牌后判定顺序：
  1. `chain += 1`
  2. 牌效结算（含 maxPot 增长）
  3. 满档判定：未 latch 且 `chain >= MaxPot` → 设 latch=true、MaxPot 冻结、触发 `OnMaxReached(MaxPot_frozen)`
  4. 溢出判定：已 latch 且 `chain > MaxPot_frozen` → `OverflowCount++`、触发 `OnOverflow(OverflowCount)`
- [ ] `OnDirectionChanged` 在反转牌 prev 后触发，参数为新方向
- [ ] xUnit 覆盖：
  - [ ] 反转牌使 MaxPot+1（满档前）
  - [ ] 王牌使 MaxPot+4（满档前）
  - [ ] 满档后反转/王牌不再增加 MaxPot（latch 冻结）
  - [ ] 满档 latch 单向：进入后即便 chain<MaxPot 仍保持 latch
  - [ ] OnMaxReached 只触发一次
  - [ ] OnOverflow 在每次满档后接合法牌时累加触发
  - [ ] OnDirectionChanged 仅反转牌触发，王牌不触发
  - [ ] 接牌顺序验证："差一张满档时来王牌" → 先 maxPot+=4 后判定（不立即满档）
  - [ ] 边界：basePot=10, 用 1 王牌 + 1 反转 → MaxPot=15，满档时 chain=15

### 依赖
Change 4（HackSession 骨架）

---

## Change 6 — `hacksession-result-and-end`

**职责**：会话结束的所有路径 + `HackResult` 计算 + `Surrender()` 完整实现。

### 范围
- [ ] `class HackResult`：`ChainCount` / `BasePot` / `MaxPot` / `OverflowCount` / `IsMaxReached` / `Reason` 字段
- [ ] `DamageReductionFactor` 计算属性（`chain / basePot`，无 clamp，basePot=0 兜底返回 0）
- [ ] `Surrender()` 完整实现：任何状态下调用合法，结束会话，触发 `OnSessionEnd(reason=Surrender)`
- [ ] `Tick` 中 `TimeRemaining <= 0` → 触发 `OnTimeUp` + `OnSessionEnd(reason=TimeUp)`
- [ ] `SelectOption` 非法路径完善：触发 `OnChainFailed` + `OnSessionEnd(reason=WrongCard)`
- [ ] `IsActive` 在结束后设为 false；之后任何方法调用应忽略或抛异常（约定其一并测试）
- [ ] xUnit 覆盖：
  - [ ] 时间到 → OnTimeUp + OnSessionEnd(TimeUp)，不扣血标记
  - [ ] 接错牌 → OnChainFailed + OnSessionEnd(WrongCard)
  - [ ] Surrender 任意状态合法（含未 Start、Start 后未 SelectOption、满档后等）
  - [ ] Surrender 触发 OnSessionEnd(Surrender)
  - [ ] HackResult.DamageReductionFactor：`chain=0 → 0.0`、`chain=basePot → 1.0`、`chain>basePot → >1.0`、`basePot=0 → 0.0`
  - [ ] HackResult 携带正确的 BasePot/MaxPot/IsMaxReached 快照
  - [ ] OnSessionEnd 后 IsActive=false，再调 Tick/SelectOption/Surrender 安全
  - [ ] 同一会话只触发一次 OnSessionEnd

### 依赖
Change 5（MaxPot/Latch 状态需被 HackResult 读取）

---

## Change 7 — `cardchain-console-demo`

**职责**：`Unomata.Core.Console` 主程序，跑通完整骇入流程并输出日志。

### 范围
- [ ] `Program.cs`：构造 config（固定参数 / 命令行参数二选一）
- [ ] `class FakePlayer`（演示用 AI 决策）：
  - [ ] 优先选第一张合法选项
  - [ ] 死局立即调 `Surrender()`
  - [ ] 满档后继续接（验证溢出充能）
- [ ] 主循环：
  - [ ] 订阅所有 8 个事件并打印日志
  - [ ] Tick 循环（固定 dt 模拟时间流逝，或按 FakePlayer 决策驱动）
  - [ ] 退出条件：`OnSessionEnd` 触发后退出
- [ ] 日志格式参照 `DEVELOPMENT_PLAN.md` Phase 1 验收示例
- [ ] 至少跑通三种结束路径的演示模式（环境变量 / 参数切换）：
  - [ ] 自然超时（TimeUp）
  - [ ] 死局突破（Surrender）
  - [ ] 接错牌（WrongCard，FakePlayer 故意选非法牌）

### 验收
- [ ] `dotnet run --project console/Unomata.Core.Console` 能输出与 DEVELOPMENT_PLAN 示例一致风格的日志
- [ ] 至少一次跑出 `Deadlock=true` + `[FakePlayer] 立即 Surrender (死局突破)`

### 依赖
Change 6（HackSession 已完整）

---

## 整体推进节奏

```
本周聚焦: Change 1 → Change 2 → Change 3
下周聚焦: Change 4 → Change 5 → Change 6 → Change 7
```

每个 change 流程：

```
/opsx:new <change-name>      # 生成 proposal + design + tasks + delta specs
→ 实现 + 测试通过
/opsx:apply                  # 标记 tasks 完成
/opsx:verify                 # 验证实现匹配 spec
/opsx:archive                # 归档, 同步 delta → 主 specs, 更新本文档勾选
```

完成后逐项把上方 `[ ]` 改为 `[x]` 并标注归档日期。

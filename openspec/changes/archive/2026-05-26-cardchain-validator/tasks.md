## 1. 内部可见性配置

- [x] 1.1 在 `CardChainCore/src/Unomata.Core/Unomata.Core.csproj` 添加 `<ItemGroup><InternalsVisibleTo Include="Unomata.Core.Tests" /></ItemGroup>`（D6 决策）
- [x] 1.2 验证 `dotnet build CardChainCore.sln` 仍通过

## 2. SessionState 实现

- [x] 2.1 创建 `CardChainCore/src/Unomata.Core/CardChain/SessionState.cs`
- [x] 2.2 定义 `internal sealed class SessionState`，含三字段：`CardColor? LastColor`、`int? LastNumber`、`ChainDirection Direction`（D2 决策）
- [x] 2.3 `Direction` 字段初始化器设为 `ChainDirection.Ascending`
- [x] 2.4 类与字段添加 XML doc summary

## 3. CardChainRules.IsValidNext 实现

- [x] 3.1 创建 `CardChainCore/src/Unomata.Core/CardChain/CardChainRules.cs`
- [x] 3.2 定义 `internal static class CardChainRules`（D1 决策）
- [x] 3.3 实现 `internal static bool IsValidNext(CardData next, SessionState state)`，用 switch 表达式按 `next.Type` 分支（D4 决策）：
  - [x] `Wild` → `true`
  - [x] `Empty` → `false`
  - [x] `Reverse` → `state.LastColor is null || next.Color == state.LastColor`
  - [x] `Number` → 委托 `IsValidNumber` 私有方法
- [x] 3.4 实现私有静态方法 `IsValidNumber(CardData next, SessionState state)`（D7 决策，严格 ±1）：
  - [x] `state.LastColor is null` → `true`（开局 / Wild 后任意合法）
  - [x] `next.Color == state.LastColor` → `true`（同色覆盖方向约束）
  - [x] `state.LastNumber is null` → `false`（反转后 lastColor!=null + lastNumber==null，异色全非法）
  - [x] 否则按 `state.Direction` 严格 ±1 判定：Asc 时 `next.Number == state.LastNumber + 1`，Desc 时 `next.Number == state.LastNumber - 1`
- [x] 3.5 default 分支抛 `ArgumentOutOfRangeException` 兜底未来枚举扩展

## 4. CardChainRules.ApplyPrev 实现

- [x] 4.1 在 `CardChainRules.cs` 实现 `internal static void ApplyPrev(CardData prev, SessionState state)`，按 `prev.Type` 分 4 个 case（D3 决策）：
  - [x] `Number` → `state.LastColor = prev.Color; state.LastNumber = prev.Number;`（不动 Direction）
  - [x] `Reverse` → `state.LastColor = prev.Color; state.LastNumber = null;` + 翻转 `Direction`
  - [x] `Wild` → `state.LastColor = null; state.LastNumber = null;`（不动 Direction）
  - [x] `Empty` → `throw new InvalidOperationException("Empty card must not be applied as prev.")`
- [x] 4.2 default 分支抛 `ArgumentOutOfRangeException`
- [x] 4.3 添加 XML doc summary 说明每个 case 的更新规则

## 5. SessionState 测试

- [x] 5.1 创建 `CardChainCore/tests/Unomata.Core.Tests/CardChain/SessionStateTests.cs`，命名空间 `Unomata.Core.Tests.CardChain`
- [x] 5.2 `[Fact] DefaultConstructor_InitialValues`：`new SessionState()` 后 `LastColor==null && LastNumber==null && Direction==Ascending`
- [x] 5.3 `[Fact] InternalAccessibility`：用 `typeof(SessionState).IsPublic == false` 断言

## 6. CardChainRules 测试 — 王牌

- [x] 6.1 创建 `CardChainRulesTests.cs`
- [x] 6.2 `[Fact] Wild_AlwaysValid`：构造多种 state（开局、含数字、刚反转、刚王牌），对每个 state 调用 `IsValidNext(Wild, state)` 都断言 `true`

## 7. CardChainRules 测试 — 反转牌

- [x] 7.1 `[Theory] Reverse_SameColorOrLastNull_Valid`：用 `InlineData` 覆盖 lastColor=null（任意反转色）+ lastColor=Red & next=Red-Reverse 等场景，断言 `true`
- [x] 7.2 `[Theory] Reverse_DifferentColor_Invalid`：lastColor=Red & next=Blue/Green/Yellow-Reverse，断言 `false`
- [x] 7.3 `[Fact] Reverse_TwoSameColorReverses_BothValid`（D7 决策，连续两张同色 Reverse）：从 `(Red,5,Asc)` 起，先 `ApplyPrev(Red-Reverse)` 得 `(Red,null,Desc)`，再调 `IsValidNext(Red-Reverse, state)` 断言 `true`，再 `ApplyPrev` 得 `(Red,null,Asc)`，验证 LastColor/LastNumber/Direction 三字段

## 8. CardChainRules 测试 — 数字牌（严格 ±1）

- [x] 8.1 `[Theory] Number_SameColor_AlwaysValid`：lastColor=Red, lastNumber=5, Asc，next=Red-{0..9}，全部断言 `true`
- [x] 8.2 `[Theory] Number_LastColorNull_AnyValid`（D7 决策，开局 / Wild 后任意合法）：lastColor=null, lastNumber=null, 方向取 Asc/Desc，next=Red/Blue/Green/Yellow-{0..9}，全部断言 `true`
- [x] 8.3 `[Theory] Number_AscendingDirection`：lastColor=Red, lastNumber=5, Asc，next=Blue-{0..9}，断言**仅 `Number==6` 时** `true`，其余 `false`
- [x] 8.4 `[Theory] Number_DescendingDirection`：lastColor=Red, lastNumber=5, Desc，next=Blue-{0..9}，断言**仅 `Number==4` 时** `true`，其余 `false`
- [x] 8.5 `[Theory] Number_ReverseAfter_DiffColorInvalid`（D7 决策，反转后异色全非法）：lastColor=Red, lastNumber=null, 方向取 Asc/Desc，next=Blue/Green/Yellow-{0..9}，全部断言 `false`
- [x] 8.6 `[Theory] Number_BoundaryNine_Ascending`：lastNumber=9, Asc，next=Red-{0..9}（同色）全部 `true`，next=Blue-{0..9}（异色）全部 `false`（理由：N'==10 不存在）
- [x] 8.7 `[Theory] Number_BoundaryZero_Descending`：lastNumber=0, Desc，next=Red-{0..9}（同色）全部 `true`，next=Blue-{0..9}（异色）全部 `false`（理由：N'==-1 不存在）

## 9. CardChainRules 测试 — Empty

- [x] 9.1 `[Fact] Empty_AsNext_Invalid`：`IsValidNext(CardData.Empty, state)` 任意 state 都断言 `false`
- [x] 9.2 `[Fact] Empty_AsPrev_Throws`：`ApplyPrev(CardData.Empty, state)` 断言抛 `InvalidOperationException`，且 state 字段值不变（前后快照对比）

## 10. CardChainRules 测试 — ApplyPrev

- [x] 10.1 `[Fact] ApplyPrev_Number_UpdatesColorAndNumber`：调用后 LastColor/LastNumber 更新，Direction 不变
- [x] 10.2 `[Fact] ApplyPrev_Reverse_FlipsDirection`：调用后 LastColor 更新、LastNumber=null、Direction 翻转
- [x] 10.3 `[Fact] ApplyPrev_Wild_ResetsState`：调用后 LastColor=null、LastNumber=null、Direction 不变
- [x] 10.4 `[Fact] ApplyPrev_TwoReverses_DirectionRestored`：连续两次反转 Direction 回到初始值

## 11. CardChainRules 测试 — 纯函数无副作用

- [x] 11.1 `[Fact] IsValidNext_DoesNotMutateState`：保存 state 三字段快照，调用 `IsValidNext`（覆盖 true/false 各一次），断言三字段值不变
- [x] 11.2 `[Fact] ApplyPrev_DoesNotMutatePrev`：保存 prev 三字段快照,调用 `ApplyPrev`，断言 prev 三字段值不变（用 Number / Reverse / Wild 各一次）

## 12. CardChainRules 测试 — GAME_DESIGN 3.5.3 重放（12 步）

- [x] 12.1 `[Fact] GameDesign_3_5_3_FullSequenceReplay`：从开局 state=(null,null,Asc) 起，按修订后文档 12 步顺序：
  - [x] Step 1 接 Red-5 → 验证合法 + state=(Red,5,Asc)
  - [x] Step 2 接 Red-2 → 验证合法 + state=(Red,2,Asc)
  - [x] Step 3 试探 Blue-5 → 验证 IsValidNext=false（异色 + 严格升序需 N'==3，5≠3；不调 ApplyPrev）
  - [x] Step 4 接 Blue-3 → 验证合法 + state=(Blue,3,Asc)
  - [x] Step 5 接 Blue-Reverse → 验证合法 + state=(Blue,null,Desc)
  - [x] Step 6 试探 Yellow-2 → 验证 IsValidNext=false（反转后 lastColor!=null + lastNumber=null，异色路径关闭）
  - [x] Step 7 接 Blue-7 → 验证合法（同色覆盖） + state=(Blue,7,Desc)
  - [x] Step 8 接 Wild → 验证合法 + state=(null,null,Desc)
  - [x] Step 9 接 Red-9 → 验证合法（lastColor=null 任意） + state=(Red,9,Desc)
  - [x] Step 10 接 Red-Reverse → 验证合法（同色，第一张） + state=(Red,null,Asc)
  - [x] Step 11 接 Red-Reverse → 验证合法（同色，第二张连续） + state=(Red,null,Desc)
  - [x] Step 12 接 Red-4 → 验证合法（同色任意） + state=(Red,4,Desc)

## 13. 构建与测试验收

- [x] 13.1 `dotnet build CardChainCore.sln` 退出码 0，0 警告
- [x] 13.2 `dotnet test CardChainCore.sln` 退出码 0，本 change 引入测试全部通过（109 通过 / 0 失败）
- [x] 13.3 验证 Change 1 已有的 17 个测试仍通过（无回归）

## 14. 文档对齐

- [x] 14.1 自检 `Docs/GAME_DESIGN.md` 3.3 卡池总数 48、3.5 规则表格、3.5.3 示例 12 步与本 change 实现一致
- [x] 14.2 自检 `Docs/INTERFACE.md` 第二节 IsValidNext 伪代码与本 change 实现一致（含 lastColor=null 显式分支、lastNumber=null 返回 false、严格 ±1）
- [x] 14.3 自检 `Docs/TODO.md` Change 2 范围条目与本 change tasks 一致
- [x] 14.4 自检 `Docs/DEVELOPMENT_PLAN.md` / `Docs/ARCHITECTURE.md` 措辞含"严格 ±1"
- [x] 14.5 更新 `Docs/TODO.md` Change 2 范围条目勾选
- [x] 14.6 准备 `/opsx:apply` 与 `/opsx:archive` 流程

## Why

Change 1 已固化所有领域类型。下一步是把"接龙合法性判定"和"会话状态更新"作为**纯函数**先写出来——这是整个 Core 层的语义内核，决定一张牌能不能接、接了之后状态怎么变。把它独立成一个 change 的好处：①没有计时、事件、IO，可以 100% 用 xUnit 把所有规则边界覆盖到位；②后续 `HackSession` 只需要把这两个函数串到事件链上，几乎零思考成本；③规则错了能在最便宜的层级发现，避免上层 debug。

会话状态机（`SessionState` + `IsValidNext` + `ApplyPrev`）是 GAME_DESIGN 3.5 节"匹配规则"和 INTERFACE 第二节"`CardData` 合法性判定"伪代码的可执行落地。

> **规则细节修订**：实施过程中将"严格升降序"收紧为**严格 ±1 升降序**，并废除"`lastNumber == null` 时方向约束失效（异色任意合法）"语义——改为"`lastColor == null` 时任意合法、`lastColor != null + lastNumber == null`（反转后）异色数字全非法"。每色 2 张 Reverse + 连续两张同色 Reverse 合法。详见 design.md D7。

## What Changes

- 新增 `internal class SessionState`：持有 `LastColor` / `LastNumber` / `Direction` 三字段
- 新增 `internal static class CardChainRules`：暴露两个纯函数
  - `bool IsValidNext(CardData next, SessionState state)`：严格 ±1 升降序判定，含同色万能规则、王牌永远合法、反转牌同色或 lastColor=null 才合法、`lastColor == null` 时任意数字合法（开局/Wild 后等价）、反转后 `lastColor != null + lastNumber == null` 时异色数字全非法
  - `void ApplyPrev(CardData prev, SessionState state)`：原地更新 state；数字牌更新 `LastColor` + `LastNumber` 不切方向；反转牌更新 `LastColor` + 清 `LastNumber` + 翻转方向；王牌清 `LastColor` 与 `LastNumber` 不切方向；`Empty` 不应作为 prev 出现（防御性断言）
- 新增 xUnit 测试套件，逐条覆盖修订后的 GAME_DESIGN 3.5 边界与示例（含连续两张同色 Reverse、反转后异色全非法）
- 不引入会话生命周期、不触发事件、不读取时间——这些进 Change 4+

## Capabilities

### New Capabilities

- `cardchain-validator`：Core 层接龙规则的纯函数实现。包含会话状态结构 `SessionState` 与两个无副作用方法 `IsValidNext` / `ApplyPrev`，是后续 `HackSession`（Change 4）的语义内核。

### Modified Capabilities

（无）

## Impact

- 新增源文件：`CardChainCore/src/Unomata.Core/CardChain/SessionState.cs` / `CardChainRules.cs`
- 新增测试目录：`CardChainCore/tests/Unomata.Core.Tests/CardChain/`（已存在，追加文件）
  - `SessionStateTests.cs` / `CardChainRulesTests_*.cs` 系列
- 不动 `cardchain-types` 已归档 spec
- 不动 Unity 端任何代码
- 与修订后的 `Docs/INTERFACE.md` 第二节"`CardData`"段中"合法性判定（`HackSession` 内部，不暴露为 CardData 方法）"伪代码契约保持一致
- 同步修订 `Docs/GAME_DESIGN.md` 3.3/3.5/3.5.3、`Docs/INTERFACE.md` 第二节伪代码 + 第五节发牌算法（Option F 合法位扩展守卫）、`Docs/TODO.md` Change 2/3 范围、`Docs/DEVELOPMENT_PLAN.md` 与 `Docs/ARCHITECTURE.md` 措辞

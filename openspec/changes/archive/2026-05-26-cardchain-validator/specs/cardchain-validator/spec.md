## ADDED Requirements

### Requirement: SessionState 结构

`Unomata.Core` 命名空间 SHALL 在 `internal` 可见性下定义类 `SessionState`，包含且仅包含以下三个字段：
- `CardColor? LastColor`：当前牌的颜色，`null` 表示无颜色基准（开局或刚接王牌后）
- `int? LastNumber`：当前牌的数字，`null` 表示无数字基准（开局、刚接反转牌、刚接王牌后）
- `ChainDirection Direction`：当前接龙方向，初始值 `Ascending`

`SessionState` 是会话内部状态载体，SHALL NOT 暴露为 `public`；仅 Core 层实现内部读写。

#### Scenario: SessionState 初始值

- **WHEN** 通过默认构造创建 `new SessionState()`
- **THEN** `LastColor` SHALL 为 `null`，`LastNumber` SHALL 为 `null`，`Direction` SHALL 为 `ChainDirection.Ascending`

#### Scenario: SessionState 不暴露为 public

- **WHEN** 通过反射读取 `SessionState` 类型可见性
- **THEN** 类型 SHALL NOT 是 `Public`（`internal` 或更窄）

---

### Requirement: 王牌作为 next 永远合法

`CardChainRules.IsValidNext(next, state)` 当 `next.Type == Wild` 时 SHALL 返回 `true`，与 `state` 的任何取值无关。

#### Scenario: 王牌在任意 state 下合法

- **WHEN** 调用 `IsValidNext(Wild, state)`，state 取值依次为：开局空 state、含数字基准的 state（任意颜色任意数字）、刚反转后 state（lastColor=Red, lastNumber=null）、刚王牌后 state（lastColor=null, lastNumber=null）
- **THEN** 每次调用都 SHALL 返回 `true`

---

### Requirement: 反转牌作为 next 的合法性

`CardChainRules.IsValidNext(next, state)` 当 `next.Type == Reverse` 时 SHALL 满足以下任一即合法：
- `state.LastColor == null`（开局或刚接王牌后，任意反转牌合法）
- `next.Color == state.LastColor`（同色反转）

否则 SHALL 返回 `false`（含异色反转、`next.Color == null` 等）。

#### Scenario: lastColor 为 null 时任意反转合法

- **WHEN** 调用 `IsValidNext(<任意色 Reverse>, state)`，state 满足 `LastColor == null`
- **THEN** SHALL 返回 `true`

#### Scenario: 同色反转合法

- **WHEN** 调用 `IsValidNext(Red-Reverse, state)`，state 满足 `LastColor == Red`
- **THEN** SHALL 返回 `true`

#### Scenario: 异色反转非法

- **WHEN** 调用 `IsValidNext(Blue-Reverse, state)`，state 满足 `LastColor == Red`
- **THEN** SHALL 返回 `false`

---

### Requirement: 连续两张同色 Reverse 合法

卡池每色提供 2 张 Reverse（见 `Docs/GAME_DESIGN.md` 3.3）。`CardChainRules.IsValidNext` 与 `ApplyPrev` 组合 SHALL 允许玩家在合适时机连续打出两张同色 Reverse 来连续翻转方向，且第二张 Reverse 的合法性 SHALL 不受第一张影响。

#### Scenario: 第一张同色 Reverse 后第二张同色 Reverse 仍合法

- **WHEN** 从 state `(Red, 5, Ascending)` 起，先 `ApplyPrev(Red-Reverse, state)` 得 `(Red, null, Descending)`，再调用 `IsValidNext(Red-Reverse, state)`
- **THEN** 第二次 `IsValidNext` SHALL 返回 `true`（同色 Reverse 合法）

#### Scenario: 连续两张同色 Reverse 后方向回到初始

- **WHEN** 从 state `(Red, 5, Ascending)` 起，先 `ApplyPrev(Red-Reverse, state)`，再 `ApplyPrev(Red-Reverse, state)`
- **THEN** 第二次调用后 state SHALL 等于 `(Red, null, Ascending)`（LastColor 仍 Red，LastNumber 仍 null，Direction 翻转两次回到 Asc）

---

### Requirement: 数字牌作为 next 的合法性

`CardChainRules.IsValidNext(next, state)` 当 `next.Type == Number` 时 SHALL 满足以下任一即合法：
- `lastColor == null`：开局或刚接王牌后，**任意数字合法**
- 同色：`next.Color == state.LastColor`（数字不限）
- 异色 + 严格 ±1：`state.LastColor != null` **且** `state.LastNumber != null` **且** 方向匹配，即
  - `state.Direction == Ascending` 时 `next.Number == state.LastNumber + 1`
  - `state.Direction == Descending` 时 `next.Number == state.LastNumber - 1`

否则 SHALL 返回 `false`。**异色同数字（如 Blue-5 接 Red-5）SHALL 永远非法**——旧版"同色 OR 同数字"规则已废除；**异色非连续（如 Blue-7 接 Red-5 升序）SHALL 同样非法**——旧版"严格大于/小于"规则已收紧为严格 ±1。

特别地：当 `state.LastColor != null` 且 `state.LastNumber == null`（**反转牌后状态**），异色数字 SHALL 永远非法——异色路径关闭，仅同色 Number / 同色 Reverse / Wild 合法。

#### Scenario: lastColor 为 null 时任意数字合法

- **WHEN** 调用 `IsValidNext(<任意色任意数字 Number>, state)`，state 满足 `LastColor == null`（开局或 Wild 后，含 `LastNumber == null` 与 `LastNumber` 任意值的情形）
- **THEN** SHALL 返回 `true`

#### Scenario: 同色任意数字合法

- **WHEN** 调用 `IsValidNext(Red-2, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Ascending`
- **THEN** SHALL 返回 `true`（同色覆盖方向约束）

#### Scenario: 异色升序合法（严格 +1）

- **WHEN** 调用 `IsValidNext(Blue-6, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Ascending`
- **THEN** SHALL 返回 `true`

#### Scenario: 异色升序非法（同数字）

- **WHEN** 调用 `IsValidNext(Blue-5, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Ascending`
- **THEN** SHALL 返回 `false`（异色同数字非法）

#### Scenario: 异色升序非法（非严格 +1）

- **WHEN** 调用 `IsValidNext(Blue-7, state)` 与 `IsValidNext(Blue-3, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Ascending`
- **THEN** 两次调用都 SHALL 返回 `false`（旧规则下 7 合法，现已收紧为严格 N'==6）

#### Scenario: 异色降序合法（严格 -1）

- **WHEN** 调用 `IsValidNext(Blue-4, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Descending`
- **THEN** SHALL 返回 `true`

#### Scenario: 异色降序非法

- **WHEN** 调用 `IsValidNext(Blue-5, state)` / `IsValidNext(Blue-3, state)` / `IsValidNext(Blue-9, state)`，state 满足 `LastColor==Red, LastNumber==5, Direction==Descending`
- **THEN** 三次调用都 SHALL 返回 `false`（仅 Blue-4 合法）

#### Scenario: 反转后异色数字全非法

- **WHEN** 调用 `IsValidNext(<任意异色任意数字 Number>, state)`，state 满足 `LastColor==Red, LastNumber==null`（刚接反转牌后），方向取 Asc 或 Desc 任意
- **THEN** SHALL 返回 `false`（异色路径在 `lastColor!=null + lastNumber==null` 时关闭）

#### Scenario: 反转后同色任意数字合法

- **WHEN** 调用 `IsValidNext(Red-x, state)`，x 取 0..9 任一，state 满足 `LastColor==Red, LastNumber==null`
- **THEN** SHALL 返回 `true`（同色覆盖方向，且无数字基准）

#### Scenario: 升序边界 9 的同色解套

- **WHEN** state 满足 `LastColor==Red, LastNumber==9, Direction==Ascending`，调用 `IsValidNext(Red-3, state)`
- **THEN** SHALL 返回 `true`（同色覆盖方向）

#### Scenario: 升序边界 9 的异色无解

- **WHEN** state 满足 `LastColor==Red, LastNumber==9, Direction==Ascending`，调用 `IsValidNext(Blue-x, state)`，x 取 0..9 任一
- **THEN** 所有 x 值 SHALL 返回 `false`（异色升序需 N'==10，不存在）

#### Scenario: 降序边界 0 的异色无解

- **WHEN** state 满足 `LastColor==Red, LastNumber==0, Direction==Descending`，调用 `IsValidNext(Blue-x, state)`，x 取 0..9 任一
- **THEN** 所有 x 值 SHALL 返回 `false`（异色降序需 N'==-1，不存在）

---

### Requirement: Empty 牌作为 next 永远非法

`CardChainRules.IsValidNext(next, state)` 当 `next.Type == Empty` 时 SHALL 返回 `false`。`Empty` 仅作开局 `CurrentCard` 占位，永不出现在选项中，更不应被选作合法 next。

#### Scenario: Empty 作为 next 非法

- **WHEN** 调用 `IsValidNext(CardData.Empty, state)`，state 取任意值
- **THEN** SHALL 返回 `false`

---

### Requirement: 数字牌作为 prev 的状态更新

`CardChainRules.ApplyPrev(prev, state)` 当 `prev.Type == Number` 时 SHALL 原地修改 `state`，满足：
- `state.LastColor = prev.Color`
- `state.LastNumber = prev.Number`
- `state.Direction` 保持不变

#### Scenario: 数字牌更新 lastColor 与 lastNumber 不切方向

- **WHEN** 调用 `ApplyPrev(Red-5, state)`，state 初始值任意（设 `Direction==Ascending`）
- **THEN** 调用后 `LastColor` SHALL 为 `Red`，`LastNumber` SHALL 为 `5`，`Direction` SHALL 仍为 `Ascending`

---

### Requirement: 反转牌作为 prev 的状态更新

`CardChainRules.ApplyPrev(prev, state)` 当 `prev.Type == Reverse` 时 SHALL 原地修改 `state`，满足：
- `state.LastColor = prev.Color`
- `state.LastNumber = null`
- `state.Direction` 翻转：`Ascending → Descending` 或 `Descending → Ascending`

#### Scenario: 反转牌切方向并清数字

- **WHEN** 调用 `ApplyPrev(Red-Reverse, state)`，state 初始 `LastColor==Blue, LastNumber==7, Direction==Ascending`
- **THEN** 调用后 `LastColor` SHALL 为 `Red`，`LastNumber` SHALL 为 `null`，`Direction` SHALL 为 `Descending`

#### Scenario: 连续两次反转牌方向回到初始

- **WHEN** 对初始 `Direction==Ascending` 的 state 连续调用 `ApplyPrev(Red-Reverse, state)` 两次
- **THEN** 第二次调用后 `Direction` SHALL 为 `Ascending`

---

### Requirement: 王牌作为 prev 的状态更新

`CardChainRules.ApplyPrev(prev, state)` 当 `prev.Type == Wild` 时 SHALL 原地修改 `state`，满足：
- `state.LastColor = null`
- `state.LastNumber = null`
- `state.Direction` 保持不变

#### Scenario: 王牌清颜色和数字基准不切方向

- **WHEN** 调用 `ApplyPrev(Wild, state)`，state 初始 `LastColor==Red, LastNumber==5, Direction==Descending`
- **THEN** 调用后 `LastColor` SHALL 为 `null`，`LastNumber` SHALL 为 `null`，`Direction` SHALL 仍为 `Descending`

---

### Requirement: Empty 牌作为 prev 的处理

`CardChainRules.ApplyPrev(prev, state)` 当 `prev.Type == Empty` 时 SHALL 抛出 `InvalidOperationException`。`Empty` 仅作 `HackSession` 开局 `CurrentCard` 占位，不应进入"被接"路径。该约束在 Core 层防御性触发，便于发现上层 bug。

#### Scenario: Empty 作为 prev 抛异常

- **WHEN** 调用 `ApplyPrev(CardData.Empty, state)`
- **THEN** SHALL 抛出 `InvalidOperationException`，state 字段值 SHALL 保持不变

---

### Requirement: GAME_DESIGN 3.5.3 示例完整可重放

`CardChainRules` 的 `IsValidNext` 与 `ApplyPrev` 组合 SHALL 能完整重放修订后 `Docs/GAME_DESIGN.md` 第 3.5.3 节的边界示例序列（严格 ±1 升降序 + 反转后异色全非法 + 连续两张同色 Reverse），每一步合法/非法判定与 state 演化都与文档一致。

#### Scenario: 重放 GAME_DESIGN 3.5.3 示例

- **WHEN** 从 `state=(null, null, Ascending)` 开始，依次执行：
  1. 接 Red-5（合法 — lastColor=null 任意 → state 变 (Red, 5, Asc)）
  2. 接 Red-2（合法 — 同色覆盖 → state 变 (Red, 2, Asc)）
  3. 试探 Blue-5（非法 — 异色 + 严格升序需 N'==3，5≠3）
  4. 接 Blue-3（合法 — 异色 + 严格 +1 → state 变 (Blue, 3, Asc)）
  5. 接 Blue-Reverse（合法 — 同色反转 → state 变 (Blue, null, Desc)）
  6. 试探 Yellow-2（非法 — 反转后 lastColor!=null + lastNumber=null，异色路径关闭）
  7. 接 Blue-7（合法 — 同色覆盖 → state 变 (Blue, 7, Desc)）
  8. 接 Wild（合法 — 永远合法 → state 变 (null, null, Desc)）
  9. 接 Red-9（合法 — 王牌后 lastColor=null 任意 → state 变 (Red, 9, Desc)）
  10. 接 Red-Reverse（合法 — 同色反转，第一张 → state 变 (Red, null, Asc)）
  11. 接 Red-Reverse（合法 — 同色反转，第二张连续 → state 变 (Red, null, Desc)）
  12. 接 Red-4（合法 — 同色任意 → state 变 (Red, 4, Desc)）
- **THEN** 每一步的 `IsValidNext` 返回值 SHALL 与文档"合法/试探"标注一致，且 `ApplyPrev` 后的 state SHALL 与文档括号内标注完全相等

---

### Requirement: 纯函数无副作用约束

`CardChainRules.IsValidNext(next, state)` SHALL NOT 修改 `state` 或 `next` 的任何字段，且 SHALL NOT 触发任何 IO（无控制台、无文件、无时间读取）。`CardChainRules.ApplyPrev(prev, state)` SHALL 仅修改传入的 `state`，SHALL NOT 修改 `prev`，SHALL NOT 触发任何 IO。

#### Scenario: IsValidNext 不修改 state

- **WHEN** 对包含初始字段值的 state 调用 `IsValidNext` 任意次（不论返回 true/false）
- **THEN** state 的 `LastColor` / `LastNumber` / `Direction` 三字段值 SHALL 与调用前完全相等

#### Scenario: ApplyPrev 不修改 prev

- **WHEN** 用同一个 `prev` 实例调用 `ApplyPrev(prev, state)`
- **THEN** `prev.Type` / `prev.Color` / `prev.Number` 三字段值 SHALL 与调用前完全相等

---

### Requirement: 单元测试覆盖

`CardChainCore/tests/Unomata.Core.Tests/CardChain/` 下 SHALL 存在 `SessionStateTests.cs` 与 `CardChainRulesTests.cs`，覆盖本 capability 所有 Scenario。`dotnet test CardChainCore.sln` SHALL 退出码为 0，新增测试用例 SHALL 全部通过、零失败、零跳过。

#### Scenario: 测试套件全部通过

- **WHEN** 在 `CardChainCore/` 下执行 `dotnet test CardChainCore.sln`
- **THEN** 退出码 SHALL 为 0，输出 SHALL 报告本 capability 引入的所有测试通过

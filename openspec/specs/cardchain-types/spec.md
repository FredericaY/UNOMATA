### Requirement: 卡牌类型枚举

`Unomata.Core` 命名空间 SHALL 定义 `CardType` 枚举，恰好包含四个成员：`Number`、`Reverse`、`Wild`、`Empty`。语义如下：
- `Number`：数字牌，有颜色有数字（0-9）
- `Reverse`：反转牌，有颜色无数字
- `Wild`：王牌，无颜色无数字
- `Empty`：占位空牌，仅作为 `HackSession.CurrentCard` 开局值使用，永不参与发牌

#### Scenario: CardType 枚举成员齐全

- **WHEN** 通过反射列举 `CardType` 的所有成员
- **THEN** 结果 SHALL 包含且仅包含 `Number`、`Reverse`、`Wild`、`Empty` 四项

---

### Requirement: 卡牌颜色枚举

`Unomata.Core` 命名空间 SHALL 定义 `CardColor` 枚举，恰好包含四个成员：`Red`、`Blue`、`Green`、`Yellow`。

#### Scenario: CardColor 枚举成员齐全

- **WHEN** 通过反射列举 `CardColor` 的所有成员
- **THEN** 结果 SHALL 包含且仅包含 `Red`、`Blue`、`Green`、`Yellow` 四项

---

### Requirement: 接龙方向枚举

`Unomata.Core` 命名空间 SHALL 定义 `ChainDirection` 枚举，恰好包含两个成员：`Ascending`、`Descending`，分别表示数字接龙的升序与降序方向。

#### Scenario: ChainDirection 枚举成员齐全

- **WHEN** 通过反射列举 `ChainDirection` 的所有成员
- **THEN** 结果 SHALL 包含且仅包含 `Ascending`、`Descending` 两项

---

### Requirement: 会话结束原因枚举

`Unomata.Core` 命名空间 SHALL 定义 `EndReason` 枚举，恰好包含三个成员：`TimeUp`、`WrongCard`、`Surrender`，对应骇入会话三种结束路径：自然超时、接错牌、玩家或系统主动弃牌。

#### Scenario: EndReason 枚举成员齐全

- **WHEN** 通过反射列举 `EndReason` 的所有成员
- **THEN** 结果 SHALL 包含且仅包含 `TimeUp`、`WrongCard`、`Surrender` 三项

#### Scenario: EndReason 不含旧版 Manual

- **WHEN** 通过反射列举 `EndReason` 的所有成员
- **THEN** 结果 SHALL NOT 包含名称为 `Manual` 的成员（旧版命名已废弃）

---

### Requirement: Combo 类型占位枚举

`Unomata.Core` 命名空间 SHALL 定义 `ComboType` 枚举，恰好包含三个成员：`None`、`SameColorTwice`、`SameDirectionTwice`。本枚举为后续 Combo 系统预留接口，v1 不实现任何检测逻辑，所有运行时取值 SHALL 始终为 `None`（在后续 change 中实现的 `HackSession.OnComboTriggered` 事件契约约束）。

#### Scenario: ComboType 枚举成员齐全

- **WHEN** 通过反射列举 `ComboType` 的所有成员
- **THEN** 结果 SHALL 包含且仅包含 `None`、`SameColorTwice`、`SameDirectionTwice` 三项

---

### Requirement: CardData 数据结构

`Unomata.Core` 命名空间 SHALL 定义 `CardData` 类，包含且仅包含以下三个公开实例字段或属性：
- `CardType Type`：必填
- `CardColor? Color`：可空，仅在 `Type` 为 `Number` 或 `Reverse` 时允许有值，`Wild` 与 `Empty` 时 SHALL 为 `null`
- `int? Number`：可空，仅在 `Type` 为 `Number` 时允许有值且取值范围 0..9，其它三种 `Type` 时 SHALL 为 `null`

`CardData` SHALL NOT 暴露任何方法（包括废弃的 `CanFollow`），所有合法性判定由后续 change 中的 `HackSession` 内部完成。

#### Scenario: Number 牌的字段约定

- **WHEN** 构造 `new CardData { Type = Number, Color = Red, Number = 5 }`
- **THEN** 三个字段 SHALL 分别为 `Number`、`Red`、`5`

#### Scenario: Reverse 牌的字段约定

- **WHEN** 构造 `new CardData { Type = Reverse, Color = Blue, Number = null }`
- **THEN** `Type` SHALL 为 `Reverse`，`Color` SHALL 为 `Blue`，`Number` SHALL 为 `null`

#### Scenario: Wild 牌的字段约定

- **WHEN** 构造 `new CardData { Type = Wild, Color = null, Number = null }`
- **THEN** `Type` SHALL 为 `Wild`，`Color` 与 `Number` SHALL 均为 `null`

#### Scenario: CardData 不暴露废弃方法 CanFollow

- **WHEN** 通过反射列举 `CardData` 的所有公开方法
- **THEN** 结果 SHALL NOT 包含名称为 `CanFollow` 的方法

---

### Requirement: CardData.Empty 占位实例

`CardData` 类 SHALL 提供 `static readonly CardData Empty` 字段，其字段值 SHALL 满足 `Type == Empty && Color == null && Number == null`。该实例为单例语义，多次访问 SHALL 返回同一引用，且 SHALL 在类型加载后立即可用（不依赖任何运行时初始化）。

`CardData.Empty` 仅用作 `HackSession.CurrentCard` 的开局占位；后续 change 中的发牌算法 SHALL NOT 将其放入选项列表。

#### Scenario: CardData.Empty 字段值正确

- **WHEN** 读取 `CardData.Empty`
- **THEN** `Type` SHALL 为 `Empty`，`Color` SHALL 为 `null`，`Number` SHALL 为 `null`

#### Scenario: CardData.Empty 是单例

- **WHEN** 多次读取 `CardData.Empty`
- **THEN** 每次读取 SHALL 返回同一对象引用（`object.ReferenceEquals` 为 `true`）

---

### Requirement: 类型层零运行时依赖与零业务逻辑

`cardchain-types` capability 引入的类型源文件（`CardData.cs` / `CardType.cs` / `CardColor.cs` / `ChainDirection.cs` / `EndReason.cs` / `ComboType.cs`）SHALL NOT 包含任何业务逻辑（不实现接龙合法性、状态更新、计时、结算）。`Unomata.Core.csproj` SHALL 保持零 `PackageReference`、零 `ProjectReference`、不引用 `UnityEngine`。

后续 change（如 `cardchain-validator`）可以在 `CardChainCore/src/Unomata.Core/CardChain/` 子目录下新增含业务逻辑的源文件，本 Requirement 不约束这些后续文件。

#### Scenario: Core 项目仍无第三方依赖

- **WHEN** 读取 `CardChainCore/src/Unomata.Core/Unomata.Core.csproj`
- **THEN** 文件 SHALL NOT 新增任何 `<PackageReference>` 或 `<ProjectReference>` 元素

---

### Requirement: 单元测试覆盖类型契约

`CardChainCore/tests/Unomata.Core.Tests/` 下 SHALL 存在覆盖本 capability 所有 Scenario 的 xUnit 测试。`dotnet test CardChainCore.sln` SHALL 退出码为 0，且所有新增测试用例 SHALL 通过。

#### Scenario: 测试套件可运行且全部通过

- **WHEN** 在 `CardChainCore/` 下执行 `dotnet test CardChainCore.sln`
- **THEN** 退出码 SHALL 为 0，且输出 SHALL 报告本 capability 引入的测试全部通过、零失败、零跳过

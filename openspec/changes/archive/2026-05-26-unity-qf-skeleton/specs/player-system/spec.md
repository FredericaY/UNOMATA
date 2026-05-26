## ADDED Requirements

### Requirement: PlayerModel 数据结构

`Unomata.Gameplay` 命名空间 SHALL 定义 `PlayerModel` 类，继承 `AbstractModel`，包含以下两个公开属性：
- `BindableProperty<float> HP`：玩家当前生命值，初始值由 `OnInit()` 设置（占位值 100f）
- `BindableProperty<float> MaxHp`：玩家最大生命值，初始值 100f

`PlayerModel` SHALL NOT 包含任何业务逻辑（仅数据 + `OnInit` 初始化），符合 QFramework Model 层职责约束。

#### Scenario: PlayerModel 初始值正确

- **WHEN** `GameApp` 初始化，进入 Play Mode
- **THEN** `this.GetModel<PlayerModel>().HP.Value` SHALL 等于 100f，`MaxHp.Value` SHALL 等于 100f

#### Scenario: PlayerModel HP 可写读

- **WHEN** 对 `PlayerModel.HP` 赋新值（如 `HP.Value = 80f`）
- **THEN** 再次读取 `HP.Value` SHALL 返回 80f，并触发 `HP.Register` 订阅回调

---

### Requirement: PlayerSystem TakeDamage 方法

`Unomata.Gameplay` 命名空间 SHALL 定义 `PlayerSystem` 类，继承 `AbstractSystem`，实现以下公开方法：
- `public void TakeDamage(float raw)`：将 `raw` 从 `PlayerModel.HP` 中扣除，HP 不得低于 0f（使用 `Mathf.Max(0f, ...)`）

`PlayerSystem.OnInit()` SHALL 通过 `this.GetModel<PlayerModel>()` 获取 PlayerModel 引用。

#### Scenario: TakeDamage 正常扣血

- **WHEN** HP 为 100f，调用 `PlayerSystem.TakeDamage(30f)`
- **THEN** `PlayerModel.HP.Value` SHALL 变为 70f

#### Scenario: TakeDamage 不低于零

- **WHEN** HP 为 20f，调用 `PlayerSystem.TakeDamage(50f)`
- **THEN** `PlayerModel.HP.Value` SHALL 为 0f（不得为负数）

#### Scenario: TakeDamage 触发 HP BindableProperty 回调

- **WHEN** 有代码注册了 `PlayerModel.HP.Register(callback)`，然后调用 `TakeDamage(10f)`
- **THEN** `callback` SHALL 被调用，参数为新 HP 值

---

### Requirement: GameApp 注册 PlayerModel 和 PlayerSystem

`GameApp.Init()` SHALL 按顺序先注册 Model 后注册 System：
1. `this.RegisterModel<PlayerModel>(new PlayerModel())`
2. `this.RegisterSystem<PlayerSystem>(new PlayerSystem())`（可与 WaveSystem 任意顺序，但必须在 Model 之后）

#### Scenario: GameApp 初始化后可获取 PlayerModel

- **WHEN** 进入 Play Mode，GameApp 初始化完成
- **THEN** `this.GetModel<PlayerModel>()` SHALL 返回非 null 实例

#### Scenario: GameApp 初始化后可获取 PlayerSystem

- **WHEN** 进入 Play Mode，GameApp 初始化完成
- **THEN** `this.GetSystem<PlayerSystem>()` SHALL 返回非 null 实例

---

### Requirement: Commands 目录与骨架声明

`Assets/_Project/Scripts/Gameplay/Commands/` 目录 SHALL 存在，并包含以下 4 个 Command 类文件（命名空间 `Unomata.Gameplay`，每个文件一公开类）：
- `StartHackCommand`：继承 `AbstractCommand`，`OnExecute()` 为空实现占位
- `SelectCardCommand`：继承 `AbstractCommand`，`OnExecute()` 为空实现占位
- `HealCommand`：继承 `AbstractCommand`，`OnExecute()` 为空实现占位
- `DamagePlayerCommand`：继承 `AbstractCommand`，`OnExecute()` 为空实现占位

所有 Command 类 SHALL 实现 `IController` 所需的 `GetArchitecture()` 返回 `GameApp.Interface`。

#### Scenario: 所有 Command 类可被实例化而不抛异常

- **WHEN** 调用 `new StartHackCommand()` / `new SelectCardCommand()` / `new HealCommand()` / `new DamagePlayerCommand()`
- **THEN** 每次调用 SHALL 返回非 null 对象，不抛出任何异常

#### Scenario: DamagePlayerCommand 空实现不影响链路

- **WHEN** 通过 `this.SendCommand<DamagePlayerCommand>()` 发出命令
- **THEN** Unity Console SHALL 无红色错误，QF 链路正常（即使 OnExecute 为空）

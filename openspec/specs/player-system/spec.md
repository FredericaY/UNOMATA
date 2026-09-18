# player-system Specification

## Purpose

Player 数据与系统能力：PlayerModel 数据结构、PlayerSystem 伤害处理、GameApp 注册、输入 Command 骨架等 QFramework 分层规格。
## Requirements
### Requirement: PlayerModel 数据结构

`Unomata.Gameplay` 命名空间 SHALL 定义 `PlayerModel` 类，继承 `AbstractModel`，包含以下两个公开属性：
- `BindableProperty<float> HP`：玩家当前生命值，初始值 100f
- `BindableProperty<float> MaxHp`：玩家最大生命值，初始值 100f

`PlayerModel` SHALL NOT 包含任何业务逻辑（仅数据 + `OnInit` 初始化），符合 QFramework Model 层职责约束。HP 类型使用 `float` 以便于 Phase 4 经 `DamageReductionFactor` 计算后直接施加伤害，UI 显示整数血条由表现层 `Mathf.RoundToInt` 处理。

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

`Assets/_Project/Scripts/Gameplay/Commands/` 目录 SHALL 存在，并包含以下 4 个 Command 类文件（命名空间 `Unomata.Gameplay`，每个文件一公开类，均继承 `AbstractCommand`）：
- `StartHackCommand`：`OnExecute()` 为空实现占位，B4 填充骧入触发逻辑
- `SelectCardCommand`：`OnExecute()` 为空实现占位，Phase 4 填充选牌逻辑
- `HealCommand`：`OnExecute()` 为空实现占位，Phase 4 填充生命回复充能逻辑
- `DamagePlayerCommand`：`OnExecute()` 为空实现占位，B3b 填充调用 `PlayerSystem.TakeDamage`

`AbstractCommand` 内部已持有架构上下文，Command 类 SHALL NOT 手动实现 `GetArchitecture()`（仅 `IController` MonoBehaviour 需要）。

#### Scenario: 所有 Command 类可通过 SendCommand 发出而不抛异常

- **WHEN** 通过 `this.SendCommand<DamagePlayerCommand>()` 等发出命令
- **THEN** Unity Console SHALL 无红色错误，QF 链路正常（OnExecute 为空实现不引入副作用）

### Requirement: PlayerSystem 订阅 PlayerInputModel.IsAiming 驱动瞄准状态

瞄准输入 SHALL 以 PlayerInputModel.IsAiming 为唯一权威输入值。`SetAimStateCommand(bool)` SHALL 先写该值；PlayerSystem 初始化时 SHALL 同步已有输入值并订阅变化，以更新 PlayerModel.IsAiming 并广播 AimStateChangedEvent。该命令 SHALL NOT 同时直接调用 PlayerSystem.SetAiming 或另发事件，避免一份意图产生双路通知。

每次实际瞄准状态转换 SHALL 产生一次匹配的新状态事件；同值重复写入 SHALL 不产生额外转换事件。输入复位 SHALL 经相同链路退出瞄准。SetAiming 的状态同步行为及动画/相机消费者接口 SHALL 保持兼容；System 订阅 SHALL 随其架构生命周期释放，不持有场景 MonoBehaviour。

#### Scenario: PlayerInputModel.IsAiming 变化触发 PlayerSystem.SetAiming
- **WHEN** SetAimStateCommand 将 PlayerInputModel.IsAiming 从 false 改为 true
- **THEN** PlayerModel.IsAiming SHALL 随即为 true，并恰好广播一次 AimStateChangedEvent(true)，现有动画和相机消费者可响应

#### Scenario: SetAimStateCommand 链路完整（保持 B1b.2 行为）
- **WHEN** 正常启用时发出 SetAimStateCommand(true)，随后发出 false
- **THEN** 链路 SHALL 为 PlayerController → Command → PlayerInputModel → PlayerSystem → PlayerModel / AimStateChangedEvent → 动画/相机；进入和退出转换正常，不要求本 change 解决未验收的 IK 枪口姿态

#### Scenario: 同值写入不重复广播
- **WHEN** 连续写入 true、true、false、false，初始值为 false
- **THEN** 观察者 SHALL 仅收到一次进入和一次退出事件，两个 Model 的值始终一致

#### Scenario: 输入清理退出瞄准
- **WHEN** 玩家正在瞄准时发生输入失效并复位
- **THEN** 两个 Model 的瞄准值 SHALL 均为 false，发生一次退出事件；相机和动画按已有过渡规则退出，不残留上一轮状态

#### Scenario: 系统释放与重新初始化
- **WHEN** 当前架构按已有生命周期释放，随后建立独立的新上下文
- **THEN** 旧 System 的输入订阅 SHALL 已注销，新上下文只有一份有效订阅；不得以重新创建架构完成旧对象清理

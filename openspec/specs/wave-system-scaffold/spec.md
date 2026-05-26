### Requirement: WaveModel 数据结构

`Unomata.Gameplay` 命名空间 SHALL 定义 `WaveModel` 类，继承 `AbstractModel`，包含以下两个公开属性：
- `BindableProperty<int> WaveNumber`：当前波次编号，初始值 0（第一波开始前为 0，第一波开始后为 1）
- `BindableProperty<int> AliveCount`：本波当前存活敌人数量，初始值 0

`WaveModel` SHALL NOT 包含任何业务逻辑（仅数据 + `OnInit` 初始化），符合 QFramework Model 层职责约束。B0 阶段 `WaveModel` 不维护敌人对象列表（由 B3c change 扩展添加）。

#### Scenario: WaveModel 初始值正确

- **WHEN** `GameApp` 初始化，进入 Play Mode
- **THEN** `this.GetModel<WaveModel>().WaveNumber.Value` SHALL 等于 0，`AliveCount.Value` SHALL 等于 0

#### Scenario: WaveModel AliveCount 可写读

- **WHEN** 对 `WaveModel.AliveCount` 赋新值（如 `AliveCount.Value = 3`）
- **THEN** 再次读取 `AliveCount.Value` SHALL 返回 3，并触发订阅回调

---

### Requirement: WaveSystem 骨架

`Unomata.Gameplay` 命名空间 SHALL 定义 `WaveSystem` 类，继承 `AbstractSystem`，包含以下方法骨架：
- `public void OnStartWave()`：B0 为空实现，B3c 填充生成敌人逻辑
- `public void OnEnemyKilled()`：B0 为空实现，B3c 填充 AliveCount-- 与全灭判定

`WaveSystem.OnInit()` SHALL 通过 `this.GetModel<WaveModel>()` 获取 WaveModel 引用。

#### Scenario: WaveSystem 可获取 WaveModel

- **WHEN** 进入 Play Mode，GameApp 初始化完成
- **THEN** 在 `WaveSystem.OnInit()` 内部调用 `this.GetModel<WaveModel>()` SHALL 返回非 null 实例，不抛异常

#### Scenario: OnStartWave 空实现不抛异常

- **WHEN** 调用 `this.GetSystem<WaveSystem>().OnStartWave()`
- **THEN** Unity Console SHALL 无红色错误（空实现不引入副作用）

---

### Requirement: GameApp 注册 WaveModel 和 WaveSystem

`GameApp.Init()` SHALL 在注册完所有 Model 之后，再注册 WaveSystem：
1. `this.RegisterModel<WaveModel>(new WaveModel())`（与 PlayerModel 同为 Model 层，顺序在 System 之前）
2. `this.RegisterSystem<WaveSystem>(new WaveSystem())`

#### Scenario: GameApp 初始化后可获取 WaveModel

- **WHEN** 进入 Play Mode，GameApp 初始化完成
- **THEN** `this.GetModel<WaveModel>()` SHALL 返回非 null 实例

#### Scenario: GameApp 初始化后可获取 WaveSystem

- **WHEN** 进入 Play Mode，GameApp 初始化完成
- **THEN** `this.GetSystem<WaveSystem>()` SHALL 返回非 null 实例

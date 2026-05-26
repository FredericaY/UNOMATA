## 1. PlayerModel

- [x] 1.1 删除 `Assets/_Project/Scripts/Gameplay/Player/.gitkeep`
- [x] 1.2 创建 `Assets/_Project/Scripts/Gameplay/Player/PlayerModel.cs`，命名空间 `Unomata.Gameplay`，类继承 `AbstractModel`
- [x] 1.3 声明 `public BindableProperty<float> HP = new BindableProperty<float>(100f)`
- [x] 1.4 声明 `public BindableProperty<float> MaxHp = new BindableProperty<float>(100f)`
- [x] 1.5 实现 `protected override void OnInit()` 空实现（BindableProperty 已在声明时初始化）
- [x] 1.6 为类添加 XML doc summary 注释

## 2. PlayerSystem

- [x] 2.1 创建 `Assets/_Project/Scripts/Gameplay/Player/PlayerSystem.cs`，命名空间 `Unomata.Gameplay`，类继承 `AbstractSystem`
- [x] 2.2 声明私有字段 `private PlayerModel _playerModel`
- [x] 2.3 实现 `protected override void OnInit()`：`_playerModel = this.GetModel<PlayerModel>()`
- [x] 2.4 实现 `public void TakeDamage(float raw)`：`_playerModel.HP.Value = Mathf.Max(0f, _playerModel.HP.Value - raw)`
- [x] 2.5 为类和方法添加 XML doc summary 注释

## 3. WaveModel

- [x] 3.1 删除 `Assets/_Project/Scripts/Gameplay/Wave/.gitkeep`
- [x] 3.2 创建 `Assets/_Project/Scripts/Gameplay/Wave/WaveModel.cs`，命名空间 `Unomata.Gameplay`，类继承 `AbstractModel`
- [x] 3.3 声明 `public BindableProperty<int> WaveNumber = new BindableProperty<int>(0)`
- [x] 3.4 声明 `public BindableProperty<int> AliveCount = new BindableProperty<int>(0)`
- [x] 3.5 实现 `protected override void OnInit()` 空实现
- [x] 3.6 为类添加 XML doc summary 注释

## 4. WaveSystem

- [x] 4.1 创建 `Assets/_Project/Scripts/Gameplay/Wave/WaveSystem.cs`，命名空间 `Unomata.Gameplay`，类继承 `AbstractSystem`
- [x] 4.2 声明私有字段 `private WaveModel _waveModel`
- [x] 4.3 实现 `protected override void OnInit()`：`_waveModel = this.GetModel<WaveModel>()`
- [x] 4.4 实现 `public void OnStartWave()` 空实现（添加 TODO 注释：B3c 填充生成逻辑）
- [x] 4.5 实现 `public void OnEnemyKilled()` 空实现（添加 TODO 注释：B3c 填充 AliveCount-- 与全灭判定）
- [x] 4.6 为类和方法添加 XML doc summary 注释

## 5. Commands 目录与骨架

- [x] 5.1 删除 `Assets/_Project/Scripts/Gameplay/.gitkeep`（若存在）
- [x] 5.2 在 `Assets/_Project/Scripts/Gameplay/Commands/` 下创建 `StartHackCommand.cs`：命名空间 `Unomata.Gameplay`，继承 `AbstractCommand`，`OnExecute()` 空实现 + 注释"B4 填充骧入触发逻辑"
- [x] 5.3 创建 `SelectCardCommand.cs`：命名空间 `Unomata.Gameplay`，继承 `AbstractCommand`，`OnExecute()` 空实现 + 注释"Phase 4 填充选牌逻辑"
- [x] 5.4 创建 `HealCommand.cs`：命名空间 `Unomata.Gameplay`，继承 `AbstractCommand`，`OnExecute()` 空实现 + 注释"Phase 4 填充生命回复充能逻辑"
- [x] 5.5 创建 `DamagePlayerCommand.cs`：命名空间 `Unomata.Gameplay`，继承 `AbstractCommand`，`OnExecute()` 空实现 + 注释"B3b 填充：调用 PlayerSystem.TakeDamage"

## 6. GameApp 注册

- [x] 6.1 打开 `Assets/_Project/Scripts/Gameplay/GameApp.cs`
- [x] 6.2 删除现有注释占位符，填充 `Init()` 方法，严格按顺序：
  ```
  this.RegisterModel<PlayerModel>(new PlayerModel());
  this.RegisterModel<WaveModel>(new WaveModel());
  this.RegisterSystem<PlayerSystem>(new PlayerSystem());
  this.RegisterSystem<WaveSystem>(new WaveSystem());
  ```
- [x] 6.3 添加必要的 `using Unomata.Gameplay;` 引用（若 GameApp 本身在同命名空间则无需）
- [x] 6.4 更新类头注释，说明当前注册的 System/Model 清单

## 7. QFrameworkValidator 扩展

- [x] 7.1 打开 `Assets/_Project/Scripts/Gameplay/Tests/QFrameworkValidator.cs`
- [x] 7.2 在现有 `Start()` 末尾追加 Phase 2 验证逻辑：
  - 获取 PlayerModel，断言 HP 初始值 == 100f，输出日志
  - 调用 `this.GetSystem<PlayerSystem>().TakeDamage(30f)`，断言 HP == 70f，输出日志
  - 调用 `TakeDamage(200f)`，断言 HP == 0f（不低于零），输出日志
  - 获取 WaveSystem，调用 `GetModel<WaveModel>()`，断言非 null，输出日志
- [x] 7.3 所有验证通过后输出 `Debug.Log("[QF验证通过] Phase2 骨架 System/Model 链路正常")`
- [x] 7.4 确认文件总行数不超过 300 行

## 8. 编译与 Play Mode 验收

- [x] 8.1 保存所有文件，等待 Unity 编译，确认 Console 零红色错误
- [x] 8.2 进入 Play Mode，确认 Console 输出包含 `[QF验证通过] Phase2 骨架 System/Model 链路正常`
- [x] 8.3 确认已有的 `[QF验证通过] Command→System→Event 链路正常` 日志仍然输出（无回归）
- [x] 8.4 退出 Play Mode，保存场景

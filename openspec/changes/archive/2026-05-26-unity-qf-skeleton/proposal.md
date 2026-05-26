## Why

Phase 2 Unity TPS 开发正式启动，所有业务逻辑 MonoBehaviour 必须通过 QFramework Architecture 分层接入——但当前 `GameApp.cs` 的 `Init()` 仍是空实现，没有任何 System/Model 注册，后续 B1~B4 change 无法在骨架上追加。本 change 一次性建立 Phase 2 所需的全部 QF 骨架，作为所有 B 端 Unity change 的公共前置依赖。

## What Changes

- **新建** `Assets/_Project/Scripts/Gameplay/Player/PlayerModel.cs`：`BindableProperty<float> HP`、`BindableProperty<float> MaxHp`
- **新建** `Assets/_Project/Scripts/Gameplay/Player/PlayerSystem.cs`：`TakeDamage(float raw)` 方法（扣 HP，保证不低于零）
- **新建** `Assets/_Project/Scripts/Gameplay/Wave/WaveModel.cs`：`BindableProperty<int> WaveNumber`、`BindableProperty<int> AliveCount`
- **新建** `Assets/_Project/Scripts/Gameplay/Wave/WaveSystem.cs`：`OnStartWave()` / `OnEnemyKilled()` 骨架（空实现，B3c 填充）
- **新建** `Assets/_Project/Scripts/Gameplay/Commands/` 目录及 4 个 Command 骨架文件：`StartHackCommand` / `SelectCardCommand` / `HealCommand` / `DamagePlayerCommand`（全部空 `OnExecute`）
- **修改** `Assets/_Project/Scripts/Gameplay/GameApp.cs`：在 `Init()` 中按规范顺序注册 2 个 Model + 2 个 System
- **修改** `Assets/_Project/Scripts/Gameplay/Tests/QFrameworkValidator.cs`：扩展 Phase 2 骨架验证链路（PlayerModel HP 读写、PlayerSystem.TakeDamage 触发 HP 变化、WaveSystem 可获取 WaveModel）

## Capabilities

### New Capabilities

- `player-system`: 玩家 HP 数据模型（PlayerModel）与受伤逻辑（PlayerSystem.TakeDamage），是玩家受击链路的 QF 层实现
- `wave-system-scaffold`: 波次数据模型（WaveModel：WaveNumber / AliveCount）与波次管理骨架（WaveSystem），为 B3c 波次管理器提供 QF 接入基础

### Modified Capabilities

- `qframework-integration`: GameApp.cs 的 Init() 由空实现扩展为完整注册；QFrameworkValidator 扩展 Phase 2 骨架验证

## Impact

- **代码**：`Assets/_Project/Scripts/Gameplay/` 下新增 9 个文件（Player/ 2 个、Wave/ 2 个、Commands/ 4 个 + 目录），修改 2 个现有文件
- **依赖**：不新增任何 Unity Package；所有代码仅依赖 QFramework（已安装）
- **后续 change**：B1a~B4 所有 Unity change 均以本 change 归档为前置条件；`StartHackCommand` / `DamagePlayerCommand` 将在 B3b/B4 中填充实际逻辑
- **不涉及**：Core 层代码、Phase 1 change、Unity 场景文件（无场景变更）

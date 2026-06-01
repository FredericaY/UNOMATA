## Why

目前所有输入状态（移动、跳跃、冲刺、瞄准）仍由 `StarterAssetsInputs` 的公开字段直接持有，绕过 QFramework 单一数据源规范；`TempAimInputDriver` 是 B1b.2 留下的临时文件，需替换为正式入口 `PlayerController`。此外 B1b.2 明确遗留了两个问题需在本 change 修复：瞄准移动时角色转身而非侧移（Strafe 逻辑缺失），以及 AnimatorAimBridge 的 MoveX/Y 直接读 `Input.GetAxis` 绕过 QF。

## What Changes

- **新建** `PlayerInputModel`：`Move / Jump / Sprint / IsAiming / Fire` 全部改为 `BindableProperty<>`，成为输入状态唯一来源
- **新建** `PlayerController`（IController，`[-20]`）：订阅 `PlayerInput` Action 回调 → `SendCommand<SetXxxInputCommand>`，替代 `TempAimInputDriver` 的临时瞄准入口并扩展全部输入
- **新建** `SAInputAdapter`（`[-10]`）：`LateUpdate` 将 `PlayerInputModel` 字段单向同步到 `StarterAssetsInputs`，使 `ThirdPersonController`（第三方，不改）继续正常消费输入
- **新建** `StrafeController`（IController，`LateUpdate`）：瞄准时在 TPC Update 后覆盖 transform.rotation，使角色身体锁定相机朝向（侧移 Strafe 行为）
- **新建** 4 个输入 Command：`SetMoveInputCommand / SetJumpInputCommand / SetSprintInputCommand / SetFireInputCommand`（`SetFireInputCommand` 为骨架，B2a 填充）
- **修改** `PlayerSystem.OnInit`：订阅 `PlayerInputModel.IsAiming` 变化（替代 B1b.2 的直接 Command 路由）
- **修改** `AnimatorAimBridge`：`MoveX / MoveY` 改从 `PlayerInputModel.Move` 读取，停止直接读 `Input.GetAxis`
- **修改** `GameApp`：注册 `PlayerInputModel`（PlayerModel 之后）
- **修改** 场景接线：`PlayerArmature` 上 `PlayerInput` 的 Behavior 改为 `Invoke C# Events`，回调连接 `PlayerController`，断开 `StarterAssetsInputs` 的默认绑定
- **删除** `TempAimInputDriver.cs`
- **扩展** `.inputactions` 资产（复制 SA 原文件到 `Assets/_Project/`，新增 `Aim` / `Fire` Action）

**StarterAssets ThirdParty 中的所有文件均不得改动。**

## Capabilities

### New Capabilities

- `player-input-model`：`PlayerInputModel` 及其完整 QF 输入链路（`PlayerController` → Commands → Model → `SAInputAdapter` → SA）；包含 Strafe 侧移的 `StrafeController` 行为

### Modified Capabilities

- `player-system`：`PlayerSystem.OnInit` 新增订阅 `PlayerInputModel.IsAiming` 变化以驱动瞄准状态（现有 `SetAiming` 触发时机从 Command 改为 Model 变化反应）
- `character-controller`：`AnimatorAimBridge.Update` 中 MoveX/Y 数据源从 `Input.GetAxis` 改为 `PlayerInputModel.Move`；新增 Strafe 朝向覆盖行为（瞄准时角色面朝相机方向）

## Impact

- `Assets/_Project/Scripts/Gameplay/Player/`：新增 `PlayerInputModel.cs` / `PlayerController.cs` / `SAInputAdapter.cs` / `StrafeController.cs`；修改 `AnimatorAimBridge.cs`；删除 `TempAimInputDriver.cs`
- `Assets/_Project/Scripts/Gameplay/Commands/`：新增 4 个 Command 文件
- `Assets/_Project/Scripts/Gameplay/GameApp.cs`：新增注册行
- `Assets/_Project/Scripts/Gameplay/Player/PlayerSystem.cs`：OnInit 订阅变化
- `Assets/_Project/Animations/Player/`（或 `Settings/`）：新增 `.inputactions` 副本
- SampleScene：PlayerArmature 场景接线变更（PlayerInput Behavior + 挂载新组件）
- 依赖：`UnityEngine.InputSystem`（已在 manifest.json 中）

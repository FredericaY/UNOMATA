# Proposal: unity-character-aim-layer

## Summary

在 B1b.1 产出的 `UnomataPlayer.controller` 上新增 `UpperBodyAim` 动画层，并接入 Cinemachine 双相机切换系统，实现持枪瞄准状态下的上半身动画叠加与镜头推进效果。

本 change 对应 DEVELOPMENT_PLAN Phase 2.1 · B1b.2。

---

## Motivation

- B1b.1 已将 Base Layer 动画素材统一为 RifleGirl 风格，但角色始终处于"下垂手臂"姿态，无持枪瞄准感
- 接龙 UI 需要在画面右侧占据空间，镜头设计必须将角色置于画面偏左（越右肩视角），两个状态都保持此构图
- 普通视角（自由视角）：相机可绕角色自由旋转，角色朝向跟移动方向
- 瞄准视角（锁定视角）：相机与角色朝向锁定，呈现 strafe 模式（B1b.3 完成朝向逻辑，本 change 仅做视觉层）

---

## Scope

### In Scope

- `UpperBodyAim` Animator Layer + Avatar Mask（上半身）
- AimIdle + 8 方向 AimWalk 2D Cartesian BlendTree
- Cinemachine 双相机（PlayerFollowCamera 调参 + PlayerAimCamera 新建）
- QF 数据流：`PlayerModel.IsAiming` → `SetAimStateCommand` → `PlayerSystem` → `AimStateChangedEvent`
- `AnimatorAimBridge` / `CameraAimBridge`（订阅 Event，驱动动画权重和相机 Priority）
- 临时输入驱动器 `TempAimInputDriver`（读 `Input.GetMouseButton(1)`，B1b.3 删除）

### Out of Scope

- Strafe 移动朝向逻辑（B1b.3）
- 射击/换弹动画（Phase 2.3）
- 瞄准 IK / `SwitchSocket` 真实实现（Phase 4）
- 准心 UI（Phase 3 副线 UI）

---

## Impact

- 修改：`Assets/_Project/Animations/Player/UnomataPlayer.controller`（新增 Layer + 参数）
- 修改：`Assets/_Project/Scenes/SampleScene.unity`（相机配置 + 新组件挂载）
- 新增：`Assets/_Project/Animations/Player/UpperBody.mask`
- 新增：`Assets/_Project/Scripts/Gameplay/Player/AnimatorAimBridge.cs`
- 新增：`Assets/_Project/Scripts/Gameplay/Player/CameraAimBridge.cs`
- 新增：`Assets/_Project/Scripts/Gameplay/Player/TempAimInputDriver.cs`
- 新增：`Assets/_Project/Scripts/Gameplay/Commands/SetAimStateCommand.cs`
- 新增：`Assets/_Project/Scripts/Gameplay/Events/AimStateChangedEvent.cs`
- 修改：`Assets/_Project/Scripts/Gameplay/GameApp.cs`（注册 IsAiming 到 PlayerModel）
- 修改：`Assets/_Project/Scripts/Gameplay/Systems/PlayerSystem.cs`（新增 SetAiming 方法）
- 修改：`Assets/_Project/Scripts/Gameplay/Models/PlayerModel.cs`（新增 IsAiming BindableProperty）

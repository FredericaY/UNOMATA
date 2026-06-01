# Tasks: unity-player-input-qf-bridge

> Phase 2.1 · B1b.3  
> 前置：B1b.2 (`unity-character-aim-layer`) 已归档  
> 下游：B1c.1 → B1c.2 → B2a

---

## 1. 前置确认

- [x] 1.1 确认 `Assets/ThirdParty/Locomotion/StarterAssets/InputSystem/StarterAssetsInputs.cs` 公开字段：`move` / `look` / `jump` / `sprint`（本 change Adapter 同步 move/jump/sprint，look 不进 PlayerInputModel）
- [x] 1.2 确认 SA `.inputactions` 文件路径 = `Assets/ThirdParty/Locomotion/StarterAssets/InputSystem/StarterAssets.inputactions`，Action Map 名 = `Player`
- [x] 1.3 确认 `PlayerArmature` 当前 `PlayerInput.Behavior` = `SendMessages`（需切换为 Invoke C# Events）
- [x] 1.4 确认 `Assets/_Project/Settings/` 目录已存在

---

## 2. .inputactions 资产复制与扩展

- [x] 2.1 `AssetDatabase.CopyAsset` 复制到 `Assets/_Project/Settings/UnomataPlayer.inputactions`（GUID: `582b9c51da566354e9db7db8d1f53081`）
- [x] 2.2 验证副本 GUID 与 SA 原文件不同 ✓
- [x] 2.3 在副本 `Player` Action Map 中新增 `Aim`（Button，Binding：`<Mouse>/rightButton`）
- [x] 2.4 新增 `Fire`（Button，Binding：`<Mouse>/leftButton`）
- [x] 2.5 验证：actions 列表 = Move/Look/Jump/Sprint/Aim/Fire ✓

---

## 3. 新建 Command 脚本

- [x] 3.1 新建 `SetMoveInputCommand.cs` ✓
- [x] 3.2 新建 `SetJumpInputCommand.cs` ✓
- [x] 3.3 新建 `SetSprintInputCommand.cs` ✓
- [x] 3.4 新建 `SetFireInputCommand.cs`（骨架）✓
- [x] 3.5 编译通过，Console 零红错 ✓

---

## 4. 新建 PlayerInputModel

- [x] 4.1 新建 `PlayerInputModel.cs`：继承 `AbstractModel`，命名空间 `Unomata.Gameplay` ✓
- [x] 4.2 5 个 `BindableProperty<>` 字段：Move/Jump/Sprint/IsAiming/Fire ✓
- [x] 4.3 `OnInit()` 空实现 ✓
- [x] 4.4 `GameApp.cs` 追加 `RegisterModel<PlayerInputModel>` ✓

---

## 5. 新建 PlayerController

- [x] 5.1 新建 `PlayerController.cs`，`[DefaultExecutionOrder(-20)]` ✓
- [x] 5.2 `_playerInput` 序列化字段，`Start()` 中获取 5 个 Action ✓
- [x] 5.3 Move performed/canceled 回调 ✓
- [x] 5.4 Jump performed/canceled 回调 ✓
- [x] 5.5 Sprint performed/canceled 回调 ✓
- [x] 5.6 Aim 回调 → SetAimStateCommand ✓
- [x] 5.7 Fire 回调 → SetFireInputCommand ✓
- [x] 5.8 OnDisable() 取消订阅 ✓

---

## 6. 新建 SAInputAdapter

- [x] 6.1 新建 `SAInputAdapter.cs`，`[DefaultExecutionOrder(-10)]` ✓
- [x] 6.2 `Start()` 获取 PlayerInputModel 和 StarterAssetsInputs ✓
- [x] 6.3 `LateUpdate()` 写 move/jump/sprint ✓
- [x] 6.4 不写 look ✓

---

## 7. 新建 StrafeController

- [x] 7.1 新建 `StrafeController.cs`，`[DefaultExecutionOrder(10)]` ✓
- [x] 7.2 `Start()` 获取 PlayerInputModel 和主相机 ✓
- [x] 7.3 `LateUpdate()` 瞄准时锁定 transform.rotation 为相机 Yaw ✓

---

## 8. 修改现有脚本

- [x] 8.1 `PlayerSystem.cs` 订阅 `_inputModel.IsAiming.Register` ✓
- [x] 8.2 `AnimatorAimBridge.cs` 改从 PlayerInputModel 读取 MoveX/MoveY ✓

---

## 9. 清理

- [x] 9.1 `TempAimInputDriver.cs` 已删除 ✓
- [x] 9.2 Missing Script（TempAimInputDriver 残留）已由场景接线时 RemoveMonoBehavioursWithMissingScript 清除 ✓

---

## 10. 场景接线

- [x] 10.1 `PlayerInput.actions` = `UnomataPlayer.inputactions` ✓
- [x] 10.2 `PlayerInput.Behavior` = `InvokeCSharpEvents` ✓
- [x] 10.3 `PlayerController` 挂载，`_playerInput` 已赋值 ✓
- [x] 10.4 `SAInputAdapter` 挂载 ✓
- [x] 10.5 `StrafeController` 挂载 ✓
- [x] 10.6 Missing Script 已清除（RemoveMonoBehavioursWithMissingScript）✓
- [x] 10.7 SampleScene 已保存 ✓

---

## 11. Play Mode 验收

- [ ] 11.1 **唯一入口验证**：在 Inspector 禁用 `PlayerController` 组件 → 按 WASD / 空格 / 右键，角色完全无响应（SAInputAdapter 无输入源，Model 全为默认值）
- [ ] 11.2 **移动**：启用后 WASD 正常移动，Shift 冲刺，角色 TPC 动画（Speed/MotionSpeed 参数）正常驱动
- [ ] 11.3 **跳跃**：空格键正常触发跳跃链路
- [ ] 11.4 **瞄准切换**：长按右键进入瞄准 → 相机推进 + 上半身持枪动画叠加；松开退出 → 相机拉远 + 上半身恢复（B1b.2 视觉验收不退化）
- [ ] 11.5 **Strafe**：瞄准状态下移动，角色身体始终朝相机方向（不转身），侧移时 BlendTree MoveX/Y 正确响应对应方向动画
- [ ] 11.6 **StarterAssets ThirdParty 未改动**：`git diff -- Assets/ThirdParty/` 输出为空
- [ ] 11.7 **Console 零红色错误**

---

## 12. TPS 朝向重构（apply 期修订：硬锁 → 死区迟滞）

> 原 7.x 的「每帧硬锁相机 Yaw」方案缺失 TPS 扭腰/迟滞手感，重构为「下半身死区迟滞 + 上半身 aim offset」双层模型。详见 design D2 修订。

- [x] 12.1 `StrafeController` 增加 `PlayerInputModel` 引用 + `_deadzoneDeg`(默认10) / `_turnSpeedDeg`(默认360) / `_moveThreshold` `SerializeField` ✓
- [x] 12.2 `StrafeController` 重写 Yaw：进入瞄准 snap → 移动锁相机 → 静止死区迟滞(`MoveTowardsAngle`)，保留 PlayerCameraRoot rotation 恢复 ✓
- [x] 12.3 `AnimatorAimBridge.LateUpdate` 恢复 `yawOffset = DeltaAngle(身体Yaw, 相机Yaw)`，与 pitch 一起叠加到 spine 骨骼 `Euler(-pitch*w, yawOffset*w, 0)` ✓
- [x] 12.4 编译通过，Console 零红错 ✓

- [ ] 12.5 Play Mode 手感验收：进入瞄准 snap、静止小幅转视角只扭腰、大幅转视角下半身追身、移动锁相机不退化
- [ ] 12.6 死区/追身速度运行时调参确认（±10° 偏小可当场加到 20~30°）



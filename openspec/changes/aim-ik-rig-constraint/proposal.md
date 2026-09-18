# Proposal: aim-ik-rig-constraint

> Phase 2.1 · B1b.4
> 前置：B1b.3 (`unity-player-input-qf-bridge`) 已归档 (2026-05-31)
> 下游：B2a；B1c.1 / B1c.2 已于 2026-05-28 归档。

## Why

B1b.3 的瞄准上半身朝向采用「手写 spine 骨骼旋转叠加」方案（`AnimatorAimBridge.LateUpdate` 用 `Quaternion.AngleAxis` 在世界空间给 `UpperChest` 叠加 yawOffset + pitch）。实测该方案有两个无法回避的问题：

1. **调参地狱**：持枪动画本身带固定 pose（R_AimIdle/AimWalk 的手臂/胸腔朝向），手写叠加必须再加一组 bias（`AimYawBias`/`AimPitchBias` + 移动态另一组）去抵消，idle 与 walk 的 pose 偏差不同还要插值，参数互相耦合，始终调不到「枪口真正对准准星」。
2. **骨骼轴向不对齐**：spine_03 的 local 轴与世界轴完全错位，只能在世界空间施加增量，但增量叠加破坏动画原有的脊椎曲线，俯仰大角度时上半身扭曲。

根因：**用代码硬掰骨骼去模拟「看向某点」，本质是在重新发明 IK**。Unity 官方 Animation Rigging 的 Multi-Aim Constraint 就是为此而生——给定一个目标点，约束骨骼链自然朝向它，权重链让脊椎平滑弯曲，且叠加在动画之上而非覆盖。项目已安装 `com.unity.animation.rigging` 1.3.1。

## What Changes

把瞄准时的**上半身朝向**从「手写 spine 叠加」改为 **Animation Rigging Multi-Aim Constraint IK**：

- 新增上半身 Aim Rig（`Rig` + 沿 Spine→Chest→UpperChest→Head 的 Multi-Aim Constraint 链），瞄准一个世界空间 `AimTarget`。
- `AimTarget` 每帧置于「相机位置 + 相机 forward × 距离」，即玩家准星指向的世界点。
- `AnimatorAimBridge` 不再手写 spine 旋转，改为：用 `IsAiming` 渐变驱动 Rig 整体权重 0↔1（进入瞄准平滑抬枪，退出平滑放下）。
- **下半身朝向逻辑（`StrafeController` 死区迟滞）保持不变**——它已正确实现「小幅转视角只扭腰、大幅转视角脚追身」，正是用户要的死区手感。IK 负责 pitch + 死区内残余 yaw 的上半身扭转，StrafeController 负责脚的 Yaw。

### 目标手感（用户定义）

- 非瞄准：现状（TPC 朝移动方向），不动。
- 进入瞄准当帧：下半身 snap 对齐相机 Yaw（StrafeController 现状），上半身 IK 权重渐入。
- Yaw：下半身死区迟滞（StrafeController）+ 上半身 IK 补足残差。
- Pitch：仅上半身 IK 跟随相机俯仰，下半身始终竖直。
- 死区：相机小幅偏转 → 仅上半身 IK 扭腰（脚不动）；大幅偏转 → StrafeController 脚追身，IK 回到中性。

## Impact

- **Specs**: `character-controller`
  - MODIFIED: `AnimatorAimBridge 上半身 Yaw 补偿 + Pitch 叠加` → 改为「Aim Rig 权重驱动」，移除手写 spine 叠加与 bias 字段
  - ADDED: 上半身 Aim Rig（Multi-Aim Constraint 链）资产与层级
  - ADDED: AimTarget 驱动器
- **代码**:
  - 修改 `Assets/_Project/Scripts/Gameplay/Player/AnimatorAimBridge.cs`：删除 spine 叠加 + bias `SerializeField`，改为 Rig 权重渐变
  - 新增 `Assets/_Project/Scripts/Gameplay/Player/AimTargetDriver.cs`：每帧更新 AimTarget 世界位置
  - `StrafeController.cs` 不改（下半身死区逻辑保留）
- **资产/场景**:
  - `PlayerArmature` 下新增 `Rig Builder` + Aim Rig 层级（GameObject `AimRig` 含 Multi-Aim Constraint）
  - 新增 `AimTarget` 子对象
  - 场景接线后需保存
- **包依赖**: `com.unity.animation.rigging` 1.3.1（已安装，无需新增）

## Non-Goals

- 不改下半身 `StrafeController` 死区迟滞逻辑
- 不改双相机切换（`CameraAimBridge`）
- 不实现武器挂点 IK / 左手扶枪（Phase 4+）
- 不改 UpperBodyAim 动画层与 BlendTree（持枪动画仍由动画层提供，IK 叠加其上）

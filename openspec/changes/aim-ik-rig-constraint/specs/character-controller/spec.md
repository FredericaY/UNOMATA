## MODIFIED Requirements

### Requirement: AnimatorAimBridge 上半身 Yaw 补偿 + Pitch 叠加

本 change（B1b.4）将职责由「手写 spine 旋转叠加」改为「驱动 Animation Rigging Aim Rig 的整体权重」。`AnimatorAimBridge` SHALL NOT 再直接旋转任何骨骼；上半身朝向（yaw 残差 + pitch）改由 Multi-Aim Constraint IK 实现（见下方 ADDED Requirement）。


`AnimatorAimBridge`（`Unomata.Gameplay`，MonoBehaviour，IController）SHALL NOT 包含任何对骨骼 `localRotation` / 世界增量旋转的写操作，SHALL NOT 包含 `AimYawBias` / `AimPitchBias` / `AimWalkYawBias` / `AimWalkPitchBias` 等瞄准校正 `SerializeField`。

`AnimatorAimBridge` SHALL 持有上半身 Aim `Rig`（`UnityEngine.Animations.Rigging.Rig`）引用，并在 `Update()`（或 `LateUpdate()`）中按 `PlayerInputModel.IsAiming.Value` 用 `Mathf.MoveTowards` 平滑驱动 `Rig.weight` 在 `0`（未瞄准）与 `1`（瞄准）之间渐变，渐变时长由 `SerializeField`（默认约 0.15s）控制。

`AnimatorAimBridge` 对 `MoveX`/`MoveY` Animator 参数的写入（从 `PlayerInputModel.Move` 读取）SHALL 保持不变（B1b.3 已立的契约）。

#### Scenario: AnimatorAimBridge 不直接旋转骨骼

- **WHEN** 代码审查 `AnimatorAimBridge.cs`
- **THEN** 文件中 SHALL NOT 包含 `Quaternion.AngleAxis` 施加到 `HumanBodyBones` 骨骼的写操作，SHALL NOT 包含 `AimYawBias` / `AimPitchBias` 字段

#### Scenario: 瞄准时 Rig 权重渐入

- **WHEN** Play Mode 下按下右键进入瞄准
- **THEN** 上半身 Aim `Rig.weight` SHALL 在约 0.15s 内从 0 平滑增至 1，松开右键后平滑回到 0

#### Scenario: MoveX/MoveY 契约不退化

- **WHEN** Play Mode 下瞄准并按 W+A
- **THEN** `Animator.GetFloat("MoveX")` 约 -1，`"MoveY"` 约 1（B1b.3 契约保持）

## ADDED Requirements

### Requirement: 上半身 Aim Rig（Animation Rigging Multi-Aim Constraint 链）

`PlayerArmature` SHALL 挂载 `RigBuilder` 组件，并包含一个名为 `AimRig` 的子 GameObject（挂 `Rig` 组件），`AimRig` 下 SHALL 包含沿脊椎链的 `MultiAimConstraint` 组件，约束骨骼自下而上为：`Spine` → `Chest` → `UpperChest`（可含 `Head`），每个 Constraint 的 `Source Objects` 指向同一个 `AimTarget` Transform。

每个 `MultiAimConstraint` SHALL：
- `Aimed Axis` 设为使骨骼「正面 / 枪口方向」朝向目标的轴（依模型实际骨骼朝向标定，验收以「枪口对准准星」为准）。
- 沿链分配递增权重（下层脊椎分担少、上层分担多），使整条脊椎平滑弯曲而非单骨硬拐。
- 设置合理的 `World Up` 与角度限制，避免大俯仰时上半身翻转。

`RigBuilder.layers` SHALL 包含 `AimRig`，初始 `Rig.weight = 0`。

#### Scenario: 瞄准时枪口随准星俯仰

- **WHEN** Play Mode 下瞄准（Rig 权重为 1），上下俯仰相机
- **THEN** 角色上半身（含武器）SHALL 随相机俯仰朝准星方向，脊椎平滑弯曲，下半身保持竖直不前倾后仰

#### Scenario: 瞄准时枪口对准准星方向

- **WHEN** Play Mode 下瞄准，准星指向场景中某点
- **THEN** 角色枪口朝向 SHALL 与准星指向一致（无需额外 bias 校正），无脊椎扭曲/翻转

#### Scenario: 未瞄准时 IK 不生效

- **WHEN** 未瞄准（Rig 权重为 0）
- **THEN** 上半身 SHALL 完全由动画驱动，无任何 IK 朝向偏移

### Requirement: AimTarget 驱动器

`Unomata.Gameplay` 命名空间 SHALL 定义 `AimTargetDriver` 类（MonoBehaviour，`[DefaultExecutionOrder]` 早于 RigBuilder 求值，约 `-5`），每帧将 `AimTarget` Transform 的世界位置设为「瞄准相机位置 + 相机 forward × 距离」（距离为 `SerializeField`，默认约 50m），使 Multi-Aim Constraint 朝向玩家准星指向的世界点。

`AimTargetDriver` SHALL 使用与瞄准准星一致的相机（`PlayerAimCamera` 或主相机）作为方向源，距离足够远以使 IK 朝向近似平行于视线。

#### Scenario: AimTarget 跟随视线

- **WHEN** Play Mode 下转动相机
- **THEN** `AimTarget` 世界位置 SHALL 实时更新到相机前方视线上的点，上半身 IK 随之朝向该点

#### Scenario: AimTarget 距离可调

- **WHEN** 在 Inspector 调整 `AimTargetDriver` 的距离字段
- **THEN** AimTarget 与角色的距离 SHALL 相应变化，不抛异常

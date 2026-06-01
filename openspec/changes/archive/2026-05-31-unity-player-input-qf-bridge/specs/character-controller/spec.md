## ADDED Requirements

### Requirement: AnimatorAimBridge MoveX/Y 从 PlayerInputModel 读取

B1b.2 中 `AnimatorAimBridge.Update()` 临时直接读 `Input.GetAxis("Horizontal/Vertical")` 写入 `MoveX`/`MoveY` Animator 参数。本 change 后，此临时做法 SHALL 被替换为从 `PlayerInputModel.Move` 读取。

`AnimatorAimBridge.Update()` 中写 MoveX/MoveY 的代码块 SHALL 改为：
```
_animator.SetFloat("MoveX", _inputModel.Move.Value.x);
_animator.SetFloat("MoveY", _inputModel.Move.Value.y);
```
（`_inputModel` 为 `this.GetModel<PlayerInputModel>()`，在 `Start` 中获取）

代码中 SHALL NOT 存在任何 `Input.GetAxis` 调用（`TempAimInputDriver` 删除后不得有任何直接读取 Unity 旧 Input System 的代码保留在 Player/ 目录下的生产脚本中）。

#### Scenario: 瞄准移动时 BlendTree 参数由 PlayerInputModel 驱动

- **WHEN** Play Mode 下瞄准状态下按 W（前进）+ A（左移）
- **THEN** `Animator.GetFloat("MoveX")` 应为约 -1，`"MoveY"` 应为约 1，上半身播放 `AimWalk_FL` 对应方向的 BlendTree 节点

#### Scenario: AnimatorAimBridge 无 Input.GetAxis 调用

- **WHEN** 代码审查 `AnimatorAimBridge.cs`
- **THEN** 文件中 SHALL NOT 包含 `Input.GetAxis` 或 `Input.GetButton`（旧 Input System API）字符串

---

### Requirement: AnimatorAimBridge 上半身 Yaw 补偿 + Pitch 叠加

`AnimatorAimBridge.LateUpdate()` SHALL 在瞄准（UpperBodyAim 层权重 `w > 0`）时对 spine 骨骼（`HumanBodyBones.UpperChest`，回退 `Chest`）叠加两个旋转分量，实现 TPS 上半身 aim offset：

- `yawOffset = Mathf.DeltaAngle(transform.eulerAngles.y, Camera.main.transform.eulerAngles.y)`：补偿下半身（由 `StrafeController` 控制）与相机的剩余 Yaw 差，实现「扭腰跟枪」。死区内该残差即上半身扭转量。
- `pitch`：相机俯仰角，`Mathf.Clamp` 限幅 `±_maxPitchDeg`（`SerializeField`，默认 50°），实现上半身随相机俯仰。

叠加方式 SHALL 在**世界空间**用 `Quaternion.AngleAxis` 施加增量旋转，**不得**用骨骼 `localRotation *= Euler(...)`——实测 spine_03 的 local 轴 X≈世界下方、Y≈角色前方、Z≈角色左方，与世界轴完全不对齐，local Euler 会把 pitch 误施加成水平偏转，导致上半身「偏左」、俯仰错位。正确写法：
```
_chestBone.rotation = Quaternion.AngleAxis(yawOffset * w, Vector3.up)
                    * Quaternion.AngleAxis(-pitch * w, transform.right)
                    * _chestBone.rotation;
```
（`w` 为 UpperBodyAim 层权重；Yaw 绕世界 Up、Pitch 绕角色 Right、`-pitch` 为抬头方向）下半身 SHALL NOT 参与 Pitch（人物不整体前倾后仰）。

B1b.2 注释中描述但实现缺失的 `yawOffset` 逻辑 SHALL 在本 change 恢复。

#### Scenario: 静止死区内上半身扭腰跟枪

- **WHEN** 瞄准静止，相机相对身体水平偏转约 8°（死区 10° 内）
- **THEN** spine 骨骼 SHALL 叠加约 8° 的 yawOffset，上半身（含双臂/武器）扭向相机，下半身 transform 不动

#### Scenario: 上半身随相机俯仰、下半身不参与

- **WHEN** 瞄准时相机上下俯仰
- **THEN** spine 骨骼 SHALL 叠加对应 pitch（限幅 ±50°），角色下半身 SHALL 保持竖直（不前倾后仰）

---

### Requirement: TempAimInputDriver 从项目中删除

`Assets/_Project/Scripts/Gameplay/Player/TempAimInputDriver.cs` SHALL 被删除（通过 `AssetDatabase.DeleteAsset`）。删除后：
- `Assets/` 目录中 SHALL NOT 存在 `TempAimInputDriver.cs`
- Scene 中 `PlayerArmature` 的组件列表 SHALL NOT 包含 `TempAimInputDriver` 组件
- 所有输入入口以 `PlayerController` 为唯一来源

#### Scenario: TempAimInputDriver 文件不存在

- **WHEN** 在 Unity Editor Project 视图搜索 `TempAimInputDriver`
- **THEN** SHALL 无搜索结果

#### Scenario: PlayerArmature 无 TempAimInputDriver 组件

- **WHEN** 选中 SampleScene 中 PlayerArmature，查看 Inspector 组件列表
- **THEN** 组件列表中 SHALL NOT 包含 `TempAimInputDriver`

---

### Requirement: PlayerInput Behavior 改为 Invoke C# Events 并接线 PlayerController

SampleScene 中 `PlayerArmature` 上的 `PlayerInput` 组件 SHALL 满足：
- `Behavior` 字段 SHALL 设为 `Invoke C# Events`（或 `Invoke Unity Events`，两者均切断 SA 默认 `SendMessage` 路由）
- `Actions` 字段 SHALL 引用 `Assets/_Project/Settings/UnomataPlayer.inputactions`
- `StarterAssetsInputs.OnMove / OnJump / OnSprint` 等 SA 默认回调 SHALL NOT 直接被 `PlayerInput` 事件系统触发（已由 `SAInputAdapter` 统一替代）

#### Scenario: PlayerInput Behavior 已切换

- **WHEN** 在 Unity Editor 选中 PlayerArmature 查看 PlayerInput 组件
- **THEN** Behavior 字段显示 `Invoke C# Events`（或 `Invoke Unity Events`，而非 `Send Messages` / `Broadcast Messages`）

#### Scenario: 移动输入经 QF 链路完整流通至 TPC

- **WHEN** Play Mode 下按 W 键
- **THEN** 链路 `PlayerInput → PlayerController.OnMove → SetMoveInputCommand → PlayerInputModel.Move → SAInputAdapter → StarterAssetsInputs.move → ThirdPersonController` SHA 完整生效，角色向前移动

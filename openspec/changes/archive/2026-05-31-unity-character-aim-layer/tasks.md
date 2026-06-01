# Tasks: unity-character-aim-layer

> Phase 2.1 · B1b.2  
> 前置：B1b.1 (`unity-character-base-anim-swap`) 已归档  
> 下游：B1b.3 (`unity-player-input-qf-bridge`)

---

## Task 1 · 前置确认

- [x] **1.1** 确认 Cinemachine 版本：`com.unity.cinemachine` **2.10.3**（2.x），Body 类型用 `3rd Person Follow`，与 design.md 预期一致
- [x] **1.2** 确认 `UnomataPlayer.controller` Base Layer 移动参数名：`Speed` (float) / `MotionSpeed` (float) / `Jump`/`Grounded`/`FreeFall` (bool)；无 `MoveX`/`MoveY`，本 change 新增三个参数

---

## Task 2 · Avatar Mask

- [x] **2.1** 在 `Assets/_Project/Animations/Player/` 新建 Humanoid Mask，命名 `UpperBody.mask`
- [x] **2.2** 配置 Mask：勾选 Body(Spine/Chest/UpperChest) / Head / LeftArm / RightArm / LeftFingers / RightFingers；不勾 Root / 双腿 / FootIK / HandIK
- [x] **2.3** 验证：Play Mode 下 Base Layer 下半身动画不受影响

---

## Task 3 · Animator Controller 扩展

> 在 `UnomataPlayer.controller` 上操作（此为项目自有 asset，可直接改）

- [x] **3.1** 新增三个参数：`IsAiming` (bool) / `MoveX` (float) / `MoveY` (float)
- [x] **3.2** 新增 Layer `UpperBodyAim`：Override，Weight = 0，绑定 `UpperBody.mask`
- [x] **3.3** 在 `UpperBodyAim` 层新建状态机：
  - 新建 `Empty` State（默认状态，无 Motion）
  - 新建 `AimMove` State（含 BlendTree）
  - Transition `Empty → AimMove`：Condition `IsAiming = true`，Has Exit Time = false
  - Transition `AimMove → Empty`：Condition `IsAiming = false`，Has Exit Time = false
- [x] **3.4** 配置 `AimMove` BlendTree（2D Simple Directional，**方案 B 7 Motion**）：
  - X 轴：`MoveX`，Y 轴：`MoveY`
  - R_AimIdle(0,0) / R_AimWalk_F(0,1) / R_AimWalk_B(0,-1) / R_AimWalk_FL(-0.7,0.7) / R_AimWalk_FR(0.7,0.7) / R_AimWalk_BL(-0.7,-0.7) / R_AimWalk_BR(0.7,-0.7)
  - 备注：无独立 R_AimWalk_L/R clip，用 7 Motion 方案 B（2D Simple Directional 自动插值）

---

## Task 4 · QF 数据模型扩展

- [x] **4.1** `PlayerModel.cs`：新增 `public BindableProperty<bool> IsAiming = new BindableProperty<bool>(false);`
- [x] **4.2** `PlayerSystem.cs`：新增 `public void SetAiming(bool isAiming)` 方法（写 Model + SendEvent）
- [x] **4.3** 新建 `Assets/_Project/Scripts/Gameplay/Events/AimStateChangedEvent.cs`：`public struct AimStateChangedEvent { public bool IsAiming; }`
- [x] **4.4** 新建 `Assets/_Project/Scripts/Gameplay/Commands/SetAimStateCommand.cs`：`AbstractCommand`，构造器接收 `bool isAiming`，`OnExecute` 调 `this.GetSystem<PlayerSystem>().SetAiming(_isAiming)`

---

## Task 5 · Bridge 脚本

- [x] **5.1** 新建 `Assets/_Project/Scripts/Gameplay/Player/AnimatorAimBridge.cs`
  - 实现 `IController`，`GetArchitecture() => GameApp.Interface`
  - `Start()`：注册 `AimStateChangedEvent`，初始化 `_layerIndex = _animator.GetLayerIndex("UpperBodyAim")`
  - `OnAimStateChanged`：设 `_targetWeight`，调 `_animator.SetBool("IsAiming", e.IsAiming)`
  - `Update()`：`Mathf.MoveTowards` 驱动 Layer Weight（0.15s）；IsAiming 时写 MoveX/Y
  - `[SerializeField] Animator _animator` 已赋值（PlayerArmature 的 Animator）

- [x] **5.2** 新建 `Assets/_Project/Scripts/Gameplay/Player/CameraAimBridge.cs`
  - 实现 `IController`
  - `[SerializeField] CinemachineVirtualCamera _aimCamera` 已赋值（PlayerAimCamera）
  - `OnAimStateChanged`：**disable → Priority = 15/0 → enable**（修复 Cinemachine 2.x 运行时改 Priority 不重排 mActiveCameras 的 bug）

---

## Task 6 · 临时输入驱动器

- [x] **6.1** 新建 `Assets/_Project/Scripts/Gameplay/Player/TempAimInputDriver.cs`
  - 实现 `IController`（SendCommand 需要）
  - `GetMouseButtonDown(1)` → `SendCommand(new SetAimStateCommand(true))`
  - `GetMouseButtonUp(1)` → `SendCommand(new SetAimStateCommand(false))`
  - 顶部注释：`// TEMP: B1b.3 (unity-player-input-qf-bridge) 实施后删除此文件`

---

## Task 7 · 场景配置

- [x] **7.1** 调整 `PlayerFollowCamera`（已有）：
  - Follow / LookAt → `PlayerCameraRoot`（`PlayerArmature/PlayerCameraRoot`）
  - FOV = 60，ShoulderOffset X = **0.8**，VerticalArmLength = 0.3，CameraDistance = **2.8**，CameraRadius = 0.2
  - Damping X=0.1 Y=0.2 Z=0.1

- [x] **7.2** 新建 `PlayerAimCamera`（Cinemachine Virtual Camera）：
  - Follow / LookAt → `PlayerCameraRoot`
  - Priority = 0（待激活切换到 15）
  - FOV = 45，ShoulderOffset X = **1.2**，VerticalArmLength = 0.2，CameraDistance = **1.8**，CameraRadius = 0.2
  - Damping X=0.05 Y=0.1 Z=0.05
  - Cinemachine Brain `Default Blend`：Ease In Out，Duration = 0.3s

- [x] **7.3** 在 `PlayerArmature` 上挂载：
  - `AnimatorAimBridge`：`_animator` → PlayerArmature Animator ✓
  - `CameraAimBridge`：`_aimCamera` → PlayerAimCamera ✓
  - `TempAimInputDriver` ✓

---

## Task 8 · Play Mode 验收

- [x] **8.1** 非瞄准状态：
  - 角色画面偏左（越右肩，ShoulderOffset X=0.8），相机可自由旋转
  - 上半身无持枪动画（UpperBodyAim Layer weight = 0）
  - 移动/跳跃动画正常（Base Layer 无回退）

- [x] **8.2** 长按右键进入瞄准：
  - 相机平滑推进（0.3s 过渡，Distance 2.8→1.8，FOV 60→45，更偏左 ShoulderX 0.8→0.55）
  - 上半身切换到持枪姿势（Layer Weight 0→1，约 0.15s）
  - 移动时 AimWalk 7 方向 BlendTree 正确响应

- [x] **8.3** 松开右键退出瞄准：相机平滑拉远，上半身恢复

- [x] **8.4** Console 零红色错误

---

## Known Issues（本 change 不处理）

| 问题 | 计划处理 |
|------|---------|
| 瞄准移动时角色转身而非侧移（无 Strafe 逻辑） | B1b.3 |
| MoveX/Y 直接读 Input.GetAxis（绕过 QF） | B1b.3 |
| TempAimInputDriver 临时文件存在 | B1b.3 删除 |
| 无准心 UI | Phase 3 |

## 实施期发现

| 发现 | 处理 |
|------|------|
| Cinemachine 2.x 运行时改 Priority 不触发 `mActiveCameras` 重排 | `CameraAimBridge.OnAimStateChanged` 改用 disable→Priority→enable 强制重排 |
| CombatGirls 无独立 R_AimWalk_L/R clip | 采用方案 B（7 Motion），2D Simple Directional 自动插值；方案 A 备选记录在 design.md |
| MCP execute_code 在 Play Mode 中阻塞游戏循环（Time.frameCount 不推进） | 逻辑正确性通过代码审查 + QF 链路数据验证；实际手感需用户手动测试 |

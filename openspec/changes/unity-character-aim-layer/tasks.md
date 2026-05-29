# Tasks: unity-character-aim-layer

> Phase 2.1 · B1b.2  
> 前置：B1b.1 (`unity-character-base-anim-swap`) 已归档  
> 下游：B1b.3 (`unity-player-input-qf-bridge`)

---

## Task 1 · 前置确认

- [ ] **1.1** 确认 Cinemachine 版本：Package Manager 查看 `com.unity.cinemachine`，记录版本（2.x 还是 3.x）
  - 预期：2.x（Unity 2022.3 LTS 默认），Body 类型用 `3rd Person Follow`
  - 若为 3.x：Body 类型改为 `CinemachineThirdPersonFollow`，参数名有差异，施工前更新 design.md
- [ ] **1.2** 确认 `UnomataPlayer.controller` 中 Base Layer 移动参数名
  - 预期：`Speed` (float)，无 `MoveX` / `MoveY`
  - 本 change 需新增 `MoveX` / `MoveY` / `IsAiming` 三个参数

---

## Task 2 · Avatar Mask

- [ ] **2.1** 在 `Assets/_Project/Animations/Player/` 新建 Humanoid Mask，命名 `UpperBody.mask`
- [ ] **2.2** 配置 Mask：勾选 Spine / Chest / UpperChest / Neck / Head / 双肩 / 双上臂 / 双前臂 / 双手；不勾 Hips / 双腿 / Root
- [ ] **2.3** 验证：Play Mode 下 Base Layer 下半身动画不受影响

---

## Task 3 · Animator Controller 扩展

> 在 `UnomataPlayer.controller` 上操作（此为项目自有 asset，可直接改）

- [ ] **3.1** 新增三个参数：`IsAiming` (bool) / `MoveX` (float) / `MoveY` (float)
- [ ] **3.2** 新增 Layer `UpperBodyAim`：Override，Weight = 0，绑定 `UpperBody.mask`
- [ ] **3.3** 在 `UpperBodyAim` 层新建状态机：
  - 新建 `Empty` State（空，无 Motion）
  - 新建 `AimMove` State（含 BlendTree，下一步配置）
  - 添加 Transition：`Empty → AimMove`，Condition: `IsAiming = true`，Has Exit Time = false
  - 添加 Transition：`AimMove → Empty`，Condition: `IsAiming = false`，Has Exit Time = false
- [ ] **3.4** 配置 `AimMove` BlendTree（2D Simple Directional）：
  - X 轴：`MoveX`，Y 轴：`MoveY`
  - 9 个 Motion 及坐标（见 design.md BlendTree 表格）
  - 全部引用 `CombatGirls/RifleGirl/Animations/Aiming/` 下对应 fbx 的主 clip

---

## Task 4 · QF 数据模型扩展

- [ ] **4.1** `PlayerModel.cs`：新增 `public BindableProperty<bool> IsAiming { get; } = new BindableProperty<bool>(false);`
- [ ] **4.2** `PlayerSystem.cs`：新增 `public void SetAiming(bool isAiming)` 方法（写 Model + SendEvent）
- [ ] **4.3** 新建 `Assets/_Project/Scripts/Gameplay/Events/AimStateChangedEvent.cs`：`public struct AimStateChangedEvent { public bool IsAiming; }`
- [ ] **4.4** 新建 `Assets/_Project/Scripts/Gameplay/Commands/SetAimStateCommand.cs`：`AbstractCommand`，构造器接收 `bool isAiming`，`OnExecute` 调 `this.GetSystem<PlayerSystem>().SetAiming(_isAiming)`

---

## Task 5 · Bridge 脚本

- [ ] **5.1** 新建 `Assets/_Project/Scripts/Gameplay/Player/AnimatorAimBridge.cs`
  - 实现 `IController`，`GetArchitecture() => GameApp.Interface`
  - `Start()`：`this.RegisterEvent<AimStateChangedEvent>(OnAimStateChanged).UnRegisterWhenGameObjectDestroyed(gameObject)`
  - `OnAimStateChanged`：记录目标权重（`_targetWeight = e.IsAiming ? 1f : 0f`）
  - `Update()`：
    - `Mathf.MoveTowards` 驱动 Layer Weight（速率 = 1f / 0.15f）
    - 若 IsAiming：每帧写 `MoveX` = `Input.GetAxis("Horizontal")`，`MoveY` = `Input.GetAxis("Vertical")`（B1b.3 后改从 PlayerInputModel 读）
  - 需序列化字段：`[SerializeField] Animator _animator`（引用 PlayerArmature 上的 Animator）
  - Layer Index 用 `_animator.GetLayerIndex("UpperBodyAim")`

- [ ] **5.2** 新建 `Assets/_Project/Scripts/Gameplay/Player/CameraAimBridge.cs`
  - 实现 `IController`
  - 需序列化字段：`[SerializeField] CinemachineVirtualCamera _aimCamera`（引用 PlayerAimCamera）
  - `Start()`：注册 `AimStateChangedEvent`
  - `OnAimStateChanged`：`_aimCamera.Priority = e.IsAiming ? 15 : 0`

---

## Task 6 · 临时输入驱动器

- [ ] **6.1** 新建 `Assets/_Project/Scripts/Gameplay/Player/TempAimInputDriver.cs`
  - 普通 MonoBehaviour（**不实现 IController**）
  - `Update()`：
    - `Input.GetMouseButtonDown(1)` → `this.SendCommand(new SetAimStateCommand(true))`（需 using QFramework）
    - `Input.GetMouseButtonUp(1)` → `this.SendCommand(new SetAimStateCommand(false))`
  - 顶部注释：`// TEMP: B1b.3 (unity-player-input-qf-bridge) 实施后删除此文件`

> 注意：`TempAimInputDriver` 调 `this.SendCommand` 需实现 `IController` 接口（QFramework 要求）。  
> 改为：实现 `IController`，`GetArchitecture() => GameApp.Interface`

---

## Task 7 · 场景配置

> Unity Editor 内操作，操作后提示保存场景

- [ ] **7.1** 调整 `PlayerFollowCamera`（已有）：
  - Body → 3rd Person Follow：`Shoulder Offset X = 0.4`，`Vertical Arm Length = 0.3`，`Camera Distance = 4.5`，`Camera Radius = 0.2`
  - Damping：X=0.1，Y=0.2，Z=0.1
  - FOV = 60

- [ ] **7.2** 新建 `PlayerAimCamera`（Cinemachine Virtual Camera）：
  - Follow / LookAt：与 `PlayerFollowCamera` 相同目标（`CinemachineCameraTarget`）
  - Priority = 0
  - Body → 3rd Person Follow：`Shoulder Offset X = 0.35`，`Vertical Arm Length = 0.2`，`Camera Distance = 2.8`，`Camera Radius = 0.2`
  - Damping：X=0.05，Y=0.1，Z=0.05
  - FOV = 45
  - Cinemachine Brain `Default Blend`：保持默认 Ease In Out，Duration = 0.3s

- [ ] **7.3** 在 `PlayerArmature` 上（或新建专用 GameObject）挂载：
  - `AnimatorAimBridge`：`_animator` 字段引用 `PlayerArmature` 上的 Animator
  - `CameraAimBridge`：`_aimCamera` 字段引用 `PlayerAimCamera`
  - `TempAimInputDriver`

---

## Task 8 · Play Mode 验收

- [ ] **8.1** 非瞄准状态：
  - 角色画面偏左（越右肩），相机可自由旋转
  - 上半身无持枪动画（UpperBodyAim Layer weight = 0）
  - 移动/跳跃动画正常（Base Layer 无回退）

- [ ] **8.2** 长按右键进入瞄准：
  - 相机平滑推进（0.3s 过渡，Distance 4.5→2.8，FOV 60→45）
  - 角色仍在画面偏左，镜头更贴近
  - 上半身切换到持枪姿势（Layer Weight 0→1，约 0.15s）
  - 移动时 AimWalk 8 方向 BlendTree 正确响应

- [ ] **8.3** 松开右键退出瞄准：
  - 相机平滑拉远（0.3s 过渡）
  - 上半身恢复（Layer Weight 1→0）

- [ ] **8.4** 已知遗留（记录，不阻塞）：
  - 瞄准时移动，角色会转身面朝移动方向（Strafe 朝向逻辑待 B1b.3）
  - 无准心 UI（待 Phase 3）

- [ ] **8.5** Console 零红色错误

---

## Known Issues（本 change 不处理）

| 问题 | 计划处理 |
|------|---------|
| 瞄准移动时角色转身而非侧移（无 Strafe 逻辑） | B1b.3 |
| MoveX/Y 直接读 Input.GetAxis（绕过 QF） | B1b.3 |
| TempAimInputDriver 临时文件存在 | B1b.3 删除 |
| 无准心 UI | Phase 3 |

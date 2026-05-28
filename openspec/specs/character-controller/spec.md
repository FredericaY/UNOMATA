### Requirement: CombatGirls 模型骨骼与 StarterAssets 兼容性明确

CombatGirls RifleGirl 角色模型 SHALL 完成与 StarterAssets Third Person Controller 的兼容性测试，并得出明确的组合方案结论（方案 A/B/C 之一）。

#### Scenario: 确定方案 B（需打补丁）

- **WHEN** 替换后存在小问题（Avatar Mask 冲突、动画层需调整等），但无根本性冲突
- **THEN** 记录「方案 B 可行」，改动清单写入 DEVELOPMENT_PLAN.md Phase 2 任务下，DEPENDENCIES.md 状态更新为「已验证-方案B」

> **实际结论（Phase 0 验证）**：方案 B。两者均 Humanoid Rig，Mecanim 自动重定向，基础兼容。需添加上半身 Animation Layer + Avatar Mask 用于持枪动画，替换视觉模型 Mesh + Avatar。

---

### Requirement: SampleScene 包含可 Play 的角色 demo

验证完成后，`Assets/_Project/Scenes/SampleScene.unity` SHALL 包含一个可直接进入 Play Mode 运行的角色控制 demo，支持 WASD 移动、鼠标控制相机。

#### Scenario: SampleScene 可直接 Play

- **WHEN** 在 Unity Editor 中打开 SampleScene 并点击 Play
- **THEN** 进入运行状态无崩溃，角色出现在场景中

#### Scenario: 基础移动输入正常

- **WHEN** Play Mode 下按 WASD 键
- **THEN** 角色在场景中移动，相机随鼠标转动

---

### Requirement: 验证所用包依赖在 manifest.json 中完整

TODO Task 2 所依赖的 Unity 包（Cinemachine、Input System）SHALL 已在 `Packages/manifest.json` 中存在。`com.unity.animation.rigging` 应在本阶段添加（瞄准 IK 依赖）。

#### Scenario: manifest.json 包含所有依赖

- **WHEN** 验证开始前检查 manifest.json
- **THEN** `com.unity.cinemachine`、`com.unity.inputsystem`、`com.unity.animation.rigging` 均存在

---

### Requirement: DEPENDENCIES.md 资产状态更新

`Docs/DEPENDENCIES.md` 的 Asset Store 资产表 SHALL 更新 CombatGirls 和 StarterAssets 的状态列，填入「已验证-方案X」或「不兼容」。

#### Scenario: 资产状态列完整

- **WHEN** 验证和文档更新完成
- **THEN** CombatGirls 和 StarterAssets 行的状态列均不为空

---

### Requirement: RifleGirl 模型正确嵌入 PlayerArmature

`PlayerArmature` 根对象 SHALL 以子对象形式包含 `Rifle_Full_Body.prefab` 实例，且 local position / local rotation 均为零（与父对象对齐）。

#### Scenario: RifleGirl 模型渲染正常

- **WHEN** 在 Unity Editor 中打开 SampleScene
- **THEN** PlayerArmature 层级下可见 Rifle_Full_Body 子对象，Renderer 激活，无紫色材质错误

---

### Requirement: StarterAssets 原视觉子对象禁用保留

原 StarterAssets 视觉子对象（`PlayerArmature/Geometry`）SHALL 被 `SetActive(false)` 禁用，不得删除，以保留回退能力。

#### Scenario: 原视觉对象存在但不可见

- **WHEN** 在 Hierarchy 面板查看 PlayerArmature 子对象
- **THEN** StarterAssets 原视觉子对象可见且 Active 状态为 false（灰显）

---

### Requirement: PlayerArmature Animator 使用 RifleGirl Avatar

`PlayerArmature` 根对象上的 `Animator` 组件 SHALL 使用 `Rifle_Full_Body.FBX` 对应的 Humanoid Avatar Asset（`Humanoid_FAvatar`），以启用正确的 Humanoid Retargeting。

#### Scenario: Animator Avatar 已切换

- **WHEN** 选中 PlayerArmature 查看 Animator 组件
- **THEN** Avatar 字段显示为 RifleGirl Humanoid Avatar（`Humanoid_FAvatar`，非 StarterAssets 原 `ArmatureAvatar`）

#### Scenario: 移动动画通过 Retargeting 正常播放

- **WHEN** Play Mode 下按 WASD 键
- **THEN** RifleGirl 模型播放对应的移动动画，动作自然无严重变形

---

### Requirement: MagicaCloth2 布料物理正常模拟

`Rifle_Dress` 和 `Rifle_Jacket` 上的 MagicaCloth2 组件 SHALL 在 Play Mode 下正常模拟布料物理。

#### Scenario: 布料随动作自然摆动

- **WHEN** Play Mode 下角色移动或静止
- **THEN** 裙摆和夹克布料显示出物理模拟效果（随动作自然摆动，无穿模警告日志）

---

### Requirement: 场景中唯一 AudioListener

SampleScene SHALL 只包含一个激活的 AudioListener 组件，避免 Unity 多 AudioListener 警告。

#### Scenario: 无 AudioListener 重复警告

- **WHEN** 进入 Play Mode
- **THEN** Console 中不出现 "There are 2 audio listeners in the scene" 黄色警告

---

### Requirement: 场景中唯一 Main Camera

SampleScene SHALL 只保留一个主相机对象（含 Cinemachine Brain），多余的 Main Camera GameObject 需被删除。

#### Scenario: 场景只有一个相机

- **WHEN** 在 Hierarchy 中搜索 "Camera"
- **THEN** 只有一个包含 Camera 组件的 Main Camera 对象（Cinemachine 虚拟相机不含 Camera 组件，不计入）

---

### Requirement: 项目自有 Animator Controller 资产存在且独立 GUID

`Assets/_Project/Animations/Player/UnomataPlayer.controller` SHALL 作为项目自有的 Animator Controller 资产存在，由 `AssetDatabase.CopyAsset` 从 `StarterAssetsThirdPerson.controller` 复制而来，拥有独立 GUID，与 `Assets/ThirdParty/Locomotion/StarterAssets/` 下任何文件无引用依赖。

#### Scenario: 资产文件就位

- **WHEN** 在 Unity Editor Project 视图中浏览 `Assets/_Project/Animations/Player/`
- **THEN** 存在 `UnomataPlayer.controller` 资产，且其 .meta 文件 GUID 与 `StarterAssetsThirdPerson.controller.meta` 不同

#### Scenario: 第三方原文件未被改动

- **WHEN** 完成本 change 后执行 `git status -- Assets/ThirdParty/Locomotion/StarterAssets/`
- **THEN** 输出为空（无任何修改）

---

### Requirement: Base Layer 动画素材切到 RifleGirl + FemaleRunnerAnimset 风格

`UnomataPlayer.controller` 的 Base Layer SHALL 把 6 个核心 Motion 槽切换为 RifleGirl 与 FemaleRunnerAnimset 提供的素材，状态机拓扑、参数、过渡条件保持与 `StarterAssetsThirdPerson.controller` 完全一致。

#### Scenario: Idle Walk Run BlendTree 三档 Motion 切换正确

- **WHEN** 在 Unity Editor 打开 `UnomataPlayer.controller` 的 Base Layer，定位到 `Idle Walk Run Blend` State 内嵌的 BlendTree
- **THEN** BlendTree 三个子 motion 分别为：
  - threshold = 0 → `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Idle.fbx` 内主 clip `Idle`（3.000s, looping）
  - threshold = 2 → `R_Walk.fbx` 内主 clip `Walk`（1.133s, looping）
  - threshold = 6 → `R_Run.fbx` 内主 clip `Run`（0.667s, looping）

---

### Requirement: 跳跃链路三状态 Motion 切换正确

`UnomataPlayer.controller` Base Layer 的 `JumpStart` / `InAir` / `JumpLand` 三个 State SHALL 把 Motion 切换为 RifleGirl + FemaleRunnerAnimset 系列素材；其中 JumpStart 改用滞空姿态素材（R_Jump_AirR），让其视觉职责降级为"滞空姿态预览"，与 InAir 的 R_Jump_AirL 配对（同源同设计）。

#### Scenario: JumpStart / InAir / JumpLand 三状态 Motion 引用正确

- **WHEN** 在 Base Layer 定位到 `JumpStart` / `InAir` / `JumpLand` State
- **THEN** 三状态满足：
  - JumpStart（AnimationClip 类型）的 Motion 字段为 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/Jump/R_Jump_AirR.fbx` 内主 clip `R_Jump_AirR`（1.167s）
  - InAir（AnimationClip 类型）的 Motion 字段为 `FemaleRunnerAnimset/Animations_Rifle/Jumps/Jump/R_Jump_AirL.fbx` 内主 clip `R_Jump_AirL`（1.167s）
  - JumpLand（**BlendTree 类型，3 children，按 Speed 参数 0/2/6 分站立/行走/奔跑落地**）的子 motion 分别为：
    - threshold = 0 → `FemaleRunnerAnimset/Animations_Rifle/Jumps/Land/R_Land_2h.fbx` 内主 clip `R_Land_2h`（1.167s）
    - threshold = 2 → `FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun1.fbx` 内主 clip `R_Land_ToRun1`（0.600s）
    - threshold = 6 → `FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun3.fbx` 内主 clip `R_Land_ToRun3`（0.867s）

#### Scenario: Base Layer 仅含 4 个 State 无 Fly

- **WHEN** 检查 `UnomataPlayer.controller` Base Layer 顶层 stateMachine 的 states 数组
- **THEN** 数组长度为 4，名称分别为 `Idle Walk Run Blend` / `JumpStart` / `InAir` / `JumpLand`，无任何 `Fly` 命名 State（与 SA 原 controller 拓扑一致）

#### Scenario: 状态机参数与拓扑无变化

- **WHEN** 对比 `UnomataPlayer.controller` 与 `StarterAssetsThirdPerson.controller` 的 Animator 参数列表与状态机结构
- **THEN** 参数名 / 类型完全一致（`Speed` float, `Jump` bool, `Grounded` bool, `FreeFall` bool, `MotionSpeed` float），所有 State / SubStateMachine / Transition 数量与连接关系一致，每个 Transition 的 `HasExitTime` / `Conditions` 一致

#### Scenario: 不引用 fbx 内 __preview__ 副本

- **WHEN** 检查任一 Motion 引用的 fileID 与 clip 名
- **THEN** clip 名不以 `__preview__` 开头（必须引主 clip）

---

### Requirement: PlayerArmature.Animator 使用项目自有 Controller

SampleScene 内 `PlayerArmature` 根对象的 `Animator` 组件 SHALL 把 `runtimeAnimatorController` 字段切换为 `Assets/_Project/Animations/Player/UnomataPlayer.controller`，不再引用 StarterAssets 自带 controller。

#### Scenario: Animator Controller 字段已切换

- **WHEN** 选中 SampleScene 中 PlayerArmature，查看 Animator 组件
- **THEN** Controller 字段显示为 `UnomataPlayer`（非 `StarterAssetsThirdPerson`）

#### Scenario: B1a 已立的 Animator 契约不退化

- **WHEN** 检查 PlayerArmature.Animator 的 Avatar 字段、PlayerArmature 子对象的 Geometry 激活状态、Rifle_Full_Body 子对象的 MagicaCloth2 组件
- **THEN** Avatar 仍为 `Humanoid_FAvatar`、Geometry 仍 `SetActive(false)`、MagicaCloth2 组件保持启用

---

### Requirement: Play Mode 下 RifleGirl 风格基础动画正确播放

进入 Play Mode 后，PlayerArmature SHALL 在站立 / 行走 / 奔跑 / 起跳 / 空中 / 落地全部状态下播放 RifleGirl 与 FemaleRunnerAnimset 风格动画，无 Avatar 警告与红色错误。

#### Scenario: 站立播 R_Idle

- **WHEN** Play Mode 下角色无任何输入静止
- **THEN** 播放 `Idle` clip（持枪站立，约 3 秒循环）

#### Scenario: WASD 移动播 R_Walk / R_Run

- **WHEN** Play Mode 下按 WASD（不按 Shift）
- **THEN** 角色播放 `Walk` clip 移动；按住 Shift 时切到 `Run` clip（BlendTree Speed 阈值过渡）

#### Scenario: 空格跳跃播完整跳跃链路

- **WHEN** Play Mode 下按 Space
- **THEN** 状态机依次进入 JumpStart（播 `R_Jump_AirR`，滞空姿态预览）→ InAir（播 `R_Jump_AirL`，滞空主循环）→ JumpLand（按落地瞬间 Speed 值播 BlendTree 内对应 land：站立 → `R_Land_2h`，走步 → `R_Land_ToRun1`，奔跑 → `R_Land_ToRun3`），整段空中视觉由 R_Jump_AirR / AirL 滞空姿态主导（无虚假"准备/蹬地"动作，匹配 ThirdPersonController 的瞬时起跳物理）

#### Scenario: Console 无红色错误与 Avatar 警告

- **WHEN** Play Mode 运行 30 秒覆盖站立 / 移动 / 跳跃所有状态
- **THEN** Console 无红色错误（含 `AnimationEvent ... has no receiver` 类错误）；无 `Avatar source not found` / `Animator is not playing an AnimatorController` / `retargeting failed` 等黄色警告

---

### Requirement: PlayerArmature 提供 SwitchSocket 动画事件接收器

PlayerArmature 根对象 SHALL 挂载提供 `void SwitchSocket(string slot)` 方法的 MonoBehaviour，吞掉 RifleGirl R_Idle / R_Walk / R_Run 等 fbx 内嵌的 `SwitchSocket` AnimationEvent，避免 Console 红色错误 spam。

`PlayerAnimEventReceiver` SHALL 是普通 MonoBehaviour（不实现 `IController`）。脚步音与落地音已由 `AudioBridge.Update()` 相位驱动处理，不依赖 AnimationEvent，故 `OnFootstep` / `OnLand` 方法不在此类实现。

`B1b.2 / Phase 4` 持枪 IK 接入时，`SwitchSocket` 将实现真实挂点切换逻辑。

#### Scenario: 接收器组件存在

- **WHEN** 选中 SampleScene 中 PlayerArmature，查看 Inspector 组件列表
- **THEN** 存在 `PlayerAnimEventReceiver` 组件（位于 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs`）

#### Scenario: SwitchSocket 方法被反射调用不抛错

- **WHEN** Play Mode 下播放 R_Idle / R_Walk / R_Run（任一会触发 `SwitchSocket` AnimationEvent 的 clip）
- **THEN** Console 不输出 `AnimationEvent 'SwitchSocket' on animation '...' has no receiver` 红色错误

---

### Requirement: JumpStart State Speed = 3.0 与其他 3 State 默认 1.0

`UnomataPlayer.controller` Base Layer 中 `JumpStart` State 的 `speed` 字段 SHALL 设为 `3.0`，让 R_Jump_AirR.fbx (1.167s) 实际播放时长压缩到 0.389s，与 SA 自家 Jump.fbx (0.400s) 节奏对齐，从而让 SA 状态机调参（exitTime=0.6637）在新素材上自洽（避免落地后才慢悠悠播 JumpStart→InAir→JumpLand 链路的"落地卡顿"）。其余 3 State (`Idle Walk Run Blend` / `InAir` / `JumpLand`) 的 speed 字段 SHALL 保持默认值 1.0。

#### Scenario: JumpStart speed = 3.0

- **WHEN** 检查 `UnomataPlayer.controller` Base Layer 中 `JumpStart` State 的 `speed` 字段
- **THEN** 字段值为 `3.0`

#### Scenario: 其他 3 个 State Speed 均为 1.0

- **WHEN** 检查 `Idle Walk Run Blend` / `InAir` / `JumpLand` 三个 State 的 `speed` 字段
- **THEN** 三个字段值均为 `1.0`

---

### Requirement: 5 条 transition 与 SA 原 controller 完全一致

`UnomataPlayer.controller` Base Layer 的 5 条 transition SHALL 与 `StarterAssetsThirdPerson.controller` 同名 transition 全部字段一字不差，包括 conditions / hasExitTime / exitTime / duration / hasFixedDuration / offset。

#### Scenario: 全 5 条 transition 字段一致

- **WHEN** 对比 `UnomataPlayer.controller` 与 `StarterAssetsThirdPerson.controller` 的 5 条 transition：
  - `Idle Walk Run Blend → InAir`
  - `Idle Walk Run Blend → JumpStart`
  - `InAir → JumpLand`
  - `JumpLand → Idle Walk Run Blend`
  - `JumpStart → InAir`
- **THEN** 每条 transition 的 conditions / hasExitTime / exitTime / duration / hasFixedDuration / offset 字段全部一致（容差 < 0.0001）

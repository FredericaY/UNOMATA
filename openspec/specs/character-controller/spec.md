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

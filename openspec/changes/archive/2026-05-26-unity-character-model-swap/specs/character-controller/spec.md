## MODIFIED Requirements

### Requirement: RifleGirl 模型正确嵌入 PlayerArmature

`PlayerArmature` 根对象 SHALL 以子对象形式包含 `Rifle_Full_Body.prefab` 实例，且 local position / local rotation 均为零（与父对象对齐）。

#### Scenario: RifleGirl 模型渲染正常

- **WHEN** 在 Unity Editor 中打开 SampleScene
- **THEN** PlayerArmature 层级下可见 Rifle_Full_Body 子对象，Renderer 激活，无紫色材质错误

---

### Requirement: StarterAssets 原视觉子对象禁用保留

原 StarterAssets 视觉子对象（`PlayerArmature/PlayerArmature` 等）SHALL 被 `SetActive(false)` 禁用，不得删除，以保留回退能力。

#### Scenario: 原视觉对象存在但不可见

- **WHEN** 在 Hierarchy 面板查看 PlayerArmature 子对象
- **THEN** StarterAssets 原视觉子对象可见且 Active 状态为 false（灰显）

---

### Requirement: PlayerArmature Animator 使用 RifleGirl Avatar

`PlayerArmature` 根对象上的 `Animator` 组件 SHALL 使用 `Rifle_Full_Body.FBX` 对应的 Humanoid Avatar Asset，以启用正确的 Humanoid Retargeting。

#### Scenario: Animator Avatar 已切换

- **WHEN** 选中 PlayerArmature 查看 Animator 组件
- **THEN** Avatar 字段显示为 RifleGirl Humanoid Avatar（非 StarterAssets 原 Avatar）

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

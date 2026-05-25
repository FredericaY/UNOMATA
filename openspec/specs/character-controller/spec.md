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

## Why

B 端在推进 Phase 2.1 B1b.1（基础动画归属修复）摸底时发现 RifleGirl 主包缺失跳跃链路（Jump / InAir / Land）动画素材；为补缺，紧急导入了 Asset Store 的 `FemaleRunnerAnimset` 包。但导入过程绕过了"二层目录归位 + 副作用清理"流程：

- 包落地于 `Assets/FemaleRunnerAnimset/`，未进 `Assets/ThirdParty/Characters/Player/<PackageName>/` 二级路径
- 包内 `Humanoid_Bot/Materials/*.mat` 与 `Scripts/Camera_Work_Script/*.cs` 等文件覆盖了已归档 change `phase0-third-party-assets-validate` 期间针对 CombatGirls 完成的 URP 材质转换（B1a 修复过的紫色材质问题面临复发风险）
- `Packages/manifest.json` 被静默追加 9 个 Unity 包（含与项目无关的 `feature.development` / `services.core`），增大依赖面与构建体积
- 包内还携带与 RifleGirl 重复的角色模型（`Humanoid_FeRifle.fbx`）、demo 场景、demo 脚本（`GroundCheck` / `WallrunCheck`）、UI 资源等噪声

这一现状违反 `phase0-third-party-assets-validate` 已建立的纪律，必须在 B1b.1 启动前以独立 change 收拾干净，并把"包导入副作用清理"沉淀为 `asset-organization` capability 的新 requirement，避免后续再发生同类漂移。

## What Changes

- 把 `Assets/FemaleRunnerAnimset/` 整体移动到 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/`（走 Unity AssetDatabase 保 GUID）
- 裁剪包内噪声：删除 `3Denvironment/`、`UI/`、`Scripts/`（含包内 PDF 文档）、`Controllers/`（11 个示例 controller，参考价值低）、`Humanoid_Bot/`（模型 / 材质 / 贴图 / 预制体，与 CombatGirls Humanoid_F 冲突）、`Scene_R.Jumps.unity`（demo 场景）
- 保留 `Animations_Rifle/` 全部内容（Jumps / Movements / TurnInplace），即真正补给 B1b.1 的素材
- 回滚 5 个被覆盖的 CombatGirls 文件至 `phase0-third-party-assets-validate` 归档版本：
  - `Humanoid_Body_Metal.mat` / `Humanoid_Body_PlasticY.mat` / `Humanoid_Face.mat`（恢复 URP/Lit 引用）
  - `Camera_Work_Script/CameraFollowWalk.cs` / `CameraWalk.cs`
- 清理 `Packages/manifest.json` 新增依赖：删除 `com.unity.feature.development`、`com.unity.services.core`；保留 `testtools.codecoverage` / `performance.profile-analyzer` / `editorcoroutines` / `ext.nunit` / `nuget.newtonsoft-json` / `settings-manager` / 内置模块新增项
- 清理 `ProjectSettings/Packages/`（新增的 `com.unity.testtools.codecoverage` 配置目录依据上一步保留决策跟随保留）
- 验证 PlayerArmature 仍正常运行、CombatGirls 材质无紫色；对保留的跳跃动画 fbx 进行 Avatar 兼容性观察（`lastHumanDescriptionAvatarSource` 指向已删除的 `Humanoid_FeRifle`），按 Console 警告决定是否需要批量改 Avatar Source
- 更新 `Docs/DEPENDENCIES.md`：把 FemaleRunnerAnimset 从 `⏳ 待迁移` 状态切到 `✅ 已迁移-...`
- 更新 `Docs/DEVELOPMENT_PLAN.md`：勾选 Phase 0 B 段对应任务行
- 在 `asset-organization` capability 新增 requirement：第三方包导入后 SHALL 在合并前完成"二层目录归位 + 覆盖文件回滚 + manifest.json 副作用评估"三项清理（沉淀本 change 暴露的纪律缺口）

## Capabilities

### New Capabilities
（无）

### Modified Capabilities
- `asset-organization`：
  - ADD 新 requirement：第三方资产包导入流程必须包含副作用清理（覆盖文件回滚 / manifest.json 评估 / 包内噪声裁剪），并要求在合入前完成
  - ADD 新 requirement（或扩展既有 "DEPENDENCIES.md 同步"）：把 FemaleRunnerAnimset 作为本 change 完成后期望的资产清单条目锁定在 `✅ 已迁移`

## Impact

- **资产**：
  - 新增（迁入）：`Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/`（约 40 个 fbx）+ 该路径下的 .meta
  - 删除：`Assets/FemaleRunnerAnimset/` 临时根目录、包内 5 个噪声子目录与 1 个 demo 场景
  - 回滚：5 个 CombatGirls 已修改文件（git restore）
- **包依赖**：`Packages/manifest.json` 删 2 项、保留 7 项 + 内置模块 2 项（净增 9 项 → 净增 7 项）
- **文档**：`Docs/DEPENDENCIES.md` 与 `Docs/DEVELOPMENT_PLAN.md` 状态同步
- **specs**：`openspec/specs/asset-organization/spec.md` 通过本 change 的 delta 增加新 requirement
- **不影响**：B1a 已落地的 PlayerArmature 层级、Humanoid Avatar 切换、QF 骨架代码、CardChainCore；本 change 不改任何 `Assets/_Project/` 下的代码
- **后置 change**：解锁 B1b.1 `unity-character-base-anim-swap`（其映射表中的 Jump / InAir / Land 候选 Motion 来源依赖本 change 完成的二级路径）

## Context

B 端在 Phase 2.1 B1b.1 摸底过程中发现 RifleGirl 主包缺失跳跃链路（Jump / InAir / Land）动画，紧急导入 Asset Store 的 `FemaleRunnerAnimset` 包补缺。导入方式是 Unity Editor 内默认导入流程，结果包落在 `Assets/FemaleRunnerAnimset/`（Asset Store 默认根路径），并附带：

- 5 个 CombatGirls 文件被覆盖（3 个材质 URP→Built-in 退化、2 个 Camera Script）
- `Packages/manifest.json` 静默追加 9 个 Unity 包
- 包内大量噪声（demo 场景、demo 脚本、demo UI、与 RifleGirl 冲突的 Humanoid_FeRifle 模型、11 个示例 controller）

上一个 change `phase0-third-party-assets-validate` 刚把第三方资产纪律建立起来，如不立即收拾本次漂移，纪律会被快速侵蚀。同时为避免类似事件再次发生，把"包导入副作用清理"沉淀为 `asset-organization` capability 的新 requirement。

本 change 与 B1b.1 是前置依赖关系：B1b.1 的 Motion 替换映射表中的 Jump / InAir / Land 槽位都引用本 change 完成的二级路径下的 fbx。

## Goals / Non-Goals

**Goals:**

- 把 `Assets/FemaleRunnerAnimset/` 整理到 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/`，仅保留 `Animations_Rifle/` 子目录
- 回滚 5 个被覆盖的 CombatGirls 文件至前一个归档版本
- 清理 `Packages/manifest.json` 不必要的依赖
- Play Mode 验证 PlayerArmature 仍正常、CombatGirls 材质无紫色复发
- 把"包导入副作用清理"纪律写入 `asset-organization` 主 spec

**Non-Goals:**

- 不替换任何 Animator Controller 的 Motion（这是 B1b.1 的工作）
- 不新建 PlayerArmature 上的 Animator 配置
- 不验证跳跃动画的 retarget 视觉效果（同上，B1b.1 范围）
- 不做 Sandbox 场景验证（包内自带 demo 场景将被删除；本包有效素材将由 B1b.1 在 PlayerArmature 上做最终业务验证）
- 不向 `Docs/INTERFACE.md` 引入新接口

## Decisions

### D1：整理路径 = `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/`

**选项**：
- A. 独立路径 `Characters/Player/FemaleRunnerAnimset/`
- B. 合并进 `Characters/Player/CombatGirls/AnimSet_FemaleRunner/`
- C. 新二级分类 `Animations/FemaleRunnerAnimset/`

**决策**：A

**理由**：FemaleRunnerAnimset 与 CombatGirls 是不同 publisher 的独立包，合并会模糊归属、未来更新困难；新建 `Animations/` 二级分类破坏 `DEPENDENCIES.md` 既有分类拓扑（角色相关动画包归 `Characters/Player/` 已是当前约定）。独立路径与现有约定最一致。

### D2：保留范围 = 仅 `Animations_Rifle/`

**选项**：
- A. 仅保留 `Animations_Rifle/`，删其余全部
- B. 保留 `Animations_Rifle/` + `Controllers/`（参考用）
- C. 全保留按需删除

**决策**：A

**理由**：`Controllers/` 内 11 个独立 controller 用命令式 trigger 驱动，与 ThirdPersonController.cs 的每帧 SetFloat/SetBool 风格不兼容，参考价值低。`Humanoid_Bot/` 内的 Humanoid_FeRifle 与 CombatGirls Humanoid_F 同类冲突。`3Denvironment/` / `UI/` / `Scripts/`（demo 脚本与包内嵌 PDF）均与本项目无关。

`Animations_Rifle/` 三个子目录全保留：
- `Jumps/`（B1b.1 必需的核心素材）
- `Movements/`（与 RifleGirl/Normal 形成互补，含跑步起步停止变体；同名 `R_Idle` 不冲突，路径不同）
- `TurnInplace/`（潜在未来需要）

### D3：CombatGirls 5 个文件用 git restore 回滚，再重启 Unity

**选项**：
- A. `git restore` 回滚 + 重启 Unity 让 Editor 重新 import
- B. 用 unityMCP 通过 AssetDatabase 修复（Material 改 shader 引用）
- C. 把 CombatGirls 整目录从一份 backup 还原

**决策**：A

**理由**：被覆盖文件本质是磁盘字节级被替换，git 仓库的前一个 commit 即为正确版本，restore 是最直接的恢复手段。git restore 后 Unity 不会自动重新加载磁盘上的 .mat 内容，必须重启 Editor（或 AssetDatabase Refresh）让其重新读取。AssetDatabase Refresh 通常足够，但材质引用涉及 SerializedObject 缓存，重启更稳。

### D4：`Packages/manifest.json` 删除原则 = 与项目无明确关联即删

**选项**：
- A. 全保留（避免破坏隐式依赖）
- B. 全删除（保险起见）
- C. 按"必要性 + 间接依赖"逐项评估

**决策**：C，偏激进倾向

**保留**：
- `com.unity.testtools.codecoverage`（开发期实用）
- `com.unity.performance.profile-analyzer`（Phase 5 平衡调试需要）
- `com.unity.editorcoroutines`（上面两个的依赖）
- `com.unity.ext.nunit`（test-framework 已在用，是 NUnit 扩展）
- `com.unity.nuget.newtonsoft-json`（体积小，未来可能用）
- `com.unity.settings-manager`（codecoverage 间接依赖）
- 内置模块新增：`com.unity.modules.subsystems` / `com.unity.modules.uielementsnative`（这些是新版 UI Toolkit 的运行时基础，部分包间接依赖）

**删除**：
- `com.unity.feature.development`（整个开发功能集，重复包含已单独保留的项，体积大）
- `com.unity.services.core`（Unity 云服务，本项目不接 Unity Services）

**理由**：偏激进策略减少推送体积与潜在后续维护负担，对应用户对"push 又要半天"的明确反馈。如果删除后 Unity Console 报缺失依赖红错，按错误信息回滚对应包并记录在 tasks 末尾遗留项。

### D5：Avatar Source 修复 = change 内 task 现场决策

**背景**：FemaleRunnerAnimset 全部跳跃 fbx 的 importer `lastHumanDescriptionAvatarSource` 字段指向包内 `Humanoid_FeRifle.fbx`（guid `c0426d61788399a4aa9702989176ee68`）。删除 `Humanoid_Bot/Models/Humanoid_FeRifle.fbx` 后该字段成为悬空引用。

**选项**：
- A. propose 阶段就决定批量改 Avatar Source
- B. 现场决策：先删 Humanoid_Bot 再 Refresh，看 Console 警告类型与数量决定
- C. 不删 Humanoid_FeRifle.fbx，保留作为 Avatar 锚点

**决策**：B

**理由**：`lastHumanDescriptionAvatarSource` 多数情况下是 import 阶段的参考字段，运行时不参与播放；fbx 内置自身 Humanoid Avatar 子资产是独立生成的，与外部 Source 字段无关。Animator 在 PlayerArmature 上用 `Humanoid_FAvatar`（CombatGirls Humanoid_F 的 Avatar）做 Humanoid Retargeting 即可重定向到 RifleGirl 骨骼。但 Unity Editor 可能对悬空引用产生黄色 import 警告——按警告级别决策：
- 仅黄色警告：忽略，写入 tasks 末尾遗留项
- 任意红色错误：批量改 Avatar Source 指向 `Humanoid_F.fbx`（即 CombatGirls 的 Avatar，guid 在 B1a 归档中可查）

### D6：操作工具栈优先级

**Asset 移动 / 删除**：必须走 Unity AssetDatabase API，禁用文件系统 mv/rm。优先级：
1. unityMCP `manage_asset action=move/delete`
2. Unity Editor 手动拖拽（unityMCP 不可用时）

**git restore CombatGirls 文件**：可直接走 git CLI（这是恢复磁盘内容，不是 Asset 移动）。restore 后必须 Unity Editor `AssetDatabase.Refresh()` 或重启 Editor。

**`Packages/manifest.json` 编辑**：直接文本编辑（不通过 unityMCP）。编辑后 Unity Package Manager 自动 resolve。

**理由**：与 `phase0-third-party-assets-validate` 已确立的工具栈一致。

## Risks / Trade-offs

### R1：删除 manifest.json 包后 Unity 报缺失依赖

**场景**：删除 `feature.development` 后，某个保留包的间接依赖被同时移除，触发红色错误

**Mitigation**：
- 删除前先用 `manage_packages action=list_packages` 备份当前状态
- 删除后立即触发 Unity 包重 resolve，监控 Console
- 如出现红色错误，按错误信息回滚对应包，并把"为何不能删"写入 tasks 末尾遗留项
- 备选：先只删 `services.core`（最不可能有依赖），再删 `feature.development`，逐步验证

### R2：跳跃 fbx 的 Avatar Source 悬空导致警告堆积

**Mitigation**：见 D5。change 内 task 现场判断警告级别。如确需批量改，单独写一个 task 用 unityMCP 的 `manage_asset` 修改 importer 配置（约 30+ 个 fbx 的 SerializedObject 操作）。

### R3：CombatGirls 材质 git restore 后 Unity 仍显示旧（紫色）状态

**场景**：Editor 内存中缓存的材质引用未刷新

**Mitigation**：restore 后必须 AssetDatabase.Refresh()；如仍紫，重启 Unity Editor。验收阶段必须 Play Mode 实际触发 PlayerArmature 渲染才算通过。

### R4：本 change 不做 Sandbox 验证可能漏掉素材损坏

**Mitigation**：把"PlayerArmature Play Mode 验证场景渲染正常"作为本 change 的最低验收线。包内素材是否真的可用（动画是否撞见空指针等深度问题）由 B1b.1 在业务集成时承担，本 change 不要求所有 fbx 都被实际播放过。

### R5：归档时主 spec 同步遗漏

**场景**：归档时只迁 delta 没把 ADDED/MODIFIED 内容应用到主 spec

**Mitigation**：归档前严格按 `rules.mdc` 的"OpenSpec 归档纪律"逐项核：delta 应用到主 spec、`Docs/DEPENDENCIES.md` 状态切换、`Docs/DEVELOPMENT_PLAN.md` 勾选、`Docs/TODO.md` B1b.1 段不再需要"待整理"标注。

## 1. 前置准备

- [x] 1.1 确认 Unity Editor 已打开 SampleScene 且非 Play Mode（编辑器状态 ready，非 Play Mode；当前活动场景 Sandbox_SciFiArena，SampleScene 切换留至 Task 7.1）
- [x] 1.2 用 `manage_packages action=list_packages` 备份当前 Packages 状态（导出到 tasks 末尾遗留项作为回滚参考）（备份方式调整：`Packages/manifest.json` 与 `packages-lock.json` 已在 git working tree，前一个 commit HEAD 即为干净基线，`git diff Packages/manifest.json` 可随时查看新增项；新增包清单已记录于 design.md D4 段）
- [x] 1.3 确认 git working tree 当前修改面（仅本 change 应处理的文件，无其他业务在途）

## 2. 二层目录归位

- [x] 2.1 用 unityMCP `manage_asset action=move` 把 `Assets/FemaleRunnerAnimset/` 整体移动到 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/`（MCP 返回 false 但实际成功，git status 已确认源消失、目标就位）
- [x] 2.2 触发 AssetDatabase Refresh，确认 Console 无红色错误
- [x] 2.3 确认 `Assets/FemaleRunnerAnimset.meta`（Asset 根的 .meta）已被自动清理或显式删除

## 3. 包内噪声裁剪

> 顺序：先删 demo 场景与与运行时无关项，再删与项目冲突的角色资源，最后删示例 controller。每步后 AssetDatabase Refresh + 监控 Console。

- [x] 3.1 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Scene_R.Jumps.unity`（demo 场景）（实际位置在 `Animations_Rifle/Scene_R.Jumps.unity`，propose 阶段误判为顶层路径；归档后 commit 前 git status 暴露该文件被 add，回头补删 + Refresh 验证已不存在）
- [x] 3.2 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/3Denvironment/`（demo 环境美术与材质）
- [x] 3.3 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/UI/`（demo Canvas + Logo）
- [x] 3.4 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Scripts/`（含 GroundCheck/CliffCheck/WallrunCheck 与 PDF 文档）
- [x] 3.5 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Controllers/`（11 个示例 controller，命令式 trigger 驱动，与本项目风格不兼容）
- [x] 3.6 删除 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Humanoid_Bot/`（含 Models/Humanoid_FeRifle.fbx + 同名 Prefab + Materials/Texture），确认与 CombatGirls 的 Humanoid_F 角色资源彼此隔离
- [x] 3.7 AssetDatabase Refresh 一次，确认 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/` 仅剩 `Animations_Rifle/` 一个子目录与配套 .meta

## 4. CombatGirls 被覆盖文件回滚

- [x] 4.1 git restore `Assets/ThirdParty/Characters/Player/CombatGirls/Humanoid_Bot/Materials/Humanoid_Body_Metal.mat`
- [x] 4.2 git restore `Assets/ThirdParty/Characters/Player/CombatGirls/Humanoid_Bot/Materials/Humanoid_Body_PlasticY.mat`
- [x] 4.3 git restore `Assets/ThirdParty/Characters/Player/CombatGirls/Humanoid_Bot/Materials/Humanoid_Face.mat`
- [x] 4.4 git restore `Assets/ThirdParty/Characters/Player/CombatGirls/Scripts/Camera_Work_Script/CameraFollowWalk.cs`
- [x] 4.5 git restore `Assets/ThirdParty/Characters/Player/CombatGirls/Scripts/Camera_Work_Script/CameraWalk.cs`
- [x] 4.6 触发 Unity AssetDatabase Refresh；如材质 Inspector 仍显示 Built-in Standard，则关闭并重启 Unity Editor 让其重新加载磁盘上的 .mat（force refresh 经历一次重连后恢复 ready，Editor 已重新加载）
- [x] 4.7 三个材质的 Shader 字段 SerializedObject 检查应回到 `Universal Render Pipeline/Lit`（或 B1a 设定的 URP Toon Shader），不为 `Standard`（文件级验证：三个 .mat 的 m_Shader 字段 guid 均为 933532a4fcc9baf4fa0491de14d08ed7 type:3，与 B1a 归档版一致；最终 Inspector 视觉确认在 Task 7.1 Play Mode 验证时由用户完成）

## 5. Packages/manifest.json 副作用清理

- [x] 5.1 编辑 `Packages/manifest.json`，删除 `com.unity.feature.development` 行
- [x] 5.2 编辑 `Packages/manifest.json`，删除 `com.unity.services.core` 行
- [x] 5.3 切回 Unity，等待 Package Manager 自动 resolve；监控 Console（manage_packages action=resolve_packages 触发；force refresh 恢复 ready）
- [x] 5.4 如 Console 出现红色错误（缺失依赖），按错误信息回滚对应包；把回滚原因记录在本 tasks.md 末尾"遗留项"段（resolve 后 Console 零红错，激进删除策略验证通过，无回滚需要）
- [x] 5.5 保留以下包不动：testtools.codecoverage / performance.profile-analyzer / editorcoroutines / ext.nunit / nuget.newtonsoft-json / settings-manager / 内置模块 subsystems + uielementsnative
- [x] 5.6 检查 `ProjectSettings/Packages/com.unity.testtools.codecoverage` 配置目录跟随保留（若 codecoverage 包被回滚则一并删除）（codecoverage 包保留，配置目录保留）
- [x] 5.7 提交（暂存）`Packages/manifest.json` 与 `Packages/packages-lock.json` 的最终状态（git diff 净增 8 行 manifest + 41 行 packages-lock；commit 时机由用户控制）

## 6. Avatar Source 悬空引用现场决策（D5 验证）

- [x] 6.1 完成 Task 3.6 删除 Humanoid_Bot 后，AssetDatabase Refresh
- [x] 6.2 收集 `Animations_Rifle/` 下所有 fbx 在 Console 的警告/错误条目（用 `read_console action=get types=["warning","error"] filter_text="FemaleRunnerAnimset"`）（filter_text="FemaleRunner" 返回 0 条警告/错误；全局 Console 除 1 条 MCP WebSocket 内部警告外无其他条目）
- [x] 6.3 判定级别：
      - 仅黄色 import 警告（如"Avatar source not found"）：忽略，记录到本 tasks 末尾遗留项
      - 任意红色错误：执行 Task 6.4
      （**实际：零黄色 import 警告，零红色错误。Avatar Source 悬空引用未触发任何 Console 提示。**）
- [x] 6.4 （条件触发）批量修改 `Animations_Rifle/` 下所有跳跃相关 fbx 的 importer，把 Avatar Source 改为指向 `Assets/ThirdParty/Characters/Player/CombatGirls/Humanoid_Bot/Models/Humanoid_F.fbx` 的 Avatar 子资产；通过 unityMCP `manage_asset` 或 SerializedObject 操作完成（**未触发**：Task 6.3 判定无须执行）
- [x] 6.5 任务 6.4 完成后再次 Refresh 与 Console 检查，确认所有红色错误清零（**未触发**：跟随 6.4 跳过）

## 7. Play Mode 验证

- [x] 7.1 打开 SampleScene；确认 PlayerArmature 层级与 B1a 归档时一致（RifleGirl 子对象激活、Geometry 子对象禁用、Animator 用 Humanoid_FAvatar）
- [x] 7.2 进入 Play Mode 短暂运行（约 5-10 秒），观察：
      - 角色显示正常，无紫色或粉色材质（CombatGirls 三材质 URP 正确）
      - 控制角色可移动，原 SA Idle/Walk/Run 动画通过 Humanoid Retargeting 正常播放
      - MagicaCloth2 布料模拟正常
      - Console 无红色错误
      （**用户手动验证：无报错、无粉色、移动正常；动画未绑属 B1b.1 范围，不在本 change 验收项**）
- [x] 7.3 退出 Play Mode；保存场景

## 8. 文档同步

- [x] 8.1 更新 `Docs/DEPENDENCIES.md`：把 FemaleRunnerAnimset 一行的状态从 `⏳ 待迁移-phase0-femalerunner-animset-validate` 改为 `✅ 已迁移-phase0-femalerunner-animset-validate`
- [x] 8.2 检查 `Docs/DEPENDENCIES.md` "Asset Store 资产清单"表共 9 行（含 FemaleRunnerAnimset），目录组织约定小节无需变动（FemaleRunnerAnimset 仍归 `Characters/Player/`）
- [x] 8.3 在 `Docs/DEVELOPMENT_PLAN.md` Phase 0 B 段把"FemaleRunnerAnimset 二层目录迁移 + 副作用清理"任务行 `[ ]` 改为 `[x]`
- [x] 8.4 检查 `Docs/TODO.md` B1b.1 段对 FemaleRunnerAnimset 的依赖说明不变（依旧标注本 change 为前置）

## 9. 归档前自检

- [x] 9.1 git status 检查：本 change 涉及的修改面包括（且仅限）
      - 新增 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/` 全部 .fbx + .meta
      - 删除 `Assets/FemaleRunnerAnimset/` 整体
      - 修改 `Packages/manifest.json` + `Packages/packages-lock.json`
      - 还原 5 个 CombatGirls 文件（git diff 应显示这些文件无差异）
      - 文档：`Docs/DEPENDENCIES.md` / `Docs/DEVELOPMENT_PLAN.md`
      - openspec：本 change 目录 + 主 spec `openspec/specs/asset-organization/spec.md`（归档时同步）
- [x] 9.2 在归档命令前完成 delta → 主 spec 同步：把 `openspec/changes/phase0-femalerunner-animset-validate/specs/asset-organization/spec.md` 内的 ADDED/MODIFIED 内容应用到 `openspec/specs/asset-organization/spec.md`
- [x] 9.3 用 `openspec validate phase0-femalerunner-animset-validate` 通过校验
- [x] 9.4 mv 到 `openspec/changes/archive/2026-05-27-phase0-femalerunner-animset-validate/`

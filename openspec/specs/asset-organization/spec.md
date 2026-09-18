# asset-organization Specification

## Purpose

规定项目自有资产与第三方包的目录边界、特殊保留路径、元数据配对和迁移要求，使工程整理后仍能追溯资产来源，并保持场景与预制体引用稳定。

## Requirements

### Requirement: 第三方资产包位于规划目录

所有通过 Asset Store 或手动导入的第三方资产包，均 SHALL 位于 `Assets/ThirdParty/<分类>/<PackageName>/` 二层目录下。`<分类>` 为按用途归类的一级目录（如 `Characters/Player`、`Characters/Enemy`、`Locomotion`、`Cloth`、`Environment`、`VFX`、`Audio`、`AI`），`<PackageName>` 为不含空格的 PascalCase 包名（拍平作者命名层）。

`Assets/` 根目录下不得存在错位的第三方资产文件夹，例外如下：

- `Assets/QFramework/` 与 `Assets/QFrameworkData/`：QFramework 框架硬编码路径
- `Assets/Gizmos/`：Unity 引擎保留路径，Editor 自动从该目录加载 Gizmo 图标，由 Behavior Designer 等资产包提供运行时 Gizmo 资源
- `Assets/StreamingAssets/`：Unity 引擎保留路径
- `Assets/Screenshots/`：项目截图目录

#### Scenario: StarterAssets 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Locomotion/StarterAssets/` 包含 StarterAssets 全部内容；`Assets/StarterAssets/` 与 `Assets/ThirdParty/StarterAssets/` 均不存在

#### Scenario: CombatGirls 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Characters/Player/CombatGirls/` 包含 CombatGirls 全部内容；`Assets/CombatGirlsCharacterPack/` 与 `Assets/ThirdParty/CombatGirls/` 均不存在

#### Scenario: MagicaCloth2 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Cloth/MagicaCloth2/` 包含 MagicaCloth2 全部内容；`Assets/MagicaCloth2/` 与 `Assets/ThirdParty/MagicaCloth2/` 均不存在

#### Scenario: Behavior Designer 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/AI/BehaviorDesigner/` 包含 Behavior Designer 全部 Runtime/Editor/Integrations 内容；`Assets/Behavior Designer/` 不存在

#### Scenario: Mech Pack 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Characters/Enemy/MechPack/` 包含 Mech Pack 全部内容；`Assets/Mech Pack/` 不存在

#### Scenario: SciFiArena 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Environment/SciFiArena/` 包含 Sci Fi Arena 1 与 Sci Fi Arena 2 两套子目录；`Assets/Sci fi 2in1/` 不存在

#### Scenario: SciFiEffects 位于二层正确目录（拍平作者目录）

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/VFX/SciFiEffects/` 包含 FORGE3D Sci-Fi Effects 全部内容；`Assets/FORGE3D/` 不存在

#### Scenario: SciFiWeaponsBulletHell 音效包位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Audio/SciFiWeaponsBulletHell/` 包含全部音频与 PDF 文档；`Assets/Sci-Fi Weapons-Bullet Hell Sound Effects Pack/` 不存在

#### Scenario: FemaleRunnerAnimset 位于二层正确目录

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/` 包含 RifleGirl 跳跃动画补充包的有效素材子目录（`Animations_Rifle/`）；`Assets/FemaleRunnerAnimset/` 不存在

#### Scenario: Gizmos 目录保留在 Assets 根

- **WHEN** 项目目录整理完成
- **THEN** `Assets/Gizmos/` 仍存在于 Assets 根目录下，包含 Behavior Designer 提供的 hierarchy 与 scene 图标 PNG，不被视为违规散落目录

#### Scenario: Monsters 空壳目录已删除

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/Monsters/` 不存在（已被 `Characters/Enemy/` 取代）

#### Scenario: ThirdParty/QFramework 空目录仍不存在

- **WHEN** 项目目录整理完成
- **THEN** `Assets/ThirdParty/QFramework/` 目录不存在（QFramework 在 `Assets/QFramework/` 根目录下）

---

### Requirement: 第三方资产 URP 材质兼容性

所有可能含 Built-in 渲染管线材质的第三方资产包 SHALL 完成 URP 兼容性检查。检查方式：在场景中拖入代表性 prefab 目视确认；紫/粉材质 SHALL 通过 `Edit → Render Pipeline → Universal Render Pipeline → Convert Selected Built-in Materials to URP` 工具或包内自带的 URP 转换器修复。无法转换的少量材质 SHALL 记录在对应 change 的 tasks.md 末尾"遗留项"段，并标记为 Phase 6 打磨期处理。

#### Scenario: CombatGirls 材质在 URP 下正常显示

- **WHEN** URP 材质转换完成，在 Scene 视图中查看 CombatGirls 角色
- **THEN** 角色模型显示正常颜色，无紫色或粉色材质占位

#### Scenario: Mech Pack 材质在 URP 下正常显示

- **WHEN** URP 材质转换完成，在 Sandbox_MechPack 场景中查看 mech prefab
- **THEN** mech 模型表面金属/喷漆材质显示正常，无紫色或粉色材质占位

#### Scenario: SciFiArena 场景材质在 URP 下正常显示

- **WHEN** URP 材质转换完成，在 Sandbox_SciFiArena 场景中查看 Arena 主 prefab
- **THEN** 场景墙体、地面、装饰物材质显示正常，Lightmap 与 Reflection Probe 无明显错误，无紫色或粉色材质占位

#### Scenario: SciFiEffects 粒子在 URP 下正常显示

- **WHEN** URP 材质转换完成，在 Sandbox_SciFiEffects 场景中 Play Mode 触发特效
- **THEN** 粒子系统正常渲染，无紫色或粉色材质占位（少量需手动替换 Shader 的可登记为遗留项）

---

### Requirement: 移动后无资产引用断裂

资产移动 SHALL 通过 Unity AssetDatabase API 完成，确保所有 GUID 引用保持有效，Unity Editor Console 在移动后无红色错误。每次目录迁移操作后 SHALL 立即触发 `AssetDatabase.Refresh()`，确认 Console 无报错后再进行下一次迁移。

#### Scenario: 移动后 Console 无报错

- **WHEN** 所有资产移动完成，执行 AssetDatabase Refresh
- **THEN** Unity Console 中不出现与移动资产相关的编译错误或缺失引用错误

#### Scenario: 文件系统命令绕道被禁止

- **WHEN** 任何资产迁移操作发起
- **THEN** 操作 SHALL 通过 unityMCP 的 manage_asset action=move 或 Unity 内手动拖拽完成；不得使用 PowerShell `mv` / `Move-Item` / 文件系统 `git mv` 直接搬动 `Assets/` 下的资产或其 .meta 文件

---

### Requirement: DEPENDENCIES.md 资产清单与目录组织约定同步

`Docs/DEPENDENCIES.md` 的 Asset Store 资产表 SHALL 包含项目当前所有第三方资产包条目（含资产名、用途、目标二层目录、状态四列），且 SHALL 包含一节"目录组织约定"说明二层分类规则与 `Assets/Gizmos/` 等例外路径。表内状态语义 SHALL 涵盖 `✅ 已验证-...`、`✅ 已迁移-...`、`⚠ 已迁移-... 登记遗留`、`⏳ 待迁移-<change-name>`、`⏳ 待选型` 五类，其中"待迁移"用于包已导入到临时位置但二层目录归位尚未完成的过渡状态。

#### Scenario: DEPENDENCIES.md 包含全部 9 个资产条目

- **WHEN** 文档更新完成
- **THEN** Asset Store 资产表中包含 CombatGirls、FemaleRunnerAnimset、StarterAssets、MagicaCloth2、BehaviorDesigner、MechPack、SciFiArena、SciFiEffects、SciFiWeaponsBulletHell 共 9 条记录，每条均填写资产名、用途、目标目录（二层路径）、状态四列

#### Scenario: DEPENDENCIES.md 包含目录组织约定小节

- **WHEN** 文档更新完成
- **THEN** DEPENDENCIES.md 存在一节明确说明 `Assets/ThirdParty/<分类>/<PackageName>/` 二层结构、各分类含义，以及 `Assets/Gizmos/`、`Assets/QFramework[Data]/` 等不属于本规则覆盖范围的例外路径

#### Scenario: 已有三包目标目录列已修订为二层路径

- **WHEN** 文档更新完成
- **THEN** CombatGirls / StarterAssets / MagicaCloth2 三条目的"目标目录"列分别为 `Assets/ThirdParty/Characters/Player/CombatGirls/` / `Assets/ThirdParty/Locomotion/StarterAssets/` / `Assets/ThirdParty/Cloth/MagicaCloth2/`

#### Scenario: FemaleRunnerAnimset 状态切到已迁移

- **WHEN** `phase0-femalerunner-animset-validate` 归档时
- **THEN** DEPENDENCIES.md 中 FemaleRunnerAnimset 一条目的状态列为 `✅ 已迁移-phase0-femalerunner-animset-validate`，目标目录列为 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/`

---

### Requirement: 第三方资产包导入后必须完成副作用清理

任何第三方资产包（Asset Store 包、`.unitypackage` 离线包、Package Manager 注册包）导入后，在合并到主分支或被后续业务 change 引用前，SHALL 在同一个 change 内完成"二层目录归位 + 覆盖文件回滚 + 包依赖评估 + 包内噪声裁剪"四项清理。任一项未完成的导入视为漂移状态，不得进入业务开发。

清理项定义：

1. **二层目录归位**：包默认落地路径如不在 `Assets/ThirdParty/<分类>/<PackageName>/` 下，SHALL 通过 Unity AssetDatabase 移动到该路径
2. **覆盖文件回滚**：包内若与已存在的第三方资产同名（典型如多个包共享角色 `Humanoid_Bot/Materials/*.mat`、`Scripts/*.cs`），覆盖原文件时 SHALL 通过 `git restore` 恢复被覆盖文件至前一个归档版本，再通过 AssetDatabase 把新包的同名文件迁移到独立子目录或删除
3. **包依赖评估**：`Packages/manifest.json` 与 `ProjectSettings/Packages/` 的新增项 SHALL 逐项评估必要性，与项目无关的依赖（如 `com.unity.feature.development` 整集合包、`com.unity.services.core` 等）SHALL 在 change 内移除
4. **包内噪声裁剪**：包内 demo 场景、demo 脚本、demo UI、demo 模型（与项目角色冲突时）等 SHALL 评估并删除；保留项 SHALL 限定为本 change 后续业务实际需要的素材子目录

#### Scenario: FemaleRunnerAnimset 完成全部四项清理

- **WHEN** `phase0-femalerunner-animset-validate` 归档时
- **THEN** `Assets/FemaleRunnerAnimset/` 临时根路径不存在；`Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/` 仅包含 `Animations_Rifle/` 子目录；CombatGirls 的 5 个被覆盖文件已恢复 `phase0-third-party-assets-validate` 归档版本；`Packages/manifest.json` 不再包含 `com.unity.feature.development` 与 `com.unity.services.core`

#### Scenario: 漂移状态被业务 change 引用时阻塞

- **WHEN** 后续 change（如 B1b.1 `unity-character-base-anim-swap`）尝试引用某个未完成清理的包内资产
- **THEN** 该业务 change SHALL 显式声明 Phase 0 清理 change 为前置依赖，且在前者归档前不得开 propose

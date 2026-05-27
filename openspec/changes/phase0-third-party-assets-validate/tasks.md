## 1. 准备：Unity 状态确认与分支

- [x] 1.1 确认 Unity Editor 已打开当前项目，且**处于非 Play Mode**
- [x] 1.2 创建工作分支 `feat/phase0-third-party-assets-validate`
- [x] 1.3 在 Unity 中保存当前所有未保存场景，避免迁移时丢失改动
- [x] 1.4 git commit 当前状态作为基线（"chore: baseline before third-party asset reorg"）

## 2. 创建二层分类壳目录

> 全部通过 unityMCP 的 `manage_asset` 在 `Assets/ThirdParty/` 下创建空文件夹（带 .meta），保证 GUID 注册。

- [x] 2.1 创建 `Assets/ThirdParty/Characters/`
- [x] 2.2 创建 `Assets/ThirdParty/Characters/Player/`
- [x] 2.3 创建 `Assets/ThirdParty/Characters/Enemy/`
- [x] 2.4 创建 `Assets/ThirdParty/Locomotion/`
- [x] 2.5 创建 `Assets/ThirdParty/Cloth/`
- [x] 2.6 创建 `Assets/ThirdParty/Audio/`
- [x] 2.7 创建 `Assets/ThirdParty/AI/`
- [x] 2.8 验证 `Assets/ThirdParty/Environment/` 与 `Assets/ThirdParty/VFX/` 已存在（`phase0-cleanup-and-validate` 已创建），如不存在则补建

## 3. 已有三包二层化迁移（保 GUID）

> 每步迁移后立即 `AssetDatabase.Refresh()`，确认 Console 零红色错误后再做下一步；任何错误立即 git reset 并定位 GUID 断裂源。

- [x] 3.1 通过 unityMCP 把 `Assets/ThirdParty/CombatGirls/` 整体移动到 `Assets/ThirdParty/Characters/Player/CombatGirls/`
- [x] 3.2 验证 PlayerArmature 视觉模型引用未断裂（打开 SampleScene，PlayerArmature 子对象 RifleGirl 不报缺失引用）
- [x] 3.3 通过 unityMCP 把 `Assets/ThirdParty/StarterAssets/` 整体移动到 `Assets/ThirdParty/Locomotion/StarterAssets/`
- [x] 3.4 验证 SampleScene 中 PlayerArmature 的 ThirdPersonController 脚本与 Animator Controller 引用未断裂
- [x] 3.5 通过 unityMCP 把 `Assets/ThirdParty/MagicaCloth2/` 整体移动到 `Assets/ThirdParty/Cloth/MagicaCloth2/`
- [x] 3.6 验证 RifleGirl 子对象 Rifle_Dress / Rifle_Jacket 上的 MagicaCloth2 组件引用未断裂
- [x] 3.7 git commit "refactor: 已有三包迁入二层目录"

## 4. 删除空壳目录

- [x] 4.1 通过 unityMCP 删除 `Assets/ThirdParty/Monsters/`（已被 `Characters/Enemy/` 取代）
- [x] 4.2 删除其他遗留空壳（如旧 `Assets/ThirdParty/CombatGirls/` 等迁移源目录）

## 5. 新进 5 包二层化迁移（保 GUID）

- [ ] 5.1 把 `Assets/Behavior Designer/` 整体移动到 `Assets/ThirdParty/AI/BehaviorDesigner/`
- [ ] 5.2 验证 Console 无红色错误，Behavior Designer 菜单（Tools → Behavior Designer）仍可打开
- [ ] 5.3 把 `Assets/Mech Pack/` 整体移动到 `Assets/ThirdParty/Characters/Enemy/MechPack/`
- [ ] 5.4 把 `Assets/Sci fi 2in1/` 整体移动到 `Assets/ThirdParty/Environment/SciFiArena/`（保留内部 Sci Fi Arena 1 / Sci Fi Arena 2 两套子目录）
- [ ] 5.5 把 `Assets/FORGE3D/Sci-Fi Effects/` 整体移动到 `Assets/ThirdParty/VFX/SciFiEffects/`（拍平作者目录）
- [ ] 5.6 删除空目录 `Assets/FORGE3D/`（如已空）
- [ ] 5.7 把 `Assets/Sci-Fi Weapons-Bullet Hell Sound Effects Pack/` 整体移动到 `Assets/ThirdParty/Audio/SciFiWeaponsBulletHell/`
- [ ] 5.8 确认 `Assets/Gizmos/` **保持原位**（Unity 引擎保留路径）
- [ ] 5.9 整体目录扫描：`Assets/` 根目录除 `_Project / ThirdParty / QFramework / QFrameworkData / Gizmos / StreamingAssets / Screenshots` 外，无散落第三方包目录
- [ ] 5.10 git commit "refactor: 新进 5 包迁入二层目录"

## 6. URP 材质兼容性检查

- [ ] 6.1 **Mech Pack**：在临时场景拖入一个代表性 mech prefab，目视检查所有材质是否紫色
- [ ] 6.2 如 Mech Pack 出现紫材质，Project 视图选中 `Assets/ThirdParty/Characters/Enemy/MechPack/Materials/`，运行 `Edit → Render Pipeline → Universal Render Pipeline → Convert Selected Built-in Materials to URP`
- [ ] 6.3 **SciFiArena**：分别拖入 Sci Fi Arena 1 与 Sci Fi Arena 2 的主 prefab 或 demo 场景，目视检查材质、Lightmap、Reflection Probe
- [ ] 6.4 如 SciFiArena 出现紫材质，对其 Materials 目录运行 URP Convertor
- [ ] 6.5 **SciFiEffects**：拖入 1~2 个代表性特效 prefab，Play Mode 触发，检查粒子 Shader 渲染
- [ ] 6.6 SciFiEffects 粒子 Shader 如使用 `Particles/Standard Surface` 等 Built-in 系列，手动替换为 URP `Particles/Lit` 或 `Particles/Unlit`
- [ ] 6.7 **CombatGirls** 复检：上次迁移后再次确认 RifleGirl 材质无紫色（既有 URP 转换器已跑过，本步仅复核）
- [ ] 6.8 转换不动的少量材质登记到本文件末尾"遗留项"段，标注 Phase 6 处理
- [ ] 6.9 git commit "fix(urp): 第三方资产 URP 材质兼容性"

## 7. Sandbox 验证场景目录建立

- [ ] 7.1 在 Unity 中创建目录 `Assets/_Project/Scenes/Sandbox/`
- [ ] 7.2 验证目录已生成 .meta 文件

## 8. Sandbox 场景：Mech Pack

- [ ] 8.1 创建 `Assets/_Project/Scenes/Sandbox/Sandbox_MechPack.unity`
- [ ] 8.2 场景中放入 1 个 mech prefab，位置归零
- [ ] 8.3 添加 Directional Light 与 Camera 默认对焦 mech
- [ ] 8.4 mech 上 Animator Controller 设置为播放 Idle 默认动画
- [ ] 8.5 Play Mode 验证：mech 渲染正常、Idle 动画播放、Console 零红色错误
- [ ] 8.6 Save Scene

## 9. Sandbox 场景：SciFiArena

- [ ] 9.1 创建 `Assets/_Project/Scenes/Sandbox/Sandbox_SciFiArena.unity`
- [ ] 9.2 场景中拖入 SciFiArena（Sci Fi Arena 1 或 Sci Fi Arena 2 任选其一）主 prefab
- [ ] 9.3 从 SampleScene 引入或新建一个 PlayerArmature 实例（仅作移动测试，不挂业务）
- [ ] 9.4 Play Mode 验证：玩家可在 Arena 内移动、Collider 正常阻挡、Console 零红色错误
- [ ] 9.5 Save Scene

## 10. Sandbox 场景：SciFiEffects

- [ ] 10.1 创建 `Assets/_Project/Scenes/Sandbox/Sandbox_SciFiEffects.unity`
- [ ] 10.2 场景中放入 1 个特效 prefab（如爆炸/能量类代表）
- [ ] 10.3 特效 prefab 设置为 Play Mode 自动播放（PlayOnAwake 或 Loop）
- [ ] 10.4 添加 Camera 与 Light 保证特效可见
- [ ] 10.5 Play Mode 验证：粒子系统正常渲染、Console 零红色错误
- [ ] 10.6 Save Scene

## 11. Sandbox 场景：Audio

- [ ] 11.1 创建 `Assets/_Project/Scenes/Sandbox/Sandbox_Audio.unity`
- [ ] 11.2 场景中创建空 GameObject 挂 AudioSource，引用 SciFiWeaponsBulletHell 中任一段武器音效（如 `AUDIO/` 下随机一个 .wav）
- [ ] 11.3 AudioSource PlayOnAwake = true
- [ ] 11.4 Play Mode 验证：音效自动播放可听、Console 零红色错误
- [ ] 11.5 Save Scene

## 12. Sandbox 场景：Behavior Designer

- [ ] 12.1 创建 `Assets/_Project/Scenes/Sandbox/Sandbox_BT.unity`
- [ ] 12.2 场景中创建空 GameObject 挂 `BehaviorTree` 组件
- [ ] 12.3 在 Behavior Designer 编辑器中编辑该 GameObject 的 BT：Sequence → Idle (Wait 1s) → Log("BT tick OK")
- [ ] 12.4 Save BT 与场景
- [ ] 12.5 Play Mode 验证：BT 正常 tick、Log 输出"BT tick OK"、Console 零红色错误
- [ ] 12.6 git commit "test: 第三方资产 5 个 Sandbox 验证场景"

## 13. 文档同步：DEPENDENCIES.md

- [ ] 13.1 资产表新增 5 条记录：BehaviorDesigner / MechPack / SciFiArena / SciFiEffects / SciFiWeaponsBulletHell（含资产名、用途、目标二层目录、状态四列）
- [ ] 13.2 资产表已有 3 条修订目标目录列：CombatGirls → `Assets/ThirdParty/Characters/Player/CombatGirls/`；StarterAssets → `Assets/ThirdParty/Locomotion/StarterAssets/`；MagicaCloth2 → `Assets/ThirdParty/Cloth/MagicaCloth2/`
- [ ] 13.3 资产表所有条目状态列填写完整（已验证 / 已验证-Sandbox demo / 待补充等）
- [ ] 13.4 新增"目录组织约定"小节，说明 `Assets/ThirdParty/<分类>/<PackageName>/` 二层结构、各分类含义（Characters/Player、Characters/Enemy、Locomotion、Cloth、Environment、VFX、Audio、AI），以及 `Assets/Gizmos/`、`Assets/QFramework[Data]/`、`Assets/StreamingAssets/`、`Assets/Screenshots/` 例外条款
- [ ] 13.5 BehaviorDesigner 条目"用途"列明确写为"敌人 AI Behavior Tree 框架"

## 14. 文档同步：DEVELOPMENT_PLAN.md

- [ ] 14.1 Phase 0 → B（队友）段，把"第三方资产二层目录整理"、"URP 材质兼容性检查"、"B 档最小可用性验证"、"敲定敌人 BT 框架选型 = Opsive Behavior Designer"四项任务行勾选为 `- [x]`
- [ ] 14.2 Phase 2.3 C2 任务行：BT 包选型措辞改为"Opsive **Behavior Designer**（Phase 0 补充工作已导入并验证，位于 `Assets/ThirdParty/AI/BehaviorDesigner/`），无需走 Package Manager"（如未在前次 edit 中改完则补改）

## 15. 文档同步：agent rules

- [ ] 15.1 `.codemaker/rules/rules.mdc` "目录约定"段的 `Assets/ThirdParty/<PackageName>/` 描述补一句"可二层分类 `<分类>/<PackageName>/`，分类规则见 DEPENDENCIES.md"
- [ ] 15.2 同段 `Assets/Gizmos/` 例外说明（Behavior Designer 提供运行时 Gizmo 图标，Unity 引擎保留路径）

## 16. 终验

- [ ] 16.1 Unity Console 零红色错误
- [ ] 16.2 5 个 Sandbox 场景每个 Play Mode 跑通且符合预期
- [ ] 16.3 `Assets/` 根目录除 `_Project / ThirdParty / QFramework / QFrameworkData / Gizmos / StreamingAssets / Screenshots` 外，无散落第三方包目录
- [ ] 16.4 `Docs/DEPENDENCIES.md` 资产表所有条目状态列填写完整
- [ ] 16.5 运行 `openspec validate phase0-third-party-assets-validate --strict`，输出零错误
- [ ] 16.6 git commit "docs: 第三方资产整理与可用性验证落地"

## 遗留项（Phase 6 打磨期处理）

> URP 转换不动的材质 / Shader 在此登记。本 change 完成时此段可为空，验收期发现的问题写入。

- （待填写）

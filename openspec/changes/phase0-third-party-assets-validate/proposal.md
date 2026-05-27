## Why

Phase 2/3/4 所需的剩余 5 个第三方资产包（Behavior Designer、Mech Pack、Sci fi 2in1、FORGE3D Sci-Fi Effects、Sci-Fi Weapons-Bullet Hell SFX）已全部导入但散落在 `Assets/` 根目录，且 `asset-organization` 现有契约只覆盖一层结构 `ThirdParty/<PackageName>/`，无法承载日益增多的同类资产。在写任何 Phase 2 业务代码之前，必须完成目录二层化整理、URP 材质兼容性确认与最小可用性验证（B 档：每包跑通一个最小 demo，不接业务）。同时本次需敲定敌人 BT 框架选型为 Opsive Behavior Designer，落地 Phase 2.3 C2。

## What Changes

- **BREAKING**：`asset-organization` 目录约定从一层 `ThirdParty/<PackageName>/` 升级为二层 `ThirdParty/<分类>/<PackageName>/`，已存在的三个包（CombatGirls / StarterAssets / MagicaCloth2）需同步迁入新二层路径
- 新增分类壳目录：`Characters/Player/`、`Characters/Enemy/`、`Locomotion/`、`Cloth/`、`Audio/`、`AI/`；保留 `Environment/`、`VFX/`；删除空壳 `Monsters/`（被 `Characters/Enemy/` 取代）
- 5 个新进资产包通过 Unity AssetDatabase 迁移到二层路径
- 明确约定 `Assets/Gizmos/` 为 Unity 引擎保留路径（Behavior Designer 运行时 Gizmo 图标），不动且不算违规
- URP 材质兼容性检查覆盖 Mech Pack / SciFiArena / SciFiEffects 三个高风险包；紫材质执行 URP Convertor，转换不动者登记为 Phase 6 打磨期遗留项
- 新增"第三方资产 B 档可用性验证"契约：每包通过一个 `Sandbox_<PackageName>.unity` 最小场景在 Play Mode 跑通最小可见行为
- Sandbox 场景统一放 `Assets/_Project/Scenes/Sandbox/`，禁止污染 `SampleScene`
- BT 框架选型敲定：Opsive Behavior Designer（已导入），不再使用 `com.unity.behavior`
- 同步更新 `Docs/DEPENDENCIES.md`（5 个新条目 + 已有三包路径修订 + 二层分类规则小节）
- 同步更新 `Docs/DEVELOPMENT_PLAN.md`（Phase 0 B 段任务勾选 + Phase 2.3 C2 BT 选型敲定）
- 同步更新 `.codemaker/rules/rules.mdc`（"目录约定"段补"可二层分类"说明）

## Capabilities

### New Capabilities

- `third-party-asset-validation`：每个第三方资产包必须通过一个独立的最小可用性 Sandbox 场景验证（B 档：Play Mode 跑通最小可见行为，不接业务、不接 QF），并约定 Sandbox 场景目录与命名规范

### Modified Capabilities

- `asset-organization`：目录结构契约从一层升级为二层 `ThirdParty/<分类>/<PackageName>/`；新增 `Assets/Gizmos/` 例外条款；URP 材质转换契约从仅 CombatGirls 扩展为对所有可能含 Built-in 管线材质的第三方包通用；新增 5 个第三方资产条目纳入 DEPENDENCIES.md 文档同步要求

## Impact

- `Assets/` 目录结构重组：5 个新包 + 3 个已有包共 8 处 AssetDatabase Move 操作（保 GUID）
- `Assets/_Project/Scenes/Sandbox/` 新建子目录及 5 个 Sandbox 场景
- `Docs/DEPENDENCIES.md`：资产表新增/修订 8 条，新增"目录组织约定"小节
- `Docs/DEVELOPMENT_PLAN.md`：Phase 0 B 段任务勾选，Phase 2.3 C2 BT 选型措辞更新
- `.codemaker/rules/rules.mdc`：目录约定段措辞更新
- `openspec/specs/asset-organization/spec.md`：通过 delta spec 应用 MODIFIED/ADDED/REMOVED 操作
- `openspec/specs/third-party-asset-validation/spec.md`：新建
- 不影响 Core 层（`Unomata.Core` / `CardChainCore`）任何代码
- 不影响 QFramework 框架层
- 不引入任何新的 Package Manager 依赖（Behavior Designer 是资产包，不走 PM）

## Why

三个第三方资产包（StarterAssets、CombatGirlsCharacterPack、MagicaCloth2）导入后均落在根目录，与 DEPENDENCIES.md 规划的 `Assets/ThirdParty/` 结构不符，且两个关键技术依赖（QFramework 兼容性、CombatGirls + StarterAssets 角色控制器可用性）尚未验证——这是 Phase 1 及后续所有开发的前提条件，必须在写任何业务代码之前完成。

## What Changes

- 将 `Assets/StarterAssets/` 移动到 `Assets/ThirdParty/StarterAssets/`
- 将 `Assets/CombatGirlsCharacterPack/` 内容迁移到 `Assets/ThirdParty/CombatGirls/`
- 将 `Assets/MagicaCloth2/` 移动到 `Assets/ThirdParty/MagicaCloth2/`
- 删除 `Assets/ThirdParty/QFramework/` 空目录（文档已明确 QFramework 必须在根目录）
- 对 CombatGirls 材质运行 URP 转换（包内自带 `URP_UTS_Convertor_CombatGirl_Rifle.unitypackage`）
- 验证 QFramework 在 Unity 2022.3 LTS 下无编译错误、菜单正常、IOC/事件机制可用
- 验证 CombatGirls 模型可替换 StarterAssets 默认 Armature，动画无冲突，角色可正常操控
- 更新 `DEPENDENCIES.md`：补充 MagicaCloth2 条目、补全资产状态列
- 将 `TODO.md` 对应任务按验证结论迁移至 `DEVELOPMENT_PLAN.md`

## Capabilities

### New Capabilities

- `asset-organization`: Assets 目录结构整洁，第三方资产均在 ThirdParty 规划目录下，URP 材质转换完成
- `qframework-integration`: QFramework 在 Unity 2022.3 LTS 下完整可用，Architecture/IOC/事件机制验证通过
- `character-controller`: CombatGirls 模型 + StarterAssets 控制器组合方案确定（方案A/B/C 三选一），SampleScene 有可 Play 的 demo

### Modified Capabilities

<!-- 无已有 spec 需要变更 -->

## Impact

- `Assets/` 目录结构重组（GUID 通过 Unity AssetDatabase 移动，不破坏引用）
- `Packages/manifest.json` 可能需要补充 `com.unity.animation.rigging`（验证 Task 2 依赖项）
- `Docs/DEPENDENCIES.md` 更新
- `Docs/TODO.md` 内容迁移/删除
- `Docs/DEVELOPMENT_PLAN.md` Phase 0 任务状态更新
- `Assets/_Project/Scenes/SampleScene.unity` 将包含可 Play 的角色 demo

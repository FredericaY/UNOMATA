## Why

Phase 2.0 QF 骨架已归档，Phase 2.1 C1 正式启动。当前 SampleScene 中 PlayerArmature 使用的是 StarterAssets 自带的简单 Capsule/机器人视觉模型，需要替换为 CombatGirls RifleGirl（含 MagicaCloth2 布料物理）。Phase 0 验证已确认方案 B（两者均 Humanoid Rig，Mecanim 自动重定向），本 change 执行该补丁：嵌入 RifleGirl 模型、切换 Avatar、清理多余摄像头。

## What Changes

- **操作** `PlayerArmature` 下 StarterAssets 原视觉子对象（`PlayerArmature/Geometry` 或等效子对象）：`SetActive(false)` 禁用，保留不删除
- **加入** `Assets/ThirdParty/CombatGirls/RifleGirl/Prefab/Rifle_Full_Body.prefab` 作为 `PlayerArmature` 的子对象，local position/rotation 归零
- **切换** `PlayerArmature` 根 `Animator` 组件的 Avatar 为 `Rifle_Full_Body.FBX` 对应的 Humanoid Avatar Asset
- **清理** SampleScene 中多余的 Main Camera / AudioListener，确保场景只有唯一 AudioListener
- **验证** Play Mode：RifleGirl 正确渲染（无紫色材质）、WASD 移动动画通过 Humanoid Retargeting 正常播放、MagicaCloth2 布料物理（Rifle_Dress、Rifle_Jacket）正常模拟

## Capabilities

### Modified Capabilities

- `character-controller`：PlayerArmature 视觉从 StarterAssets 占位换为 RifleGirl；Avatar 切换完成，为 B1b 上半身动画层提供正确的 Humanoid Avatar 基础

## Impact

- **场景**：`Assets/_Project/Scenes/SampleScene.unity` 内 PlayerArmature 层级结构变更（新增子对象、禁用旧子对象、清理相机）
- **无新脚本**：本次纯 Unity 场景/Inspector 操作，不新增 C# 代码
- **不影响**：QF 骨架（GameApp、PlayerSystem 等代码不变）；TPS 控制器脚本不变
- **后置 change**：B1b（上半身动画层 + 双相机）依赖本 change 完成后的 Humanoid Avatar 状态

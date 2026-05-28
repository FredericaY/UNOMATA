## Why

B1b.1 (`unity-character-base-anim-swap`) 把 SA 自家素材换成 RifleGirl/FRA 后，原 fbx 内嵌的 `OnFootstep` / `OnLand` AnimationEvent 不再存在，`ThirdPersonController.OnFootstep` / `OnLand` 私有方法永不被 SendMessage 触发，所有走 / 跑 / 落地音效完全失声。资产侧 `FootstepAudioClips[10]` 与 `LandingAudioClip` 仍完整挂在 PlayerArmature 的 ThirdPersonController 组件上，唯独缺动画端事件触发钩子。

本 change 以"工作量极低、立即恢复音效"为目标，通过 Unity `ModelImporter.clipAnimations[].events` API 给 5 个新素材 clip 注入 `OnFootstep` / `OnLand` 事件——事件数据写入 .meta 文件而非 fbx 二进制，与 B1a 切 RifleGirl Humanoid Avatar 时改 .meta 的做法同口径。

Audio QF 化（建 AudioModel/AudioSystem、迁移 clips 引用、把出口从 ThirdPersonController 私有方法切到 AudioSystem）拆到下一个 change `unity-audio-system-qf`（B1c.2），本期 scope 锁死"补回失声"。

## What Changes

- 通过 `execute_code` 调 `AssetImporter.GetAtPath<ModelImporter>` API，给 5 个 fbx 主 clip 注入 AnimationEvent：
  - `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Walk.fbx` 主 clip：2 个 `OnFootstep` 事件
  - `.../R_Run.fbx` 主 clip：2 个 `OnFootstep` 事件
  - `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/Land/R_Land_2h.fbx` 主 clip：1 个 `OnLand` 事件（time=0）
  - `.../LandToRun/R_Land_ToRun1.fbx` 主 clip：1 个 `OnLand` 事件（time=0）
  - `.../LandToRun/R_Land_ToRun3.fbx` 主 clip：1 个 `OnLand` 事件（time=0）
- 走 / 跑相位标定方式：先尝试程序化（遍历 clip 的 LeftFoot/RightFoot Y 轴 transform curves 找局部最小值对应的 normalizedTime），失败则用经验值兜底——`R_Walk` `[0.25, 0.75]` / `R_Run` `[0.20, 0.70]`
- 全部事件 `messageOptions = SendMessageOptions.DontRequireReceiver`：B1c.2 未上线前 receiver 只有 ThirdPersonController（私有方法照样收 SendMessage），上线后 PlayerAnimEventReceiver 会扩展同名方法接管，DontRequireReceiver 让两阶段切换都不会报错
- `importer.SaveAndReimport()` 持久化，写入 5 个 .fbx.meta
- 不动 `PlayerAnimEventReceiver.cs`（仍只吞 SwitchSocket）
- 不动 `ThirdPersonController.cs`（私有 `OnFootstep` / `OnLand` 不变，自动收 SendMessage）
- 不迁移 ThirdPersonController Inspector 上的 FootstepAudioClips / LandingAudioClip 引用（B1c.2 处理）
- 不补"起跳音"（SA 原项目本就无此线）

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `character-controller`: 新增"角色动画 AnimationEvent 钩子"requirement——B1b.1 归档后 base layer motion 切到 RifleGirl/FRA 系列，这些 fbx 不带 SA 风格的 `OnFootstep` / `OnLand` 事件。本 change 通过 .meta 注入事件，让 ThirdPersonController 的私有音频回调重新被触发，恢复走 / 跑 / 落地音效。第三方 fbx 二进制文件保持只读

## Impact

- **资产**：
  - 修改：5 个 .fbx.meta（仅 `clipAnimations[].events` 字段）
    - `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Walk.fbx.meta`
    - `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Run.fbx.meta`
    - `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/Land/R_Land_2h.fbx.meta`
    - `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun1.fbx.meta`
    - `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun3.fbx.meta`
- **不修改**：
  - 5 个 .fbx 二进制本体
  - `Assets/ThirdParty/Locomotion/StarterAssets/` 任何文件
  - `ThirdPersonController.cs`
  - `Assets/_Project/Animations/Player/UnomataPlayer.controller`（B1b.1 产出）
  - `PlayerAnimEventReceiver.cs`
  - QF 骨架（GameApp / PlayerSystem / PlayerModel 等）
  - SampleScene.unity（无场景层修改）
- **规则边界备注**：严格解读"`Assets/ThirdParty/` 只读"包含 .meta，但 B1a 切 RifleGirl Humanoid Avatar 时已修改过第三方 fbx 的 .meta（importer 的 rig/avatar 设置），本 change 沿用同一口径，仅向 `clipAnimations[].events` 数组追加数据，不动 rig/avatar/animation type 等结构性 importer 设置
- **后置 change 解锁**：B1c.2 `unity-audio-system-qf`（QF 化 Audio 出口，把脚步/落地音事件接到 PlayerAnimEventReceiver → Command → AudioSystem 链路）
- **specs**：`openspec/specs/character-controller/spec.md` 通过本 change 的 delta 增加 1 条新 requirement
- **文档同步（归档时执行）**：
  - `Docs/DEVELOPMENT_PLAN.md` Phase 2.2 C1 任务行勾选
  - `Docs/TODO.md` B1c.1 段落标注归档日期
  - `Docs/AboutTheAnimation.md` 末段"相关已知遗留（音效）"更新为已修复并指向归档目录

## Why

B1a 归档时只切了 PlayerArmature 的 Humanoid Avatar，Animator Controller 仍指向 `Assets/ThirdParty/Locomotion/StarterAssets/.../StarterAssetsThirdPerson.controller`，Base Layer 的 Idle / Walk / Run / Jump / InAir / Land 全部仍是 StarterAssets 自带 Mecanim 通用动画。视觉上 RifleGirl 模型在用"陌生的"基础动画，与 PRAGMATA 持枪机甲战斗调性不符。本 change 把 Base Layer 动画素材统一切到 RifleGirl + FemaleRunnerAnimset 系列（已 Phase 0 整理就位），让 Base Layer 整体风格收敛到角色主美术风格。

本期严格限定为"换 Motion 引用，不改状态机拓扑、不改参数名、不动第三方文件"，避免与后续 B1b.2（瞄准层）/ B1b.3（输入 QF 化）耦合。

## What Changes

- 用 `AssetDatabase.CopyAsset` 把 `StarterAssetsThirdPerson.controller` 复制为 `Assets/_Project/Animations/Player/UnomataPlayer.controller`（独立 GUID，与 StarterAssets 解耦）
- 在 `UnomataPlayer.controller` 的 Base Layer 内逐节点替换 Motion 引用：
  - `Idle Walk Run Blend` BlendTree[0/1/2] → `R_Idle.fbx` / `R_Walk.fbx` / `R_Run.fbx` (RifleGirl/Normal)
  - `JumpStart` (AnimationClip) → `R_Jump_AirR.fbx` (FRA/Jumps/Jump, 1.167s)——apply 期 D10 方案 Y 替换：原选 `R_Jump_2h.fbx` 因关键帧分布与 ThirdPersonController 瞬时物理不匹配，改用滞空姿态素材让 JumpStart 视觉职责降级为"滞空姿态预览"
  - `InAir` (AnimationClip) → `R_Jump_AirL.fbx` (FRA/Jumps/Jump, 1.167s)
  - `JumpLand` BlendTree[0/1/2] (按 Speed 0/2/6 分站立/走/跑落地) → `R_Land_2h.fbx` / `R_Land_ToRun1.fbx` / `R_Land_ToRun3.fbx` (FRA/Jumps/Land + LandToRun)
- 状态机拓扑、所有 State / Transition / 参数 (`Speed` / `MotionSpeed` / `Grounded` / `Jump` / `FreeFall`) 保持完全一致（4 个顶层 State，无 Fly 节点）
- **Animator State Speed**：仅 `JumpStart.speed = 3.0`（让 R_Jump_AirR 1.167s 实际播 0.389s 与 SA 节奏对齐），其余 3 个 State 保持默认 1.0
- **5 条 transition 全部保持 SA 原值**：apply 期曾尝试调 InAir→JumpLand offset 与 JumpStart→InAir 三字段，最终全部撤回
- 把场景中 `PlayerArmature` 根对象 Animator 组件的 Controller 字段从 `StarterAssetsThirdPerson` 切换到 `UnomataPlayer.controller`
- **apply 期补充修正**：
  - 新增 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs`，提供空 `SwitchSocket(string)` stub 吞掉 RifleGirl fbx 内嵌的动画事件，挂载到 PlayerArmature 上（消除 Console 红色错误 spam）
  - **JumpStart Motion + Speed 联合方案（D10 方案 Y + speed 联合，最终方案）**：
    - Motion 由原计划的 `R_Jump_2h.fbx` 改为 `R_Jump_AirR.fbx`（滞空姿态素材）：R_Jump_2h 关键帧分布（蹬地帧 25%）与 ThirdPersonController 瞬时起跳物理不匹配，任何 speed 调参都无法对齐"动画蹬地帧"与"物理起跳瞬间"
    - Speed 由默认 1.0 改为 3.0：让 R_Jump_AirR 1.167s 实际播放压缩到 0.389s，与 SA Jump.fbx 0.400s 节奏对齐，避免 SA 状态机调参在长素材上失效（apply 期发现 R_Jump_AirR 1.0 倍速时 JumpStart 几乎填满整个物理滞空 0.8s，落地后才慢悠悠播 JumpStart→InAir→JumpLand 链路）
    - JumpStart 视觉职责降级为"滞空姿态预览"，与 InAir 的 R_Jump_AirL 配对（同源同设计，过渡天然契合）
    - R_Jump_2h 在素材库保留，未来若引入"蓄力起跳"物理可恢复使用
- ~~**不调整 Animator State Speed**：R_Jump_2h 1.2s 偏长的潜在手感问题留 Play 自测后单独评估，本期不做缓解~~ → ✅ 维持原决策（apply 期曾尝试调 JumpStart.speed=3.0 + transition 三字段，最终全部撤回，由换 motion 素材根本解决）
- ~~**不调整 Animator State Speed**：R_Jump_2h 1.2s 偏长的潜在手感问题留 Play 自测后单独评估，本期不做缓解~~ → explore 期决策已在 apply 期反悔（见 design.md D10）

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `character-controller`: 新增"Base Layer 动画素材归属"requirement——B1a 归档后 Animator Controller 仍指向 SA 自带版本，本 change 切到项目自有 `UnomataPlayer.controller`，Base Layer 6 槽 Motion 切到 RifleGirl/FemaleRunnerAnimset 风格。第三方 controller 文件保持只读

## Impact

- **资产**：
  - 新增：`Assets/_Project/Animations/Player/UnomataPlayer.controller` + .meta（独立 GUID）
  - 新增目录：`Assets/_Project/Animations/Player/`（若不存在）
  - 新增脚本：`Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs` + .meta（apply 期补充，吞 SwitchSocket 事件）
  - 修改：`Assets/_Project/Scenes/SampleScene.unity`（PlayerArmature.Animator.runtimeAnimatorController 字段 + 挂载 PlayerAnimEventReceiver 组件）
- **不修改**：
  - `Assets/ThirdParty/Locomotion/StarterAssets/` 任何文件
  - `Assets/ThirdParty/Characters/Player/CombatGirls/` 任何文件
  - `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/` 任何文件
  - `ThirdPersonController.cs`（Animator 参数读取写法不变，5 参数完全兼容）
  - QF 骨架（GameApp / PlayerSystem / PlayerModel 等）
  - QFrameworkValidator 等运行时验证脚本
- **不破坏 B1a 已立的契约**：
  - PlayerArmature.Animator.Avatar 仍是 `Humanoid_FAvatar`
  - Geometry 子对象仍 SetActive(false) 保留备份
  - MagicaCloth2 布料物理无影响（Animator 切换不动子对象）
- **后置 change 解锁**：B1b.2 `unity-character-aim-layer`（在本 change 产出的 `UnomataPlayer.controller` 上扩展 UpperBodyAim 层）
- **specs**：`openspec/specs/character-controller/spec.md` 通过本 change 的 delta 增加 1 条新 requirement
- **文档同步（归档时执行）**：
  - `Docs/DEVELOPMENT_PLAN.md` Phase 2.1 C2 任务行勾选
  - `Docs/TODO.md` B1b.1 段落标注归档日期

## ADDED Requirements

### Requirement: 角色基础动画 fbx 内嵌脚步与落地 AnimationEvent

为让 ThirdPersonController.cs 的私有 `OnFootstep(AnimationEvent)` 与 `OnLand(AnimationEvent)` 方法在 RifleGirl/FRA 素材播放期间被 Unity SendMessage 触发，5 个角色基础动画 fbx 的主 clip MUST 在 .meta 中嵌入对应 AnimationEvent。事件数据 MUST 写入 .fbx.meta 文件而非 fbx 二进制本体。

具体要求：

- `R_Walk.fbx` 主 clip MUST 包含 2 个 `OnFootstep` AnimationEvent，分布在脚步触地相位（程序化标定优先，经验值 `[0.25, 0.75]` 兜底）
- `R_Run.fbx` 主 clip MUST 包含 2 个 `OnFootstep` AnimationEvent（程序化标定优先，经验值 `[0.20, 0.70]` 兜底）
- `R_Land_2h.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- `R_Land_ToRun1.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- `R_Land_ToRun3.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- 全部事件 `messageOptions` MUST = `SendMessageOptions.DontRequireReceiver`
- 全部事件 `functionName` 拼写 MUST 与 ThirdPersonController.cs 内方法名完全一致（`OnFootstep` / `OnLand`，区分大小写）
- 5 个 fbx 二进制本体 MUST NOT 被修改（git status 显示 .fbx 不变，仅 .fbx.meta modified）
- 5 个 fbx 的 ModelImporter 其他配置（rig / avatar / animation type / clipAnimations 数量与名称等）MUST 不变

#### Scenario: 走路状态触发脚步音

- **WHEN** PlayerArmature 处于 `Idle Walk Run Blend` state 且 Speed 参数 ≈ 2（走路）
- **THEN** R_Walk clip 每播放一周期 SendMessage 调用 `OnFootstep` 2 次
- **AND** ThirdPersonController.OnFootstep 私有方法触发 `AudioSource.PlayClipAtPoint` 播放 FootstepAudioClips 中随机一段
- **AND** 听感上每次脚步音对应脚部触地视觉

#### Scenario: 跑步状态触发脚步音

- **WHEN** PlayerArmature 处于 `Idle Walk Run Blend` state 且 Speed 参数 ≈ 6（跑步）
- **THEN** R_Run clip 每播放一周期 SendMessage 调用 `OnFootstep` 2 次
- **AND** 节奏比走路明显加快

#### Scenario: 站立落地触发落地音

- **WHEN** PlayerArmature 进入 `JumpLand` state 且 Speed 参数 ≈ 0
- **THEN** R_Land_2h clip 在 time = 0 时刻 SendMessage 调用 `OnLand` 1 次
- **AND** ThirdPersonController.OnLand 私有方法触发 `AudioSource.PlayClipAtPoint` 播放 LandingAudioClip
- **AND** 整个落地过程仅响一次

#### Scenario: 走步落地与跑步落地分别触发落地音

- **WHEN** PlayerArmature 进入 `JumpLand` state 且 Speed 参数 ≈ 2 或 ≈ 6
- **THEN** 对应的 R_Land_ToRun1 / R_Land_ToRun3 clip 在 time = 0 时刻 SendMessage 调用 `OnLand` 1 次
- **AND** 整个落地过程仅响一次（不因 BlendTree 内部 weight 切换而重复触发）

#### Scenario: receiver 未挂载或方法被改名时无 Console 错误

- **WHEN** ThirdPersonController.cs 上的 `OnFootstep` / `OnLand` 私有方法因任何原因（如未来重构）被移除或改名
- **THEN** AnimationEvent 触发但不抛出 `'PlayerArmature' AnimationEvent 'OnFootstep' on animation '...' has no receiver!` 红色错误
- **AND** Console 保持清洁

#### Scenario: 第三方 fbx 二进制保持只读

- **WHEN** 本 change 应用完成
- **THEN** `git status -- Assets/ThirdParty/` 仅显示 5 个 .fbx.meta 为 modified
- **AND** 5 个对应 .fbx 文件 MUST NOT 出现在 modified 列表
- **AND** 其他 ThirdParty 文件（StarterAssets / CombatGirls 模型 / FRA 其他 clip 等）MUST NOT 出现在 modified 列表

#### Scenario: ModelImporter 其他配置保持不变

- **WHEN** 本 change 应用完成
- **THEN** 5 个 fbx 的 `ModelImporter.animationType`（Humanoid）/ `avatarSetup` / `clipAnimations.Length` / 各 clip name 字段保持与 change 应用前一致
- **AND** 仅 `clipAnimations[].events` 数组发生变化

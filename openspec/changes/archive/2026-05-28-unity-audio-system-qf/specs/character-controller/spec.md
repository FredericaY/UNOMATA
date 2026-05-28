## MODIFIED Requirements

### Requirement: PlayerArmature 提供 SwitchSocket 动画事件接收器

PlayerArmature 根对象 SHALL 挂载提供 `void SwitchSocket(string slot)` 方法的 MonoBehaviour，吞掉 RifleGirl R_Idle / R_Walk / R_Run 等 fbx 内嵌的 `SwitchSocket` AnimationEvent，避免 Console 红色错误 spam。

`PlayerAnimEventReceiver` SHALL 同时实现 `IController`（`GetArchitecture() => GameApp.Interface`），在接收到 `OnFootstep(AnimationEvent)` / `OnLand(AnimationEvent)` 时转发 QF Command，实现音频出口 QF 化。

`OnFootstep` 与 `OnLand` 回调 MUST 包含 `animationEvent.animatorClipInfo.weight > 0.5f` 过滤条件，防止动画过渡期副层误触发。

#### Scenario: 接收器组件存在

- **WHEN** 选中 SampleScene 中 PlayerArmature，查看 Inspector 组件列表
- **THEN** 存在 `PlayerAnimEventReceiver` 组件（位于 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs`）

#### Scenario: SwitchSocket 方法被反射调用不抛错

- **WHEN** Play Mode 下播放 R_Idle / R_Walk / R_Run（任一会触发 `SwitchSocket` AnimationEvent 的 clip）
- **THEN** Console 不输出 `AnimationEvent 'SwitchSocket' on animation '...' has no receiver` 红色错误

#### Scenario: OnFootstep 转发 PlayFootstepCommand

- **WHEN** fbx AnimEvent 触发 `OnFootstep`，且 `animatorClipInfo.weight > 0.5f`
- **THEN** `PlayerAnimEventReceiver` 调用 `this.SendCommand(new PlayFootstepCommand(transform.position))`

#### Scenario: OnLand 转发 PlayLandCommand

- **WHEN** fbx AnimEvent 触发 `OnLand`，且 `animatorClipInfo.weight > 0.5f`
- **THEN** `PlayerAnimEventReceiver` 调用 `this.SendCommand(new PlayLandCommand(transform.position))`

#### Scenario: 过渡期 weight ≤ 0.5f 时不触发 Command

- **WHEN** 动画过渡期间副层 `animatorClipInfo.weight ≤ 0.5f` 时触发 AnimEvent
- **THEN** `PlayerAnimEventReceiver` 不发送任何 Command，无音频播放

---

### Requirement: 角色基础动画 fbx 内嵌脚步与落地 AnimationEvent

为让 `PlayerAnimEventReceiver.cs` 的 `OnFootstep(AnimationEvent)` 与 `OnLand(AnimationEvent)` 方法在 RifleGirl/FRA 素材播放期间被 Unity SendMessage 触发，5 个角色基础动画 fbx 的主 clip MUST 在 .meta 中嵌入对应 AnimationEvent（B1c.1 已完成；本 requirement 更新接收方描述）。

具体要求：

- `R_Walk.fbx` 主 clip MUST 包含 2 个 `OnFootstep` AnimationEvent，分布在脚步触地相位（程序化标定优先，经验值 `[0.25, 0.75]` 兜底）
- `R_Run.fbx` 主 clip MUST 包含 2 个 `OnFootstep` AnimationEvent（程序化标定优先，经验值 `[0.20, 0.70]` 兜底）
- `R_Land_2h.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- `R_Land_ToRun1.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- `R_Land_ToRun3.fbx` 主 clip MUST 包含 1 个 `OnLand` AnimationEvent，`time = 0`
- 全部事件 `messageOptions` MUST = `SendMessageOptions.DontRequireReceiver`
- 全部事件 `functionName` 拼写 MUST 与 `PlayerAnimEventReceiver.cs` 内方法名完全一致（`OnFootstep` / `OnLand`，区分大小写）
- 5 个 fbx 二进制本体 MUST NOT 被修改（git status 显示 .fbx 不变，仅 .fbx.meta modified）
- 5 个 fbx 的 ModelImporter 其他配置（rig / avatar / animation type / clipAnimations 数量与名称等）MUST 不变

#### Scenario: 走路状态触发脚步音

- **WHEN** PlayerArmature 处于 `Idle Walk Run Blend` state 且 Speed 参数 ≈ 2（走路）
- **THEN** R_Walk clip 每播放一周期 SendMessage 调用 `OnFootstep` 2 次
- **AND** `PlayerAnimEventReceiver.OnFootstep` 转发 `PlayFootstepCommand` → `AudioSystem` → `AudioBridge._footstepSrc` 播放脚步音
- **AND** 听感上每次脚步音对应脚部触地视觉

#### Scenario: 跑步状态触发脚步音

- **WHEN** PlayerArmature 处于 `Idle Walk Run Blend` state 且 Speed 参数 ≈ 6（跑步）
- **THEN** R_Run clip 每播放一周期 SendMessage 调用 `OnFootstep` 2 次
- **AND** 节奏比走路明显加快，无叠播浑浊感

#### Scenario: 站立落地触发落地音

- **WHEN** PlayerArmature 进入 `JumpLand` state 且 Speed 参数 ≈ 0
- **THEN** R_Land_2h clip 在 time = 0 时刻 SendMessage 调用 `OnLand` 1 次
- **AND** `PlayerAnimEventReceiver.OnLand` 转发 `PlayLandCommand` → `AudioSystem` → `AudioBridge._landSrc.PlayOneShot()` 播放落地音
- **AND** 整个落地过程仅响一次

#### Scenario: 走步落地与跑步落地分别触发落地音

- **WHEN** PlayerArmature 进入 `JumpLand` state 且 Speed 参数 ≈ 2 或 ≈ 6
- **THEN** 对应的 R_Land_ToRun1 / R_Land_ToRun3 clip 在 time = 0 时刻 SendMessage 调用 `OnLand` 1 次
- **AND** 整个落地过程仅响一次（不因 BlendTree 内部 weight 切换而重复触发）

#### Scenario: receiver 未挂载或方法被改名时无 Console 错误

- **WHEN** `PlayerAnimEventReceiver.cs` 上的 `OnFootstep` / `OnLand` 方法因任何原因被移除或改名
- **THEN** AnimationEvent 触发但不抛出 `'PlayerArmature' AnimationEvent 'OnFootstep' on animation '...' has no receiver!` 红色错误
- **AND** Console 保持清洁

#### Scenario: 第三方 fbx 二进制保持只读

- **WHEN** 本 change 应用完成
- **THEN** `git status -- Assets/ThirdParty/` 仅显示 5 个 .fbx.meta 为 modified（B1c.1 已改，本 change 不新增）
- **AND** 其他 ThirdParty 文件 MUST NOT 出现在 modified 列表

## Why

脚步音与落地音当前通过 `ThirdPersonController` 私有方法直接调用 `AudioSource.PlayClipAtPoint`，完全脱离 QFramework 分层，无法统一扩展枪声/命中音/UI 音，跑步脚步声也因音频素材长度参差与触发间距重叠导致听感不均（B1c.1 遗留）。需要建立 QF `AudioModel/AudioSystem` 骨架，把所有音频出口收编进 QF，一次性干净收尾音频部分。

## What Changes

- **新建** `Audio/AudioModel.cs`：持有脚步/落地 `AudioClip[]` 引用，预留 `MasterVolume` 等扩展字段
- **新建** `Audio/AudioSystem.cs`：暴露 `PlayFootstep(Vector3)` / `PlayLand(Vector3)` / `Play(SoundId, Vector3)` 接口；内部 `SendEvent` 解耦 MonoBehaviour 层
- **新建** `Audio/SoundId.cs`：`enum SoundId`，前两项本期实装，其余预留
- **新建** `Audio/AudioEvents.cs`：`FootstepPlayedEvent` / `LandPlayedEvent` / `SoundPlayedEvent`（struct）
- **新建** `Audio/AudioBridge.cs`：MonoBehaviour + IController；`Awake` 注入 Inspector 引用到 AudioModel；持 2 个 `AudioSource`（脚步 Stop+Play 截断治理，落地 PlayOneShot）；订阅 QF Event 出声
- **新建** `Commands/PlayFootstepCommand.cs`：调 `AudioSystem.PlayFootstep(pos)`
- **新建** `Commands/PlayLandCommand.cs`：调 `AudioSystem.PlayLand(pos)`
- **修改** `GameApp.cs`：`RegisterModel<AudioModel>` + `RegisterSystem<AudioSystem>`（顺序：PlayerSystem 之后）
- **修改** `Player/PlayerAnimEventReceiver.cs`：升级为 `IController`；添加 `OnFootstep` / `OnLand` 回调（含 `weight > 0.5f` 过滤）→ `SendCommand`
- **场景** `SampleScene`：新建 `Audio` GameObject 挂 `AudioBridge`，Inspector 赋 FootstepClips/LandingClip；`PlayerArmature` TPC `FootstepAudioClips` 清空、`LandingAudioClip` 置 None

**跑步听感治理**：`_footstepSrc.Stop()` + `Play()` 强制截断上一段，消除 SA wav 长度参差（0.264~0.346s）与 R_Run 0.333s 触发间距重叠的问题。

## Capabilities

### New Capabilities

- `audio-system`：QF 分层音频系统骨架——Model 持资产引用、System 提供播放接口、Event 解耦 MonoBehaviour、AudioBridge 出声；为枪声/命中音/UI 音预留统一扩展点

### Modified Capabilities

- `character-controller`：`PlayerAnimEventReceiver` 升级为 IController，接管 AnimationEvent → QF Command 转发；TPC Inspector 音频字段迁空

## Impact

- **新文件**：`Assets/_Project/Scripts/Gameplay/Audio/`（5 个 cs）、`Assets/_Project/Scripts/Gameplay/Commands/`（2 个 cs）
- **改文件**：`GameApp.cs`、`Player/PlayerAnimEventReceiver.cs`
- **场景**：`SampleScene.unity`（Audio GO + TPC 字段清空）
- **第三方文件**：无改动
- **依赖**：B1c.1（fbx AnimationEvent 已注入 OnFootstep/OnLand）
- **后续接入点**：B2a/B2b 枪声命中音 → `SendCommand<PlaySoundCommand>(SoundId.GunShot, pos)`；Phase 3 UI 音同一接口

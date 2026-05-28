## Context

当前音频路径：`fbx AnimEvent → SendMessage → TPC.OnFootstep/OnLand → AudioSource.PlayClipAtPoint`。
TPC 私有方法直接持有 AudioClip 数组，出口在第三方代码内，无法接入 QF 分层，无法统一扩展。
B1c.1 归档时记录遗留问题：SA 10 段脚步 wav 长度 0.264~0.346s 参差，R_Run 触发间距 0.333s 与最长音重叠，导致跑步听感不均。

目标：把所有音频出口迁入 QF AudioSystem，用 Stop+Play 单 channel 策略根治跑步重叠，同时为后续枪声/命中音/UI 音建立统一扩展骨架。

## Goals / Non-Goals

**Goals:**
- 建立 `AudioModel / AudioSystem / AudioBridge` QF 三层骨架
- 脚步音与落地音完全迁移到 AudioSystem 出口
- 用 Stop+Play 单 channel 策略消除跑步脚步声重叠
- TPC 音频字段迁空（不改 TPC.cs）
- 预留 `SoundId` 扩展点，B2a/B2b/Phase3 可直接接入

**Non-Goals:**
- 枪声/命中音/UI 音本期不实装（仅 enum 预留）
- 空间音频 / 音量衰减 / 混音器（Phase 6 可加）
- `MasterVolume` BindableProperty 绑定 UI 滑块（预留字段，不接 UI）

## Decisions

### D1：AudioSystem 不持有 AudioSource，用 QF Event 解耦

**问题**：`AudioSystem` 是纯 C# `AbstractSystem`，不能持有 `MonoBehaviour` 引用（AudioSource 在 MB 上）。

**方案**：`AudioSystem.PlayFootstep(pos)` 内部调 `this.SendEvent(new FootstepPlayedEvent { Position=pos })`，`AudioBridge`（IController MonoBehaviour）订阅 Event 后操作 AudioSource。

**替代方案**：静态 `AudioManager` 单例持有 AudioSource — 违反 QF 禁止静态单例的规范，否决。

---

### D2：AudioBridge 用 2 个 AudioSource 分别处理脚步/落地

**脚步**：使用单 AudioSource `_footstepSrc`，每次触发先 `Stop()` 再设 `clip` 再 `Play()`，强制截断上一段，彻底消除 R_Run 0.333s 间距与最长音 0.346s 的重叠。

**落地**：使用独立 AudioSource `_landSrc`，调 `PlayOneShot(clip)`——落地频率低（每次跳跃一次），无重叠问题，不需要截断。

**替代方案**：`PlayClipAtPoint`（每次 new 临时 GO）— 不支持 Stop，无法治理跑步重叠，否决。

---

### D3：AudioModel 持 AudioClip 引用，由 AudioBridge.Awake 注入

**问题**：`AbstractModel` 是纯 C# 类，不能序列化到 Inspector。

**方案**：`AudioBridge`（MonoBehaviour）在 `Awake()` 把 Inspector 上的 `[SerializeField]` AudioClip 引用写入 `AudioModel`。`AudioSystem` 运行时从 `GetModel<AudioModel>()` 取 clips。

**时序**：`AudioBridge.Awake → GetModel<AudioModel>()` 触发 `GameApp.Init()`，AudioModel 创建时 clips 为 null；`Awake` 完成后写入，之后所有运行时播放调用均可取到有效 clips。

**替代方案**：ScriptableObject AudioConfig — 引入额外资产层，且 AudioModel 持有 SO 引用仍需 bridge 注入，复杂度更高，否决。

---

### D4：PlayerAnimEventReceiver 升级为 IController，添加 weight 过滤

`PlayerAnimEventReceiver` 需调 `SendCommand`，必须实现 `IController`。

`OnFootstep(AnimationEvent)` / `OnLand(AnimationEvent)` 均加 `animationEvent.animatorClipInfo.weight > 0.5f` 判断，与 TPC 原实现一致，防止动画过渡期副层误触发。

---

### D5：TPC.LandingAudioClip 置 null 安全性

Unity `AudioSource.PlayClipAtPoint(null, pos, vol)` 内部有 null guard，静默返回，无异常无 warning（已通过 Unity 源码确认）。TPC.FootstepAudioClips 清空（Length=0）同理有 `Length > 0` guard，安全。

## Risks / Trade-offs

- **[风险] AudioBridge Awake 注入时序**：若有其他脚本在 `Awake` 阶段（且 Script Execution Order 早于 AudioBridge）就调用 `AudioSystem.PlayFootstep`，此时 clips 尚未注入会无声。→ 缓解：当前无其他 Awake 期音频调用，运行时第一个音频触发发生在 Play Mode 玩家操作后，时序安全。可在 `AudioSystem.PlayFootstep` 加 null guard 防御性检查，仅 Debug.LogWarning。
- **[风险] Stop+Play 截断策略**：极短间隔（< 1 帧）连续触发会导致静音帧。→ 可接受：R_Walk 两事件间距 ~0.6s，R_Run ~0.33s，均远大于 1 帧（0.016s）。
- **[取舍] 随机池缩小**：Stop+Play 已消除重叠，暂不额外筛短 wav，保留 10 段完整随机池。若仍感觉听感问题可后续加筛短逻辑（< 0.3s 子集）。

## Migration Plan

1. 新建 Audio/ 目录下 5 个 cs 文件
2. 新建 Commands/ 下 2 个 Command 文件
3. 修改 GameApp.cs 注册 AudioModel + AudioSystem
4. 修改 PlayerAnimEventReceiver.cs 升级 IController + 添加回调
5. Scene：新建 Audio GO，挂 AudioBridge，赋 Inspector 引用
6. Scene：PlayerArmature TPC FootstepAudioClips 清空、LandingAudioClip 置 None
7. Play Mode 验证：走路/跑步/落地音效正常；禁用 AudioBridge → 音效消失；Console 零红错

**回滚**：恢复 TPC Inspector 字段（重新赋 SA 素材），回退 PlayerAnimEventReceiver.cs，删除 Audio GO。无数据破坏，可随时回滚。

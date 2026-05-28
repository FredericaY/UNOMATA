## Context

B1b.1 (`unity-character-base-anim-swap`, 归档 2026-05-28) 把 PlayerArmature 的 Animator Controller 切到自有 `UnomataPlayer.controller`，base layer 6 槽 motion 全部换成 RifleGirl/FRA 系列素材。SA 自家素材原本在 `Walk.fbx` / `Run.fbx` / `Jump.fbx` 等 clip 内嵌 `OnFootstep` / `OnLand` AnimationEvent，配合 `ThirdPersonController.cs` 的 private `OnFootstep(AnimationEvent)` / `OnLand(AnimationEvent)` 方法（Unity SendMessage 可触发 private 方法）实现脚步声 / 落地声。换素材后 RifleGirl/FRA fbx 内嵌的是 `SwitchSocket(string)` 事件而非 `OnFootstep` / `OnLand`——所有走 / 跑 / 落地音效失声。

B1b.1 apply 期已识别此问题（tasks.md Group 10），归档时记录到 `Docs/AboutTheAnimation.md` 末段，留待后续 patch change 解决。本 change 即该 patch。

ThirdPersonController.cs Inspector 仍持有 `FootstepAudioClips[10]` + `LandingAudioClip` 引用，资产链路完整，唯独缺动画端事件触发。`PlayerAnimEventReceiver.cs`（B1b.1 apply 期补充，挂在 PlayerArmature 上）当前仅吞 `SwitchSocket` 占位。

Audio 系统的 QF 化（建 AudioModel / AudioSystem、迁移 clips 引用、把音频出口收编进 QF 链路）拆到下一个 change `unity-audio-system-qf` (B1c.2)。本 change 锁死"恢复失声"目标，scope 越窄越好，避免 B1b.1 同款 scope 蔓延。

## Goals / Non-Goals

**Goals:**
- 走路（R_Walk）有脚步声，每步触发一次，节奏与脚部触地视觉同步
- 跑步（R_Run）有脚步声，节奏更快
- 落地（R_Land_2h / R_Land_ToRun1 / R_Land_ToRun3）触发一次落地音
- ThirdPersonController.cs 不动，复用其私有 `OnFootstep` / `OnLand` 自播音频
- 5 个 fbx 二进制文件不动，事件仅写入 .meta 文件
- Console 零红色错误，与 B1b.1 终态一致
- 验收基线：B1b.1 终态的 5 条 transition / 4 个 state speed / motion 配置完全保持

**Non-Goals:**
- ❌ 建 AudioModel / AudioSystem / Command（B1c.2 处理）
- ❌ 迁移 ThirdPersonController Inspector 上的 FootstepAudioClips / LandingAudioClip 引用（B1c.2 处理）
- ❌ 修改 ThirdPersonController.cs 任何代码
- ❌ 修改 PlayerAnimEventReceiver.cs（仍只吞 SwitchSocket）
- ❌ 修改 UnomataPlayer.controller 任何 motion / state / transition 配置
- ❌ 补"起跳音"（SA 原项目本就无此线，超 scope）
- ❌ 调整音频音量 / 音色 / 风格（沿用 SA 现有 clips）
- ❌ 修改 SampleScene.unity（无场景层改动）
- ❌ AI 听觉感知 / SoundPlayedEvent 等扩展能力（后续单独 change）

## Decisions

### D1 — 事件注入方式：`ModelImporter.clipAnimations[].events` API

**选项：**
- A：通过 ModelImporter API 注入事件，写入 .meta（**选用**）
- B：在自有 .anim 副本上加事件，让 controller 引用副本而非 fbx 内嵌 clip
- C：写一个 FootstepDriver MB 监 Animator state normalizedTime + 配置相位表
- D：监 root motion 速度阈值估算节拍

**理由：**
- A 工作量极低，事件直接挂在 fbx 主 clip 上，无需新建 .anim 资产或修改 controller 引用
- A 触发精度由相位标定值决定，可程序化测出
- B 需要把 5 个 clip 在项目内复制为 .anim 文件 + 修改 controller 5 处 motion 引用——破坏 B1b.1 的 motion 配置基线，工作量明显更大
- C 需要新增 MB + 配置数据 + 每帧 polling，复杂度更高，与 SA 既有"AnimationEvent 驱动音频"机制不一致
- D 节拍与动画脚步相位非严格同步，走路慢速时易出戏

### D2 — 走 / 跑相位标定：程序化优先 + 经验值兜底

**程序化方案：**
- 通过 `AnimationUtility.GetCurveBindings(clip)` 找到 `LeftFoot` / `RightFoot` 骨骼的 `localPosition.y` 或 world `T.y` curve
- 遍历 curve 在 `[0, length]` 区间，找局部最小值对应的 `time / length` → 即触地相位
- 走路通常 2 个最小值（左右各一），跑步同
- 容错：若 curves 不可解析（e.g. Humanoid generic curves 编码格式特殊），跌回经验值

**经验值（兜底）：**
- `R_Walk` 1.133s loop：相位 `[0.25, 0.75]`
- `R_Run` 0.667s loop：相位 `[0.20, 0.70]`
- 落地：单事件 `time = 0`（state 进入瞬间，落地视觉已对齐）

**理由：**
- Humanoid clip 的 curves 通常以 muscle space 编码，直接读骨骼 transform.y 可能拿不到——所以需要兜底
- 经验值是 SA 自家 Walk/Run 节拍的等效迁移，听感差异可在 Play Mode 微调（apply 期任务里留窗口）

### D3 — `messageOptions = SendMessageOptions.DontRequireReceiver`

B1c.2 上线后 `PlayerAnimEventReceiver` 会扩展 `OnFootstep` / `OnLand` 方法接管音频出口，FootstepAudioClips/LandingAudioClip 引用会从 ThirdPersonController 迁出。届时 ThirdPersonController.OnFootstep 仍会被 SendMessage 触发但因字段空而无声——双 receiver 同时存在不会报错。但若设 `RequireReceiver` 且未来某 receiver 移除/改名，则会触发 Console 红错。`DontRequireReceiver` 让 B1c.1 → B1c.2 切换平滑。

### D4 — 落地事件 time = 0 单触发

JumpLand 是 BlendTree（B1b.1 D3v2），内含 3 个 land clip。BlendTree 进入瞬间会按 `Speed` 参数选定主 clip 并从 time=0 播放——把 OnLand 放在 time=0 等价于"BlendTree 进入即触发一次"，符合落地音预期。

不在 BlendTree 内部加二次音（如调整步音）——SA 原项目也只有单次落地音，保持一致。

### D5 — 程序化执行入口：execute_code 一次性脚本

通过 UnityMCP `execute_code` 跑一段 C# 脚本完成所有 5 个 fbx 的事件注入。脚本结构：

```csharp
// 1. 列出 5 个 fbx 路径 + 每个 fbx 的目标 events
// 2. for each fbx:
//      var importer = AssetImporter.GetAtPath(p) as ModelImporter;
//      var clips = importer.clipAnimations;  // ModelImporterClipAnimation[]
//      // 找主 clip（一般是 clips[0] 或按 name 匹配）
//      // 尝试程序化标定相位（D2 程序化），失败用经验值
//      clips[i].events = newEvents;
//      importer.clipAnimations = clips;
//      importer.SaveAndReimport();
// 3. 验证：AssetDatabase.LoadAllAssetsAtPath 重新加载，确认 events 数组写入成功
```

脚本不写入项目（不放 Editor/ 目录），仅作为 apply 期一次性工具运行。

### D6 — `clipAnimations` 编辑前置条件

ModelImporter 有两种 import 模式：
- Default Take：fbx 内嵌 take 不可改 events
- Override → `clipAnimations` 数组：可改 events

需要先确认 5 个 fbx 是否已为 override 模式。B1a 切 Avatar 时通常会自动切到 override（Humanoid Avatar 配置在 importer 层）。若发现某个 fbx 仍是 default take，需先调 importer.clipAnimations 的副本（克隆 internal animations 数组）写回再改 events。

apply 期 step 1.x 加一步前置检查：每个 fbx 跑 `importer.clipAnimations.Length > 0` 验证。

## Risks / Trade-offs

**[R1] 程序化相位标定可能失败** → Mitigation：经验值兜底；apply 期单测 1 个 fbx（如 R_Walk）确认程序化结果合理后再批处理；失败也可接受经验值

**[R2] BlendTree 内 land clip 切换瞬间触发额外 OnLand** → Mitigation：BlendTree 入口的 land 事件只挂在每个子 clip 的 time=0；BlendTree 切换主 clip 不会重置 time=0（continuous blend），只有 state 整体进入才会从 0 开始；测试时确认"走路落地"vs"跑步落地"只各响一次

**[R3] DontRequireReceiver 屏蔽真实问题** → Mitigation：B1c.1 验收明确要求"听见声音"作为正向证据；DontRequireReceiver 只防 Console spam，不防"无声"

**[R4] 第三方 .meta 改动可能在 Unity 重 import 时被覆盖** → Mitigation：events 数据是 importer 的持久化字段，SaveAndReimport 后会持久写回 .meta 并由 git 跟踪；后续若有人手动重 import 或更新 ThirdParty 包要警示。归档后在 `Docs/AboutTheAnimation.md` 加 .meta 改动告警段

**[R5] 规则口径："ThirdParty 只读"是否包含 .meta** → Mitigation：proposal 已声明沿用 B1a 同口径（B1a 改了 fbx .meta 的 Avatar/Rig 字段）；本 change 仅追加 events 字段，不动结构性 importer 设置；规则原文若有歧义可在归档后建议在 `rules.mdc` 加澄清条款

**[R6] R_Walk / R_Run 经验值与实际脚部视觉不同步** → Mitigation：Play Mode 验收要求"音效与脚部触地视觉同步"，不一致则现场微调相位值（apply 期 step 4.x 留 1 轮微调窗口）；本 change 不追求帧精确同步，"听感自然"即可

**[R7] LandingAudioClip 字段在 PlayerArmature 上可能为空** → Mitigation：apply 期 step 1.x 检查 ThirdPersonController.LandingAudioClip / FootstepAudioClips 字段是否填充；若空则需先在 Inspector 拖 clip 引用（属本 change 范围），不阻塞但要记录

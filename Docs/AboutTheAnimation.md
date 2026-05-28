# AboutTheAnimation — 角色基础动画疑难记录

> 本文档为 `unity-character-base-anim-swap`（B1b.1）apply 期遗留问题的备忘。当时把 SA 自带 Mecanim 通用动画换成 RifleGirl + FemaleRunnerAnimset (FRA) 系列素材，跳跃链路反复调试 7 轮才得到一个"无明显出错但仍有点别扭"的最终方案。本文记录所有诊断过程、各方案试错、最终配置、未来彻底解决的可行方向，等动画问题需要彻底优化时回来读。

归档参考：`openspec/changes/archive/2026-05-28-unity-character-base-anim-swap/`（含 design.md D1~D10 详细决策、tasks.md Group 12~16 完整诊断 trace）。

---

## 当前最终方案（B1b.1 归档时态）

```
Animator State                Motion                  Speed       与 SA 相比
──────────────────────────────────────────────────────────────────────────
Idle Walk Run Blend           BlendTree:              1.0         motion 替换
                                R_Idle (3.000s loop)
                                R_Walk (1.133s loop)
                                R_Run  (0.667s loop)
JumpStart                     R_Jump_AirR (1.167s)    3.0  ⚠️    motion 替换 + speed 改
InAir                         R_Jump_AirL (1.167s)    1.0         motion 替换
JumpLand                      BlendTree:              1.0         motion 替换
                                R_Land_2h     (1.167s)
                                R_Land_ToRun1 (0.600s)
                                R_Land_ToRun3 (0.867s)

5 条 transition：全部 SA 原值（conditions / hasExitTime / exitTime / duration / offset 一字不差）
```

辅助脚本 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs` 挂在 PlayerArmature 上，提供空 `SwitchSocket(string)` stub 吞掉 RifleGirl fbx 内嵌的换枪挂点事件（避免 Console 红错 spam）。

---

## 核心矛盾（必读）

**SA 状态机调参专为 SA 自家素材调优**：

```
SA 自家素材时长:                替换素材时长:
  Jump.fbx       0.400s          R_Jump_2h    1.200s   (3.00x SA)
  InAir.fbx      2.667s          R_Jump_AirL  1.167s   (0.44x SA)
  Run_N_Land.fbx 0.667s          R_Land_2h    1.167s   (1.84x SA)

SA 状态机调参（5 条 transition）是对应这套时长 + 关键帧分布优化的：
  - JumpStart→InAir: exitTime=0.6637 / duration=0.4705 / offset=0.6088
  - InAir→JumpLand:  exitTime=0.3015 / duration=0.0976 / offset=0.0803
  - …

ThirdPersonController.cs 物理：
  v0 = sqrt(JumpHeight × -2 × Gravity) = sqrt(1.2 × 30) = 6 m/s
  上升 0.4s + 下降 0.4s = 0.8s 总滞空
```

→ 换素材但保 SA 状态机调参 → 调参与新素材时长不匹配 → 视觉 / 手感问题。

---

## 7 轮试错过程（精简）

### 试错 0：换 motion 完毕，直接 Play

- **JumpStart=R_Jump_2h / InAir=R_Jump_AirL / JumpLand=R_Land_2h**（最初设计），全 SA 调参，全 speed=1.0
- **现象**：用户反馈"落地半空卡一下再下落"
- **误诊**：以为是 InAir→JumpLand offset=0.08 在 ToRun 系列 land 上引发"半空姿态错位"
- **教训**：用户主观感受的卡顿位置定位不准，不能直接信"落地"二字

### 试错 1：D8 — InAir→JumpLand offset 0.08 → 0.0

- **现象**：用户反馈仍卡顿，截图定位真实卡顿点是 **JumpStart→InAir**（不是落地）
- **修正**：撤回 D8

### 试错 2：D9 v1 — JumpStart→InAir 三字段 0.85 / 0.10 / 0.0（speed=1.0）

- **意图**：让 R_Jump_2h 几乎播完才转，短过渡，从 AirL 起手帧播
- **现象**：用户反馈"角色保持半空起跳姿态卡顿"
- **诊断**：speed=1.0 时 0.85 × 1.2s = 1.02s 离开 JumpStart，比物理 0.8s 滞空还长，整个空中卡在 JumpStart
- **修正**：改 D9 v2

### 试错 3：D9 v2 — JumpStart→InAir 三字段 0.20 / 0.10 / 0.0（speed=1.0）

- **意图**：让 JumpStart 只播 24% 就让位给 InAir，AirL 滞空循环主导整段空中
- **现象**：用户反馈"角色被抬高后才在半空莫名其妙蹬地"
- **诊断**：R_Jump_2h 24% 处的关键帧是"准备/屈膝"，**真正蹬地帧约在 30~40%**——被截掉了。物理已经离地但动画在播"准备"，物理在升空时动画播出"蹬地"造成时序错乱
- **修正**：撤回 D9，转向 D10

### 试错 4：D10 v1 — JumpStart.speed=3.0（保留 R_Jump_2h，撤回 D9 transition 字段）

- **意图**：让 R_Jump_2h 1.2s 实际播 0.4s 与 SA 节奏对齐，所有 transition 还原 SA 原值
- **现象**：用户反馈"接近顶点处蹬腿"
- **诊断**：D10 后 SA 原 transition `duration=0.4705s` 比压缩后 0.4s JumpStart 还长，过渡尾段 JumpStart clip 已结束并卡在末帧——R_Jump_2h 末帧（下落起手姿态）形似蹬地
- **修正**：D10b 试图配套调 transition

### 试错 5：D10b — JumpStart.speed=3.0 + transition 0.85 / 0.10 / 0.0

- **现象**：用户反馈"仍在顶点蹬腿"
- **深度分析揭示根本矛盾**：

```
R_Jump_2h.fbx 内部时序假设："先准备 → 蹬地 → 离地 → 顶点 → 下落"
ThirdPersonController 物理模型："按 Space 瞬间给 verticalVelocity"

→ R_Jump_2h 的"蹬地动作"始终在 clip 25% 关键帧处发生
  无论 speed 调多少，这个比例不变
→ 物理上"蹬地"是按下 Space 那一帧（瞬时发力，无准备阶段）
→ 任何 speed/transition 调参都无法把"动画 25% 蹬地帧"对齐到"物理 0~0.05s 起跳瞬间"
  除非 speed > 6.0（实际播 < 0.2s），但这又导致 R_Jump_2h 后 75%（顶点+下落）完全废弃
```

- **修正**：放弃 R_Jump_2h，转向方案 Y

### 试错 6：方案 Y — JumpStart.motion = R_Jump_AirR（speed=1.0，撤回 D10/D10b）

- **意图**：JumpStart 视觉职责降级为"滞空姿态预览"，不再尝试演蹬地
- **现象**：用户反馈"落地又有卡顿感"
- **诊断**：R_Jump_AirR 1.167s × SA 原 exitT 0.6637 = **0.775s 才触发 JumpStart→InAir**，比物理 0.8s 滞空还接近——整段空中 JumpStart 几乎不让位给 InAir，落地后状态机才慢悠悠播 JumpStart→InAir→JumpLand 链路
- **修正**：方案 Y + speed=3.0 联合（最终方案）

### 试错 7（最终）：方案 Y + D10 联合 — JumpStart.motion=R_Jump_AirR + speed=3.0

- **现象**：用户反馈"虽然还是有点别扭，但已经没有明显出错的感觉"
- **结论**：归档落地

---

## 关键洞察（按重要性排序）

### 1. 同步动画 / 骨骼速度 ≠ 关键帧分布对齐

D10 v1 的核心思想是"speed=3.0 让素材时长与 SA 等长"，但**等长 ≠ 关键帧分布兼容**。R_Jump_2h 不论 speed 调多少，"蹬地帧"始终在 25% 处——若素材本身的"动作叙事"（先准备再蹬地）与目标物理（瞬时起跳）冲突，speed 救不了。

→ 选素材时不仅看时长，还要看**关键帧分布是否与目标物理节奏匹配**。

### 2. SA 状态机调参 ≠ 通用调参

explore 期判断"SA transition 调参为通用节奏"——错。SA 调参（exitTime/duration/offset 全部）是针对 SA 自家 0.4s/2.6s 素材专门调的，换素材后这些值的语义全部改变。

→ 换 motion 时**必须同时校对 transition 调参的素材依赖**，不能假设 SA 默认值还合理。

### 3. transition `duration` 的绝对秒数语义陷阱

`hasFixedDuration=true` 时 `duration` 是**绝对秒数**而非归一化比例。SA 原值 `duration=0.4705s` 是为 0.4s 的 JumpStart 设计的——0.4705s 比 JumpStart 整个时长还长，过渡尾段 clip 已结束并卡在末帧。换长素材（如 R_Jump_AirR 1.167s）时 0.4705s 比例上变小（40%），看似无问题但实际播放时序又变了。

→ `fixedDuration=true` 的 transition 在换素材后必须重新校对绝对时间是否合理。

### 4. 动画长度 vs 物理滞空时长的"夹逼关系"

```
JumpStart 实际播放时长 × exitTime > 物理滞空时长
→ 状态机来不及在物理落地前转出 JumpStart
→ 落地后才慢悠悠播 JumpStart→InAir→JumpLand 全链路
→ 用户感知"落地卡顿"

JumpStart 实际播放时长 × exitTime < 物理滞空时长
→ JumpStart 早早让位给 InAir, AirL 滞空循环主导整段空中
→ 用户感知合理
```

简单算式：`JumpStart 实际时长 × 0.6637 < 0.8s` 必须成立。  
当前配置：`1.167 / 3.0 × 0.6637 = 0.258s < 0.8s` ✓

→ 改 JumpHeight / Gravity / 跳跃物理时，**必须重新校对 JumpStart 时长**。

### 5. fbx 内嵌 AnimationEvent 是"预设的回调钩子"

不只 SwitchSocket（CombatGirls 设计）会触发问题，SA 自家素材的 OnFootstep / OnLand 也是这种机制。换素材时 fbx 内嵌事件可能：

- **a) 多余事件触发 Error**：CombatGirls SwitchSocket 在 RifleGirl fbx 上触发 → PlayerArmature 无对应方法 → Console 红错
- **b) 缺失事件导致功能失效**：SA 的脚步声依赖 OnFootstep 事件触发 → RifleGirl/FRA fbx 没有该事件 → 走/跑/落地音效全部失声

→ 换 fbx 素材时**必须扫一遍 fbx 内嵌 AnimationEvent 列表**：

```csharp
foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
  if (clip is AnimationClip c) foreach (var e in c.events)
    Debug.Log(c.name + " event=" + e.functionName);
```

---

## 未来彻底解决的方向（按工程量从小到大）

### 方向 A：换更适合"瞬时起跳物理"的 Jump 素材

寻找类似 SA Jump.fbx 0.4s 那种**短而利落、无"准备阶段"** 的起跳素材：
- Asset Store 搜 "instant jump" / "snappy jump"
- 看 `Mixamo` 跳跃动画
- 自家 Animator Window 截 R_Jump_2h 的 25%~50% 区段做新 clip（涉及修改第三方资产，需先复制到 _Project/）

成功标准：JumpStart 播完整 clip 时长 ≈ 0.4s，关键帧分布与"物理瞬时起跳"对齐，弃掉当前 R_Jump_AirR + speed=3.0 的折中方案。

### 方向 B：引入"蓄力跳跃"物理

按住 Space 蓄力 0.3s 再起跳——R_Jump_2h 这种"准备+蹬地"素材的设计意图与新物理对齐，可启用：

```csharp
// 修改 ThirdPersonController.cs (复制到 _Project/, 不动第三方)
if (Input.GetButton("Jump")) _chargeTime += Time.deltaTime;
if (Input.GetButtonUp("Jump") && _chargeTime > 0.05f) {
  // 起跳，蓄力越长跳越高
  _verticalVelocity = Mathf.Sqrt(JumpHeight * (1 + _chargeTime) * -2f * Gravity);
}
```

R_Jump_2h 的"准备帧"在玩家按住 Space 期间播，"蹬地帧"在松手起跳那一帧播——动画与物理时序对齐。

涉及改 ThirdPersonController.cs（fork 到 _Project/）+ 状态机加 ChargeJump 状态 + 新 transition cond。是较大改动，可能要单开 change。

### 方向 C：Animation Rigging 程序化蹬地叠加

在按 Space 后 0.05s 内用 Animation Rigging 强制叠加"弯腿+伸腿"姿态——不依赖 fbx 关键帧。完全摆脱"素材时序与物理瞬时性矛盾"。

涉及 Unity Animation Rigging 包（已在 manifest）+ 自写 IK 链 + Two Bone IK Constraint。技术栈较新，需调研。

### 方向 D：JumpStart State 完全去除

从状态机层面移除 JumpStart，让 `Idle Walk Run Blend → InAir` 直接由 `Jump=true` cond 触发。优势：去掉了"虚假起跳动作"环节，结构最干净。

代价：完全无任何起跳视觉表现，按 Space 瞬间切到滞空姿态。

涉及修改状态机拓扑（违反 B1b.1 Non-Goal 第一条），需要更大范围 change。但本质是"承认 R_Jump_AirR 当前方案就是 JumpStart 的等价物"——既然 JumpStart 现在播的也是滞空姿态，干脆删掉 State。

---

## 验证清单（未来改进时复用）

每次动画素材或物理参数变更后，跑这套清单：

```
[ ] 进 Play, 站立观察 Idle 是否正常播放、是否有 Console 错
[ ] WASD 走路 → R_Walk 正常播
[ ] Shift+WASD 跑步 → R_Run 正常播
[ ] 站立跳 (Space) → 起跳→空中→落地 链路是否流畅
[ ] 走路跳 → 同上 + 落地 BlendTree[1] R_Land_ToRun1 衔接 Walk
[ ] 奔跑跳 → 同上 + 落地 BlendTree[2] R_Land_ToRun3 衔接 Run
[ ] Console 仍 0 红错 0 警告
[ ] git status -- Assets/ThirdParty/ 仍空 (无第三方文件改动)
[ ] 自动校对 5 条 transition 与 SA 原值差异 (用 design.md 内 execute_code 脚本)

如发现"卡顿"主观反馈：
[ ] 截图反馈，看 Animator window 哪个 state 高亮、进度条到哪
[ ] 别想当然把"卡顿"归因到落地——可能是 JumpStart→InAir 切换期
[ ] 用 Animator.GetCurrentAnimatorStateInfo + GetNextAnimatorStateInfo 实时打点
```

---

## 相关已知遗留（音效）

~~走/跑/落地音效完全失声——SA 的脚步声依赖 fbx 内嵌的 `OnFootstep` AnimationEvent + ThirdPersonController.cs 的 `OnFootstep(AnimationEvent)` 方法（同 OnLand）。RifleGirl 与 FRA 的 fbx 内嵌的是 `SwitchSocket` 事件而非 `OnFootstep`，换素材后所有走/跑/落地音效失效。~~

→ ✅ **已由 `unity-character-footstep-events` (B1c.1) 部分修复**（归档 2026-05-28，参考 `openspec/changes/archive/2026-05-28-unity-character-footstep-events/`）。通过 `ModelImporter.clipAnimations[].events` API 给 5 个 fbx 主 clip 的 .meta 注入 `OnFootstep` / `OnLand` 事件，全部 `messageOptions = DontRequireReceiver`：
- R_Walk: `OnFootstep@0.3246s, 0.9055s`（程序化 LF/RF Y 最低点标定，与脚部触地视觉对齐）
- R_Run: `OnFootstep@0.1809s, 0.5259s`（同上）
- R_Land_2h / R_Land_ToRun1 / R_Land_ToRun3: `OnLand@0`

ThirdPersonController.cs 私有 `OnFootstep` / `OnLand` 自动通过 SendMessage 触发，资产层 FootstepAudioClips[10] / LandingAudioClip 不变，走/跑/落地音效全部恢复。

### 第三方 .meta 改动告警

B1c.1 按"沿用 B1a 同口径"修改了 5 个第三方 fbx 的 .meta 文件（仅 `clipAnimations[].events` 数组追加，不动 rig/avatar/animation type 等结构性 importer 设置）。具体涉及：
- `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Walk.fbx.meta`
- `Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Normal/R_Run.fbx.meta`
- `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/Land/R_Land_2h.fbx.meta`
- `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun1.fbx.meta`
- `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/LandToRun/R_Land_ToRun3.fbx.meta`

**警示**：未来更新 CombatGirls / FemaleRunnerAnimset 第三方包 / 手动 Reimport / Unity 升级触发 schema 升级时，`clipAnimations[].events` 注入数据可能丢失。需要在新文件上重跑 B1c.1 注入脚本（保存在 `openspec/changes/archive/2026-05-28-unity-character-footstep-events/tasks.md` Group 3+4）。

### 跑步听感"间隔不一致"遗留 → B1c.2 治理

B1c.1 apply 期发现：跑步听感节奏不均匀。诊断如下：
- 事件相位本身严格 LF/RF Y 最低点对齐脚部视觉，**不是相位问题**
- 根因：SA 10 段脚步音 wav 长度 0.264s ~ 0.346s 参差不齐（差 31%），与 R_Run 0.333s 触发间距重叠最长音；TPC 用 `Random.Range` 抽段加剧节拍混乱
- 走路 0.567s 间距 > 最长音 0.346s，听感可接受

属音频素材 + 调度策略问题，B1c.1 不动 cs/scene 故无法治理。**转 B1c.2 (`unity-audio-system-qf`) 用 AudioSystem 接管音频出口时根本治理**，治理思路（4 选 1，B1c.2 apply 期决定）：
- (a) 单 channel `AudioSource.PlayOneShot` 强制截断上一段
- (b) 按音长筛段（仅抽 ≤ 0.3s 的子集）
- (c) AudioImporter 批量调短/淡出（修改第三方 .meta，沿用 B1c.1 同口径）
- (d) 跑步触发改单脚（事件减半，间距 0.667s）

---

_文档创建：2026-05-28，change `unity-character-base-anim-swap` 归档时_

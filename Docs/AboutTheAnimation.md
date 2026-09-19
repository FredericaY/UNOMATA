# AboutTheAnimation — 角色基础动画疑难记录

> 当前状态：2026-09-18 用户确认移动/越肩瞄准与跳跃修复全部验收通过，47/47 项完成，正式规格已同步归档。本文按阶段保留排查与验收过程；各阶段的“待验收/未完成”描述只代表当时状态，移动瞄准结论见“最终用户验收与归档”；2026-09-19 射击反馈验收及归档见文末新增记录。

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

## 2026-09-17 重启：瞄准与枪口方向

用户确认：当时人物瞄准动作和枪口朝向始终做不好，是搁置主因。当前 B1b.4 `aim-ik-rig-constraint` 保持活动，未完成最终验收。

**已有尝试与当前起点**

- 手写 spine yaw/pitch + 多套 bias 的方案因动画固定 pose、轴向和 idle/walk 差异难以校正，已切换为 Multi-Aim Constraint。
- 历史记录显示约束曾因层级解析警告不起效；重挂 AimRig 后开始工作。这里只记录本模型的诊断，不将特定层级要求视为所有 Humanoid 的通用规则。
- 当前代码：AnimatorAimBridge 驱动 UpperBodyAim 和 Rig 权重，AimTargetDriver 在 LateUpdate 更新目标，StrafeController 控制下半身 yaw。
- SampleScene 中 Aim_Spine3 offset 当前为 `(-50, 20, -35)`，不同于历史“清零待标定”的尾注。该值仅为当前序列化事实，尚无通过记录。
- 手骨 forward 与目标角度不等于枪口角度。重新标定前需先确定实际枪口位置/前向和参考轴。

**下一轮诊断与验收步骤（尚未执行）**

1. 使用固定相机、固定距离与正前方目标，记录枪口方向、Rig 权重、三段脊椎参数和 Console 警告；保留起始场景副本或可回退差异。
2. 分别观察 Rig=0 与 Rig=1 的静止 pose，确认是约束未参与、轴向/offset 偏差，还是持枪姿态本身的偏差。
3. 检查相机、AimTarget、Animator/PlayableGraph 和 Strafe 的实际求值顺序。DefaultExecutionOrder 不能单独证明 LateUpdate 早于动画图求值。
4. 固定 yaw，检查中立及上下俯仰；固定 pitch，检查小幅死区内转向和大幅脚追身；观察是否扭曲、翻转或抖动。
5. 对照 idle、前进、后退、斜向移动。一次只调整一个因素，记录前后结果；不要通过扩大偏移掩盖不同姿态的根因。
6. 检查约 0.15 秒进入/退出渐变、连续瞄准切换，以及移动/跳跃/音频回归。
7. 补输入禁用与重新启用验收：按住移动/瞄准时禁用 PlayerController，检查残留 Model 输入，重新启用后检查回调。Look 由适配器直通，单独记录其行为。
8. 参数在 Edit Mode 保存并重新 Play 复核；Console 无新增未解释错误/警告，记录视图或视频。用户确认手感后才完成任务勾选、同步规格和归档。

“无需手写 bias”不等于已证明 Multi-Aim 可以在所有姿态自动对齐枪口。若现有目标需武器/手臂约束或不同瞄准模型，先明确方案与范围，再更新活动变更；本轮不选择新实现。

## 2026-09-18 移动与越肩瞄准修复实施

> 当前结果见文末“最终交付记录”；以下中间测试与失败记录保留诊断历史，不作为最终版本结论。

当前 change：[fix-over-shoulder-aim-locomotion](../openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md)。本节记录本次实施证据，不覆盖旧验收；当前尚未完成角色接线与整体视觉验收。

### 基线与入口

- 正式 SampleScene 初始 SHA256：7CE51F4EE7FD2C36701126C80F5185FAE7DF8E498F35555E58F5000DD6FDC7C9。
- 场景、场景 meta、Animator Controller、Mask 与输入资产的本地基线副本保存在 .utmp/aim-repair/baseline/；scene-bindings.json 记录对象属性及持久引用标识。临时证据目录被 Git 忽略。
- 新增 Editor 隔离入口 AimRepairDiagnostics.SampleClips / RenderWeapon：在 PreviewScene 中复制角色并采样，不保存正式场景；结束销毁副本、相机和临时图。
- 十个瞄准/移动/转身 clip 已完成 17 个相位采样与固定视角截图；单独采样无 Animator 绑定警告。高速移动录像、布料及完整动作矩阵仍待完成，不能把素材采样等同任务 1.2 全部通过。
- 手动 AnimatorControllerPlayable 与外部 RigBuilder 图的四组隔离组合（有/无子 Animator、有/无 Rig）均能驱动动作且无绑定警告；尚未完成真实帧时序/布料/音频验证，不算任务 1.3 通过。

### 问题与验证追踪

| 项目 | 当前状态 | 后续任务 |
| --- | --- | --- |
| 枪管未作为最终测量对象、固定 50m 脊椎目标 | 已实现几何与快照基础，尚未接角色 | 2、4、8 |
| 手动回转与动画求值顺序冲突 | 项目运动适配代码已加入，尚未替换场景组件 | 3、4.4 |
| FL/FR/BL/BR 文件名误导、方向与步速适配 | 已采样原始方向/相位；高速观感待验证 | 1.2、5 |
| 60°死区、大幅脊椎 offset、双手握枪 | 尚未替换旧场景配置 | 3.3、4 |
| Animator 绑定警告 | 关闭 Animation 窗口、重载正式场景并重新 Play 后仍出现；不能认定为旧预览残留。隔离副本单 clip/手动控制器+Rig 均未出现，继续定位正式绑定路径 | 1.4 |
| Priority 启停补丁、Offset 同步假设、剔除 | 源码事实已核对，相关运行检查尚未全部完成 | 1.5、4.5、6.1 |
| 相机碰撞、生命周期、脚步音适配 | 尚未实施/验收 | 6、7 |
| 正式场景的自动扣血验证、不可达 Fly | 尚未处理 | 5.4、7.3 |

### 本次基础测试

AimGeometryValidation.Run 在隔离测试架构与临时物理场景执行：536 个断言通过，最大几何角误差 0.0000264219°。涵盖 256 组有限距离/枪口偏移/本地枪管朝向组合、不可达和非有限输入、当前帧发布/过期读取/场景代际、无副作用查询、遮挡/内部起点及实际物理自体/触发器过滤。

测试首先发现 Unity FromToRotation 对极小角度的近似使 200m 视差补偿丢失；实现改用 atan2 构造精确小角度旋转后按原阈值重跑通过，没有降低测试门槛。此结果仅证明几何和基础契约，不代表场景枪口、握把、帧时序或视觉手感已通过。

编译期间 MCP 曾报告 WebSocket 重连警告；每次 Domain Reload 后已重新检查项目与就绪状态，不能将工具重连日志视为角色修复结果。

### 用户实施中反馈与方案修订

用户实测指出三项仍需修复：非瞄准奔跑枪穿入身体中央；举枪静止/移动缺少过渡；举枪快跑的加速走路感不自然。已纳入当前 change 的 4.6、5.5、5.6，尚未通过最终复验。

上一轮冷启动 26 项 smoke 检查通过，枪管峰值误差约 0.0000289°，双手位置峰值约 0.018mm；该结果只覆盖当时的量化检查，不能否定本轮用户的视觉问题。左手握把使用统一向护木后部回移 8cm 的标定解决已测近距/俯仰可达性，没有为各动作分别加枪口偏移。

新修订采用当帧基础动画持枪姿态作为非瞄准武器来源，分开基础动画推进和约束求值；移动参数连续混合，快跑下半身复用普通 Run 并做方向适配。测试录像中途因运行状态改变取消，showcase 仅完成 5/6，不计为完整通过。另有原生资源释放警告正在定位，清理门槛仍保持未完成。


### 正式场景接入与反馈专项（2026-09-18，仍待整体验收）

- `SampleScene` 已接入 `Prefabs/Player/ShoulderAimPlayer.prefab` 与 `Animations/Player/Aiming/ShoulderAim.controller`。场景 GUID 保留，相机 Follow/表现引用和 Audio 已重绑，自动扣血的 QFrameworkValidator 已移除。原场景诊断前后 SHA256 相同，随后才执行正式迁移。
- 非瞄准武器来源改为本帧基础手部姿态；瞄准位置以躯干校正后的当帧肩部为基准，再解算枪口与双臂。基础动画和 Rig 使用同图 dt / 0 两遍，只推进一次时间。两个独立图的实验因握把回归失败而撤回，不属于交付方案。
- 位移方向和走跑比例采用连续阻尼参数；快跑使用普通 Run，其他七方向从该 Run 的脚步轨迹和腿部肌肉曲线烘焙，周期仍为约 0.667s，再按实际 5.335m/s 匹配约 5.11m/s 的原始步幅。未修改第三方动作；新的脚步相位随实际项目动作重新采样。
- 反馈专项 13/13：非瞄准站立/奔跑、瞄准起停、走跑切换、左右反向及八方向快跑；录制越肩/全身视角。6/6 showcase 覆盖静止、侧走、侧跑、后跑、移动跳落和俯视。最终正式场景基础 smoke 26/26 通过。以上不能代替完整距离矩阵和用户观感验收。
- 几何/状态独立测试本次 **547 项通过**；包含随机武器局部旋转、远点、过滤、内部起点、过近/后方、配置非法与修复、过期帧、只读查询和场景代际。
- 正式场景边界测试 **29 项通过**：枪口阻挡/内部起点、近点/无命中及恢复、三次单表现/整体角色启停、缺引用单次诊断与恢复、模拟失焦、快速举放、180°进入、静止小转/大转。模拟失焦不算物理窗口切换证据。
- 原输入验证器本次重跑 **150 项通过**，15 类错误配置为预期诊断；真实角色输入消费另行验证，不用 Model 测试替代实际位移。
- 原 Animator 绑定警告已定位到旧 RigBuilder 启动绑定路径：移除嵌套子 Animator 仍复现；启动前仅移除 RigBuilder 则警告为空，其余原运动/瞄准组件保留。旧控制器独立自动/手动采样、替换 Animator 的四组均无警告。修复方案移除旧图绑定，使用受控图，正式角色检查持续无该警告。证据：`old-without-child-animator.txt`、`old-without-rig.txt`、`binding-final-isolation.json`。
- 临时证据在 `.utmp/aim-repair/`；仅本次实际跑完的报告记通过。120fps 首轮有一项仅达到约 80fps，虽姿态达标仍记失败，必须复跑。


本轮补充：几何测试扩展为 **548 项通过**，最终手部不可达不能发布 Ready。正式输入场景 **11 项通过**：实际 Move/Sprint 在回调帧消费、按住跳跃跨落地不连跳、释放重按、真实右键到举枪、PlayerInput/Map 停用及恢复；共 2 次跳跃/2 次落地，实测最高离地约 1.146m。布料异步初始化完成后，458/458 采样有效并位于最终姿态之后。初始化第一帧未就绪的初版测试明确记失败，修订仅把运动采样起点放到布料初始化断言之后，不隐藏运行中失效。

边界报告进一步扩展为 **43 项通过**：新增普通/瞄准相机近墙避让、相机内部起点及恢复、主相机不可见时持续姿态更新、音频三轮启停无残留回调且每个命令仅播放一次。当前还需完整距离矩阵、多帧率复验及用户对视觉/听感的确认。

最终审查补修进入瞄准的连续 Yaw：过渡过程中跨 180° 不再重选反向路径。边界报告现为 **50 项通过**，追加动态进入跨界、阈值内多次往返及稳定跨界。先前矩阵在 57/417 主动取消且清理完成，不计完整通过；修复后从头运行。

连续转身补修：原先迟滞间隙使转身状态和脚步相位不断中断，180°/s 转身 3 秒实际为 0 声。现表现转速经过短时平滑，项目转身动作循环且运动时立即退出，音频跟随实际转身姿态。4/4 转身专项通过，左右及 3m 上/下俯仰各 3 秒均有 12 次接触声，最大握把误差约 2.42mm；有全身录像。边界扩展为 **53 项通过**，实测约 59.58fps，新增真实碰撞墙停止/无假脚步。此修订前 169/417 的矩阵已主动取消并清理；最终版本重新完整运行。


### 当前素材映射与复跑入口

| 原素材 | 项目动作 / 用途 | 采样事实 |
| --- | --- | --- |
| R_AimIdle | AimIdle；瞄准上身与静止 | 2.567s，无根平移 |
| R_AimWalk_F / B | AimWalk_F / B | 1.133s；前 1.523m/s、后 1.452m/s |
| R_AimWalk_FL / FR | AimWalk_L / R | 1.133s；实际沿 ±X，约 1.397m/s，不能按文件名当斜向 |
| R_AimWalk_BL / BR | 已采样，当前不接入 | 实际同样沿 ±X，作为已排查的另一组侧步 |
| R_AimJog | 已采样，当前快跑不使用 | 0.8s、2.933m/s；不再把加速 AimWalk/AimJog 当作快跑交付 |
| 普通 R_Run | Run 与 Run_45…315 | 原周期 0.667s、约 5.11m/s；七方向只适配腿部，实际运动仍 5.335m/s |
| R_AimTurn_L90 / R90 | TurnLeft / TurnRight | 原周期 1s；项目副本循环，按平滑实际角速度匹配，位移开始即退出 |
| FRA R_Jump_AirR / AirL | JumpStart / InAir | 1.167s；起跳播放率 3，物理仍 JumpHeight=1.2、Gravity=-15 |
| FRA Land_2h / ToRun1 / ToRun3 | Land / LandWalk / LandRun | 1.167 / 0.6 / 0.867s；落地由实际接地计数触发一次音效 |

脚步标定存于 `Footsteps.asset`：运动采用沿位移方向脚部最前相位作为接触代理，转身采用脚部最低相位，并与运行录像核对；最终听感仍由本轮用户验收确认。运行时不依赖某个 clip 权重超过 0.5，也不恢复供应商事件播放。

显式诊断：在 Unity 菜单 `UNOMATA/Validation/Aim Diagnostics` 指定 PlayerArmature，可查看真实枪管/中心目标/遮挡线和握把误差；窗口只在 Editor 生效。`AimGeometryValidation.Run()` 与 `AimSavedSceneAudit.Run()` 在 Edit Mode；`AimRuntimeValidation.Start(mode, fps, record)`、`AimBoundaryValidation.Start(fps)`、`AimInputSceneValidation.Start()` 在 Play Mode，单个验证器运行完成后再启另一个。`matrix` 为完整组合，`categories` 为帧率代表/极限组合，`feedback` 为三项反馈及方向运动录像，`turns` 为连续转身录像。原 `InputBaselineValidation.Start()` 仍保留独立输入故障矩阵。

动画副本生成由 `AimRepairSetup`（原始基线迁移）、`AimDirectionalRunSetup`（普通 Run 的方向腿步）、`AimRepairAnimationSetup`（状态/脚步标定）负责；修改配置后必须重跑相应报告，不从历史“通过”自动继承验收。第三方、Core、包版本和 rules.md 未修改。


### 最终完整矩阵

最终运行时固定版本的 `matrix-60` **417/417 全部通过**，合计 **42,265 帧**；实测每用例 **59.4865–60.0812fps**。覆盖 3/10/50/200m × -30/0/35/70° × 静止、八方向走跑、原地/八方向移动跳落，以及 180°/s 连续转视角。静止采样 3 秒，运动 1.4 秒（覆盖完整周期），跳落 2 秒；正常样本不排除 Invalid/Transition。

- 枪口对目标最大角误差 **0.0002254°**。
- 双手位置峰值 **6.49mm**；报告逐用例保留两手的旋转峰值。
- 发布方向与实际枪管最大角误差 **0.00000125°**。
- 无失效样本、帧不一致或重复时间推进；各移动动作有脚步，空中无假脚步，跳跃每次落地一声。
- 同一运行时源码哈希在矩阵期间未变。后续只添加 Editor 验证入口和录像场景用例，代码与资产最终仍需保存/重载审计。
- 首次保存引用审计 **101 项通过**；最终完整体验仍等待用户复验，不据此同步主规格或归档。

完整矩阵后的过渡专项发现后退/侧跑举放枪的状态误判：期望姿态可达，但当前枪尚在旋转，Complete 的前后方检查把过渡误报为 Invalid。修订仅对 Transition 跳过这个当前枪向检查，保留几何目标可达、实际遮挡与 Ready 的安全检查；稳态 Ready 路径和姿态求解未变。追加相反枪向在 Transition / Ready 两种状态的独立断言，并重跑前/后/侧奔跑举放枪及边界专项，不把此前失败录像算最终通过。

收尾 30fps 有一帧正常 Run1 因左手可达余量不足触发 InvalidPose，该轮不计通过。将同一武器的左支撑握点从 8cm 回收改为 10cm 回收（再向后 2cm），所有动作统一使用，不添加动作相关补偿。枪口/右手求解不变；手部可达性重新回归，失败样本现在同时保留实际握把误差，避免只记录状态。


### 单帧脱手根因与最终约束修正

新增失败帧实际数值后，120fps Run1 捕捉到左手偏离 **0.4716m**，排除了“只是握点余量不足”的解释。根因在 Animation Rigging 1.3.1 的肘提示步骤：两个投影方向接近反向时，通用 FromToRotation 由极小叉积/备用轴确定旋转，旋转轴并非肩到手轴，会移动本已求解的末端。独立例子中旧算法让固定末端偏移 **0.811m**。

`StableArmIKConstraint` 复用包内双骨解算和绑定，仅关闭原肘提示步骤并替换为绕肩到手轴的有向角旋转，随后保留手掌旋转。第三方包不修改。新增 256 随机轴 × 6 个角度的“末端不动/肘平面对准”回归，旧算法 838 个组合出现超过 1cm 的端点漂移，新步骤最大漂移 **6.7e-8m**；几何/状态/肘提示合计 **3623 项通过**。替换后的首轮 120fps 场景 30/30 通过，左手峰值约 **0.0115mm**，实测最低约 119.2fps。完整矩阵与收尾组将按该最终约束版本重新计数，早期通过不冒充该版本结果。

稳定约束版本的矩阵在 250 个有效通过用例后收到窗口失焦，后续记录为 Inactive/InactiveContext；这是正常的生产失焦保护，不能记成骨骼回归通过。自动姿态夹具现在在拥有模拟输入期间同步模拟焦点，真实焦点用例仍独立。补跑只保留原报告中通过的整项用例，并校验全部运行脚本、动画、预制体和场景 SHA256 与原版本一致；失败/未运行用例完整重跑，不从用例内剔除坏帧。


### 最终交付记录（等待用户复验）

当前交付以 **StableArmIKConstraint + 统一 10cm 左支撑握点** 为准。三个用户反馈均已实施：非瞄准武器跟随当帧右手；站立/移动/走跑/换向连续混合；快跑复用普通 Run 的下半身与方向适配。正式入口仍为 `Assets/_Project/Scenes/SampleScene.unity`，当前已保存并退出 Play Mode。

| 本次最终验证 | 结果 |
| --- | --- |
| 几何、只读契约与稳定肘提示 | 3623 项通过；包括旧算法反向投影回归 |
| 完整矩阵 | 417/417，通过 42256 帧；实测 57.51–60.08fps |
| 枪口 / 发布方向峰值 | 0.00022719° / 0.00000125° |
| 双手位置峰值 | 0.119mm（门槛 10mm）；旋转门槛 2°通过 |
| 三项反馈及奔跑举放枪录像 | 16/16；包含前跑、后退、侧跑举放枪，实测 59.82–60.01fps |
| 30 / 120fps 动作 | 各 30/30；含全部运动类别、极限组合与连续 180°/s 转向 |
| 30 / 120fps 边界 | 各 53 项；实测约 29.86 / 120.20fps |
| 原输入回归 | 本轮实际重跑 150 项、15 类故障配置通过 |
| 真实场景输入 / 布料 | 11 项通过；2 跳/2 落地，458/458 布料采样有效且位于最终姿态之后 |
| 保存引用审计 | 103 项通过；稳定约束、握把、相机和音频引用完整 |
| 独立启动/退出 | clean-start-2 / 3 均通过：HP=100，布料有效，Animator 无绑定警告，0 测试残留；启动与退出 Console 均 0 错误/警告 |

低帧率边界测试的两项度量已校正：转身突跳按实际角速度统计（固定每帧 30°会误判 30fps 的正常约 989°/s 进入动作）；不可见持续更新的期望帧数与等待时长使用相同帧率换算。方向连续性、0.35s 收敛和原枪口/握把门槛保留，生产代码没有因这些测试修订而变化。

矩阵保留了失焦前完整通过的 250 项，按 109 个脚本/动画/预制体/场景文件哈希核对后，重跑其余受影响和未执行用例；最终 417 个名称唯一、无遗漏，正常用例无 Invalid/Transition 排除、帧不一致或重复时间推进。最终摘要为 `.utmp/aim-repair/acceptance-summary.json`。旧 `final-validation.json` 保留最后一次测试断言失败的流水，不把它改写为一次性全绿；修订测试后的边界和输入结果在各自最新报告，汇总以版本核对后的上述摘要为准。

恢复 NativeLeakDetection 到原先 Enabled 时曾一次性输出旧追踪批次（含大量 Found 0 信息），日志保存在 `leak-mode-transition.log`；保持检测开启后又完成两次独立启停，未再出现该批次或新的泄漏告警。未以关闭检测隐藏问题。检测设置与运行帧率/后台模式均已恢复。

预览文件：`.utmp/aim-repair/aim-feedback-review.gif`，固定全身与越肩双机位，实际约 30fps 的录像帧序列。原始帧和失败/取消历史仍保留在 `.utmp/aim-repair/`，不进入 Git。

尚待本轮用户确认的项目只有人工验收与其后的规格收口：侧身/握枪/步态/跳落/镜头/布料的观感、脚步与落地听感，以及真实切走窗口再返回。自动测试中的模拟焦点不冒充真实切窗。用户确认前保持 change 未完成，不同步为已接受主规格、不归档、不提交或推送。


### 用户复验修订：仅纯向前举枪奔跑

用户认为举枪侧向与后退跑的姿势不自然，明确决定放弃这类移动方式。原来方向跑的数值通过不代表视觉验收通过；旧 417 项报告和旧 GIF 属于旧规则，不能当作当前交付的认可。

新规则：举枪时 W+Shift 可以跑；A/S/D 及斜向组合（W+A、W+D 等）即使保持 Shift 也走路。加入侧向/后退输入同帧封顶 2m/s；回到纯 W 自动恢复前跑，退出瞄准恢复原普通奔跑。Shift 原始输入不改写，跳跃物理不变。方向参数保留平滑过渡，所有非前向的方向 Run 实验片段从控制器/子树/脚步配置断开。配置生成入口也已同步，防止重新挂回。

本轮只对改变的速度策略、方向切换、动画引用及输入链重新验证；旧几何/约束结果保留历史身份，不把旧整矩阵冒充新规则的整矩阵重跑。新专项报告和记录位于 `.utmp/aim-repair/sprint-rule-*`。


前向奔跑限制修订已完成：`sprint-rule-60` 20/20，`categories-30/120` 各 30/30，实际键鼠输入链 17 项、保存引用审计 82 项通过。举枪 W+Shift 实测约 5.335m/s，其余七方向约 2m/s；限制期间原始 Sprint 改写次数为 0，混入 Run 或方向 Run 的帧数为 0。包括持续 Shift 的方向切换、侧向/后退举放枪、空中侧移，以及非瞄准八方向奔跑保留。正式 SampleScene 已保存重载，方向 Run 引用 0，Console 0 错误/警告。

记录位于 `.utmp/aim-repair/forward-only-acceptance.json`，当前动作对比为 `forward-only-review.jpg`，原始录像帧为 `sprint-rule-60/frames/`。此版本的统计与前面被用户否定的方向跑版本分开；没有宣称重新执行旧规则的 417 项全矩阵。动画采样工具也修正为先移除临时副本中的 PlayerMotor，再清除其输入依赖，重新采样无删除依赖组件错误。


### 用户复验修订：起跳同帧响应与连续空中动作

**问题已在当前场景确认。** 修复前 60fps 原地跳：起跳首帧根高度约 0.096m，膝角仍约 14.8°/6.7°，基本为地面姿态；约 0.10s 才明显收腿。随后 JumpStart(AirR) → InAir(AirL) 重播相反起手，右膝从约 131°变为约 17°，出现空中重新伸腿。此现象在基础动画替换历史已出现；当前音频只读动作/接地，不控制动画，因此不能把本次时序归因于音频播放。

修复方式：起跳信号在表现协调者中显式启动 0.04s 混合，使第一个物理离地帧即参与腿部动作；JumpStart/InAir 共用项目 AirR 片段与 AirPhase，按 Motor 的竖直速度推进上升 0.15→0.40、下降 0.40→0.90，相位不因换状态回到开头。自然下落用后段 0.75→0.90。原 Animator Jump 布尔过渡成对移除，状态按竖直速度及实际 Grounded 衔接。配置在 RifleAim.asset，生成入口 AimJumpAnimationSetup；不改供应商、跳高 1.2、重力 -15、即时起跳及音频物理触发。

本次 30/60/120fps 跳跃专项各 8/8 通过：非瞄准/瞄准下原地、向前跑、侧移跳与自然下落。原地跳首帧开始混合，三档帧率首帧膝角变化的最小值约 58.6° / 30.5° / 13.9°；上升阶段最大单帧反向膝角变化均小于 0.6°，不再出现约百余度的伸腿重播。根高度峰值约 1.10 / 1.145 / 1.175m，差异来自原物理的离散步长；配置未变。每个用例落地音一次，空中脚步 0。实际输入/布料 17 项复跑通过，2 跳/2 落地，627/627 布料采样有效；前向奔跑限制保持。

证据：`.utmp/aim-repair/jump-baseline-60`、`jump-check-30/60/120`、`jump-timing-summary.json`；等时刻侧视对比 `jump-timing-comparison.jpg`。本次补充的是跳跃姿态时序验收，之前仅检查状态、枪口和落地次数的通过不能代替它。最终观感仍等待用户确认。

跳跃修订后补跑 `categories-60`：30/30 通过，包含八方向移动跳、极限俯仰及连续转向；保存后重新加载正式场景，引用审计 79 项通过（AirL 不再被生产控制器引用，相比前次减少对应的 3 项素材检查），另行确认两空中状态共享 AirR、AirPhase 启用、旧 Jump 参数移除及跳跃配置持久化。

跳跃修订后的独立启动/退出也已通过：角色初始化完成，HP=100，布料有效，Animator 无绑定警告，0 测试残留，启动和退出 Console 均 0 错误/警告；正式场景保留在 Edit Mode 供用户 Play 复验。change 严格校验及共享文档链接检查通过。

## 最终用户验收与归档（2026-09-18）

用户在最后一轮跳跃修订交付后明确确认“验收都通过”，授权勾选任务、同步文档、归档以及 Git 提交与推送。该确认覆盖此前待人工验收的真实切窗恢复、脚步/落地听感以及角色侧身、枪向、双手、步态、跳落、镜头和布料观感；记录来源为用户确认，不声称代理额外执行了物理切窗或新增逐键录像。

最终交付保留八方向行走、仅纯向前举枪奔跑、非瞄准原奔跑，以及同帧起跳和物理相位驱动的连续跳落。上述最后两次修订以其专项和回归记录为证据；417 项矩阵保留为较早实现版本的实际测试结果，不冒充最终修订后完整矩阵重跑。

47/47 项任务完成。audio-system、character-controller、player-input-model 与新增 over-shoulder-aim 已同步正式规格；同步时补齐旧 Animator 路径、运动消费者和音频配置/生命周期描述，全部保留原有场景覆盖。新 change 替代旧 aim-ik-rig-constraint 的实现方案；旧目录及 22/37 历史不改写，不单独同步旧 delta。

本轮交付不包含射击、命中、伤害、后坐力、换弹、敌人或骇入；这些是后续开发范围。诊断报告/录像保留在本地 .utmp，源码中的验证入口及本文摘要随交付保存。

## 射击反馈接入（2026-09-19，已获用户最终验收）

`unity-shooting-damage-loop` 在 PlayerAimPresentation 的唯一求值链中增加轻微后坐：收到射击事实后，下一帧在 PrepareAimFrameCommand 之前偏移武器枢轴，再完成枪口求解与双臂约束，快照发布后不改枪。调试起点为 2.5cm 位移、0.09 秒恢复，无持续镜头上抬/随机散布，不重复推进动画时间。

R_Shoot 与 R_AimIdle_AutoShoot 均约 0.967 秒且含 SwitchSocket。为保持独立武器和双手约束，本次选用程序化回弹，不执行原挂点事件、不修改 fbx。26 组带实际开火的预检、60fps 完整 417 组矩阵、30/120fps 各 30 组动作/最差组合均已通过，另有 6 组双机位样片；射击画面/听感已获本次用户验收。详细结果维护于 [射击基线](SHOOTING_BASELINE.md)，不改写上方历史验收。

### 射击反馈用户确认与归档（2026-09-19）

用户明确回复“验收通过”，射击动作、声音搭配及真实切窗体验的待确认项据此收口；自动运行证据与用户确认分开记录，不补写代理未执行的试听或物理输入日志。[unity-shooting-damage-loop](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/tasks.md) 33/33 项完成，正式规格已同步归档。下一份 change 接入敌人模型、骨架、动画与简单敌人 UI；本次按用户要求不提交或推送 Git。

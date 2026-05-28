## 1. 前置确认（无操作风险）

- [x] 1.1 通过 `mcpforunity://editor/state` 资源确认 Unity Editor 处于 Edit Mode（非 Play、非编译中）
- [x] 1.2 用 `find_gameobjects search_term=PlayerArmature` 定位 PlayerArmature instanceID（=-1880）
- [x] 1.3 通过 `mcpforunity://scene/gameobject/<id>/component/ThirdPersonController` 资源确认 `LandingAudioClip` 与 `FootstepAudioClips` 字段已填充（FootstepAudioClips.Length=10、LandingAudioClip=Player_Land.wav，均 SA 自家素材，已就位）
- [x] 1.4 通过 `manage_asset action=get_info` 验证 5 个目标 fbx 资产路径存在（探针脚本 1.5 一并完成）
- [x] 1.5 通过 `execute_code` 跑探针脚本：5 个 fbx 全部 animationType=Human、clipAnimations.Length=1、importAnimation=True，均为 override 模式
- [x] 1.6 记录 5 个 fbx 改前 baseline（详见 Group 9.0）：
    - R_Walk: clip name=Walk / length=1.133s / events=[SwitchSocket@0 RequireReceiver]
    - R_Run: clip name=Run / length=0.667s / events=[SwitchSocket@0 RequireReceiver]
    - R_Land_2h: clip name=R_Land_2h / length=1.167s / events=[]
    - R_Land_ToRun1: clip name=R_Land_ToRun1 / length=0.600s / events=[]
    - R_Land_ToRun3: clip name=R_Land_ToRun3 / length=0.867s / events=[]
    - **关键**：R_Walk / R_Run 已有 1 个 SwitchSocket 事件（RequireReceiver），追加 OnFootstep 时 MUST 保留原 SwitchSocket 不变（PlayerAnimEventReceiver 已挂载吞此事件，不会报错）

## 2. 走 / 跑相位标定（程序化优先，经验值兜底）

- [x] 2.1 通过 `execute_code` 加载 R_Walk 主 clip，遍历 `AnimationUtility.GetCurveBindings(clip)` 找 Animator.LeftFootT.y / RightFootT.y curve；200 点等距采样找全局最小值 → LF=0.2864 / RF=0.7990
- [x] 2.2 R_Run 同 step 2.1 → LF=0.2714 / RF=0.7889
- [x] 2.3 程序化结果合理性检查：R_Walk 间距 0.487、R_Run 间距 0.482，均落在 0.5±0.2，合理 ✓
- [x] 2.4 经验值（兜底）未启用——程序化结果合理，直接采用
- [x] 2.5 实际采用相位（程序化）：
    - R_Walk: [0.2864, 0.7990]
    - R_Run:  [0.2714, 0.7889]

## 3. R_Walk / R_Run 注入 OnFootstep 事件

> apply 期把 Group 3 + Group 4 合并为一次性 5-fbx 批处理脚本，幂等（去重已有 OnFootstep/OnLand）+ 保留 SwitchSocket 原事件 + 按 time 排序写回。

- [x] 3.1 通过 `execute_code` 加载 R_Walk 的 ModelImporter，按 name='Walk' 定位主 clip
- [x] 3.2 构造 `AnimationEvent[]` 数组（追加 2 个 OnFootstep 事件）：
    - time=0.3246s (phase 0.2864), fn=OnFootstep, opt=DontRequireReceiver
    - time=0.9055s (phase 0.7990), fn=OnFootstep, opt=DontRequireReceiver
- [x] 3.3 写回 `clipAnimations[i].events`（保留原 SwitchSocket@0），`importer.SaveAndReimport()`
- [x] 3.4 R_Run 同：追加 time=0.1809s + time=0.5259s 两个 OnFootstep
- [x] 3.5 验证：R_Walk events=[SwitchSocket@0, OnFootstep@0.3246, OnFootstep@0.9055]；R_Run events=[SwitchSocket@0, OnFootstep@0.1809, OnFootstep@0.5259]，全部 DontRequireReceiver ✓

## 4. R_Land_2h / R_Land_ToRun1 / R_Land_ToRun3 注入 OnLand 事件

- [x] 4.1 通过 `execute_code` 加载 R_Land_2h 的 ModelImporter，按 name='R_Land_2h' 定位主 clip（events 原本为空）
- [x] 4.2 构造 `AnimationEvent[]`（1 个 OnLand）：time=0, fn=OnLand, opt=DontRequireReceiver
- [x] 4.3 写回 + SaveAndReimport
- [x] 4.4 R_Land_ToRun1 同 step 4.1~4.3（name='R_Land_ToRun1'）
- [x] 4.5 R_Land_ToRun3 同 step 4.1~4.3（name='R_Land_ToRun3'）
- [x] 4.6 验证：3 个 land fbx 主 clip events=[OnLand@0 DontRequireReceiver]，长度=1 ✓

## 5. ModelImporter 其他字段未变验证

- [x] 5.1 通过 `execute_code` 重新加载 5 个 fbx 的 ModelImporter 终态校对：5/5 全部 animationType=Human、clipAnimations.Length=1、clip.name 与 baseline 一致、loop/mirror 与 baseline 一致 ✓
- [x] 5.2 仅 `clipAnimations[].events` 数组发生变化（R_Walk/R_Run 由 1→3，3 个 land 由 0→1）

## 6. Play Mode 验证

- [x] 6.1 `manage_editor action=play` 进入 Play Mode（用户自测）
- [x] 6.2 Console 0 红错 0 警告 ✓（特别确认无 `OnFootstep`/`OnLand` has no receiver 错）
- [x] 6.3 站立 Idle：无脚步音 ✓
- [x] 6.4 WASD 走路：每步触发脚步音 ✓
- [x] 6.5 Shift+WASD 跑步：脚步音节奏明显加快 ✓
- [x] 6.6 站立跳跃 → 落地：仅响一次落地音（R_Land_2h）✓
- [x] 6.7 走路跳跃 → 落地：仅响一次落地音（R_Land_ToRun1）✓
- [x] 6.8 奔跑跳跃 → 落地：仅响一次落地音（R_Land_ToRun3）✓
- [x] 6.9 听感判断：走路听感可接受；**跑步听感不均**——根因不在事件相位（已确认事件 time 严格按程序化标定 LF/RF Y 最低点对齐，与左右脚视觉一致），而在 SA 10 段脚步音 wav 长度参差不齐（0.264s ~ 0.346s）+ TPC 用 `Random.Range` 抽段 + R_Run 触发间距 0.333s 与最长音 0.346s 重叠。属音频素材自身 + 调度策略问题，**超本 change scope，留 B1c.2 用 AudioSystem 接管音频出口时根本治理**
- [x] 6.10 退出 Play Mode

## 7. 第三方文件改动范围验证

- [x] 7.1 `git status -- Assets/ThirdParty/` 输出仅含 5 个 .fbx.meta 为 modified ✓
    - R_Walk.fbx.meta / R_Run.fbx.meta / R_Land_2h.fbx.meta / R_Land_ToRun1.fbx.meta / R_Land_ToRun3.fbx.meta
- [x] 7.2 5 个对应 .fbx 二进制未出现在 modified 列表 ✓
- [x] 7.3 其他 ThirdParty 文件（StarterAssets / CombatGirls 模型 / FRA 其他 clip / Locomotion 等）未出现在 modified 列表 ✓
- [x] 7.4 `git status -- Assets/_Project/` 含 SampleScene.unity 与 Animations/ + PlayerAnimEventReceiver.cs 等 untracked，但属 **B1a/B1b.1/B1b.2 未 commit 的工作产物**，不是本 change 引入；本 change 自身不产生任何 _Project 改动

## 8. 听感微调（apply 期 v1 / v2 试错记录，最终回退到 v1）

> apply 期 v1 听感反馈"脚步声间隔不一致"，曾尝试均值法 ± 0.25 修正（v2）听感未改善——根因不在事件相位，而在 SA 音效素材长度参差不齐 + 跑步触发间距太短（0.333s）与最长音重叠（0.346s）+ TPC `Random.Range` 抽段。该问题**超本 change scope**，留 B1c.2 Audio QF 化时根本治理。最终回退到 v1（程序化 LF/RF 最低点标定）保留与左右脚视觉对齐的设计。

- [x] 8.1 v2 诊断（已撤销）：以为相位间距不等导致听感不均，改均值法 ± 0.25
- [x] 8.2 v2 重写 events（已撤销）：R_Walk=[0.2927, 0.7927]、R_Run=[0.2801, 0.7801]
- [x] 8.3 v2 SaveAndReimport（已撤销）
- [x] 8.4 v2 Play Mode 用户复测：跑步听感仍不均匀，根因排查指向音频素材本身（而非事件相位）
- [x] 8.5 **撤回 v2，回退到 v1**：R_Walk=[0.2864, 0.7990]、R_Run=[0.2714, 0.7889]，与 LF/RF Y 最低点视觉对齐
- [x] 8.6 v1 终态校对：R_Walk SwitchSocket@0 + OnFootstep@0.3246/0.9055；R_Run SwitchSocket@0 + OnFootstep@0.1809/0.5259 ✓

## 9. 自测反馈（apply 阶段填写）

- [x] 9.1 实际采用相位（R_Walk）：[0.2864, 0.7990]（程序化 LF/RF Y 最低点）
- [x] 9.2 实际采用相位（R_Run）：[0.2714, 0.7889]（程序化 LF/RF Y 最低点）
- [x] 9.3 走路脚步音：有声、节奏可接受
- [x] 9.4 跑步脚步音：有声、节奏不完美（音长参差不齐导致；超本 change scope，留 B1c.2 治理）
- [x] 9.5 站立 / 走步 / 奔跑落地音：各响一次 ✓
- [x] 9.6 其他听感异常或 Console 错误：无 Console 错误；跑步听感问题已诊断归因（SA 10 段 wav 长度 0.264~0.346s + TPC Random 抽段 + R_Run 0.333s 间距重叠最长音）

## 10. 收尾

- [x] 10.1 SampleScene 无修改，无需保存
- [x] 10.2 用户进入 Play Mode 自测完毕，反馈填入 Group 9
- [x] 10.3 用户确认归档（跑步听感问题转交 B1c.2）→ 进入归档流程（同步 specs / Docs/DEVELOPMENT_PLAN.md / Docs/TODO.md / Docs/AboutTheAnimation.md）

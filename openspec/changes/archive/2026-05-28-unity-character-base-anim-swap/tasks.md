## 1. 前置确认（无操作风险，先看清现状）

- [x] 1.1 通过 `mcpforunity://editor/state` 资源确认 Unity Editor 处于 Edit Mode（非 Play、非编译中）；若在 Play Mode，先 stop
- [x] 1.2 确认 `Assets/_Project/Scenes/SampleScene.unity` 是当前 active scene（`mcpforunity://editor/state` 返回的 `editor.active_scene.name == "SampleScene"`）
- [x] 1.3 用 `find_gameobjects search_term=PlayerArmature` 确认 PlayerArmature 仍存在（B1a 产出，无意外删除）
- [x] 1.4 通过 `mcpforunity://scene/gameobject/<id>/component/Animator` 资源记录 PlayerArmature.Animator 当前 `runtimeAnimatorController` 与 `avatar` 字段值（用于回滚参考）
- [x] 1.5 用 `manage_asset action=get_info path=Assets/ThirdParty/Locomotion/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller` 确认源 controller 资产存在（GUID `40db3173a05ae3242b1c182a09b0a183`）

## 2. 目录与资产创建

- [x] 2.1 通过 `manage_asset action=create_folder path=Assets/_Project/Animations` 确保 `Assets/_Project/Animations/` 目录存在（若已存在则跳过）
- [x] 2.2 通过 `manage_asset action=create_folder path=Assets/_Project/Animations/Player` 创建 `Assets/_Project/Animations/Player/` 子目录
- [x] 2.3 用 `execute_code` 调 `UnityEditor.AssetDatabase.CopyAsset(...)` 复制 controller，返回 `true`
- [x] 2.4 通过 `execute_code` 确认 `UnomataPlayer.controller` 资产存在，新 GUID `7e64a29a5536876499d5f7da160e4da1`，与源 GUID 不同（独立资产）

## 3. Base Layer Motion 替换（`execute_code` 编辑 AnimatorController 资产）

- [x] 3.1 用 `execute_code` 加载 `UnomataPlayer.controller`，扫描拓扑并打印；apply 期实测：4 个顶层 state（Idle Walk Run Blend / JumpStart / InAir / JumpLand），无 subStateMachine，无 Fly state
- [x] 3.2 验证 `Idle Walk Run Blend` BT children=3，threshold=0/2/6 ✅ 与 D3 假设一致
- [x] 3.3 编辑 BlendTree 子 motion：
    - children[0] ← `R_Idle.fbx` 内主 clip `Idle`
    - children[1] ← `R_Walk.fbx` 内主 clip `Walk`
    - children[2] ← `R_Run.fbx` 内主 clip `Run`
- [x] 3.4 编辑 AnimationClip 类型 State Motion 字段：
    - `JumpStart`.motion ← `R_Jump_2h` (FRA/Jumps/Jump)
    - `InAir`.motion ← `R_Jump_AirL` (FRA/Jumps/Jump)
- [x] 3.4b 编辑 `JumpLand` BlendTree 三个子 motion（apply 期实测发现 JumpLand 实为 BT 而非单 clip，已修订 design.md D3v2 / specs）：
    - children[0] (threshold=0) ← `R_Land_2h` (FRA/Jumps/Land)
    - children[1] (threshold=2) ← `R_Land_ToRun1` (FRA/Jumps/LandToRun)
    - children[2] (threshold=6) ← `R_Land_ToRun3` (FRA/Jumps/LandToRun)
- [x] 3.5 验证 Base Layer 仅 4 个 state（无 Fly），与 SA 原 controller 拓扑一致
- [x] 3.6 `EditorUtility.SetDirty(ctrl/btIdle/btLand)` + `AssetDatabase.SaveAssets/Refresh` 持久化

## 4. 验证 Controller 内容（资产层面）

- [x] 4.1 重新加载 `UnomataPlayer.controller`，逐项核对 8 个 motion 路径与 D3 表格全匹配
- [x] 4.2 对比 SRC vs DST：layers=1 / states=4 / subSMs=0 / params=5 / transitions=5，**完全一致** ✅
- [x] 4.3 全部新 motion 的 clip 名 preview?=False ✅

## 5. 切换 PlayerArmature.Animator.Controller 字段

- [x] 5.1 `manage_components set_property` 写 runtimeAnimatorController 字段 → `Assets/_Project/Animations/Player/UnomataPlayer.controller`
- [x] 5.2 资源 `mcpforunity://scene/gameobject/-1880/component/Animator` 验证字段已切换 ✅
- [x] 5.3 Avatar 字段仍为 `Humanoid_F.fbx` (B1a 契约不退化) ✅

## 6. Play Mode 验证

- [x] 6.1 `manage_editor action=play` 进入 Play Mode
- [x] 6.2 Console 无红色错误；唯一黄色警告为 `'PlayerArmature' AnimationEvent 'SwitchSocket' on animation 'Idle' has no receiver!` —— 非 Avatar 警告，不阻塞动画播放，记录到本 tasks "已知遗留" 段
- [x] 6.3 `Animator.GetCurrentAnimatorStateInfo(0)` 验证：当前 state 长度 3.000s loop=true，clip=`Idle` weight=1.00；params Speed=0 Grounded=true → BlendTree 选 [0] 正确
- [x] 6.4 用户自测（方案 Y + D10 联合最终方案）：跳跃链路无明显出错感觉，整体可接受；细节手感仍有些"别扭"但不阻塞归档，记录到 `Docs/AboutTheAnimation.md` 待未来彻底解决
- [x] 6.5 `manage_editor action=stop` 退出 Play Mode

## 7. 第三方文件未改动验证

- [x] 7.1 `git status -- Assets/ThirdParty/` 输出空 ✅（StarterAssets / CombatGirls / FemaleRunnerAnimset 均未修改）
- [x] 7.2 `git status -- Assets/_Project/Animations/` 显示新增目录（含 `UnomataPlayer.controller` + .meta）✅
- [x] 7.3 `git status -- Assets/_Project/Scenes/SampleScene.unity` 显示 modified ✅（Animator.controller 字段切换的序列化差异）

## 8. 收尾

- [x] 8.1 SampleScene 已自动保存（apply 期 `EditorSceneManager.SaveScene` 落盘，git 已检测到 modified）
- [x] 8.2 用户进入 Play Mode 自测手感，反馈记录到 9.x 段 + `Docs/AboutTheAnimation.md`
- [x] 8.3 用户反馈"无明显出错感觉，细节别扭但不阻塞" → 归档

## 9. 自测反馈（apply 阶段填写）

> 用户 Play Mode 自测后填入此段。整体结论：方案 Y + D10 联合（JumpStart.motion=R_Jump_AirR + speed=3.0）下跳跃链路无明显出错，细节手感仍有"别扭"感（详见 `Docs/AboutTheAnimation.md`），但不阻塞本 change 归档。

- [x] 9.1 ~~R_Jump_2h~~ R_Jump_AirR 起跳手感：丢失"蹬地"视觉，按 Space 后角色"无中生有"腾空——可接受（视觉与瞬时物理一致）。R_Jump_2h 启用条件见 `Docs/AboutTheAnimation.md`
- [x] 9.2 R_Walk 1.133s 步频感知：未发现明显违和；脚步无音效（见 Group 10 / 后续 audio change 处理）
- [x] 9.3 R_Run 0.667s 奔跑节奏：未发现明显违和；脚步无音效
- [x] 9.4 InAir 用 AirL：未发现违和（仅左右脚朝前，与 JumpStart AirR 配对天然契合）
- [x] 9.5 R_Land_2h 站立落地：未发现违和
- [x] 9.5b R_Land_ToRun1 走步落地接走：未发现违和
- [x] 9.5c R_Land_ToRun3 奔跑落地接跑：未发现违和
- [x] 9.6 其他视觉异常：整体跳跃链路过渡仍有些"别扭"但无明显错误，详细记录到 `Docs/AboutTheAnimation.md` 待未来解决

## 10. 已知遗留（apply 期发现，不阻塞本 change 归档）

- **AnimationEvent `SwitchSocket` 无 receiver 警告**：~~不阻塞本 change~~ → **apply 期升级为红色 Error，已在 Group 11 修复**
- **落地半空卡顿** & **半空蹬地违和**：apply 期 Play 自测发现 → 经 Group 12 / 13 两轮 transition 调参均未解决，最终由 Group 14 引入 State Speed 解决
- **走/跑/落地音效失声（apply 期发现）**：SA 的脚步声机制依赖 fbx 内嵌的 `OnFootstep` AnimationEvent + ThirdPersonController.cs 的 `OnFootstep(AnimationEvent)` 方法（同 `OnLand`）。RifleGirl 与 FRA 的 fbx 内嵌的是 `SwitchSocket` 事件而非 `OnFootstep`，换素材后所有走/跑/落地音效失效。
  - 现状：MainCamera 已挂 AudioListener；ThirdPersonController.cs 持有 FootstepAudioClips × 10 + LandingAudioClip 引用完整；唯独缺动画端事件触发
  - 不阻塞动画播放与跳跃手感诊断；本 change 不修
  - 处理建议：B1b.x 单开 patch 或 B1b.2 顺手处理。可选方案：(a) 在新 fbx 内嵌 OnFootstep 事件（修改第三方文件，违规）；(b) 写一个 `FootstepDetector` MB 在 Update 内按 root motion 速度阈值或 BlendTree weight 估算脚步触地时刻，调 `ThirdPersonController.OnFootstep`；(c) 改用 IK 末端位置触发

## 11. SwitchSocket 红色错误修复（apply 期补充）

- [x] 11.1 新建脚本 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs`：命名空间 `Unomata.Gameplay`，提供 `SwitchSocket(string slot)` 空 stub
- [x] 11.2 通过 `manage_components action=add` 把 `PlayerAnimEventReceiver` 挂到 PlayerArmature 上（componentInstanceID=-10046）
- [x] 11.3 资源 `mcpforunity://scene/gameobject/-1880` 验证 PlayerArmature componentTypes 末尾含 `PlayerAnimEventReceiver` ✅
- [x] 11.4 Play Mode 验证：`anim.Update()` 强推 4 秒触发 Idle 多次循环 → `read_console types=[error] filter=SwitchSocket` 返回 0 条 → 全 Console 0 错误 0 警告 ✅
- [x] 11.5 `EditorSceneManager.SaveScene` 持久化场景

## 12. InAir → JumpLand transition offset 修正（apply 期补充，已撤销）

> 探索期判定"落地半空卡顿"误诊，实际卡顿点不在落地。Group 14 引入 State Speed 后已根本解决问题，本组改动全部撤回。

- [x] 12.1 v1：定位 InAir → JumpLand transition，`offset` 由 0.0803 改为 0.0
- [x] 12.2 v1 持久化
- [x] 12.3 v1 终态校对
- [x] 12.4 Play Mode 自测：用户反馈卡顿仍存在，截图定位真实卡顿点在 JumpStart→InAir 中段
- [x] 12.5 **撤回**：Group 14 (D10) 解决根本问题后，`offset` 还原为 SA 原值 0.0803 ✅

## 13. JumpStart → InAir transition 三字段修正（apply 期二次反馈，已撤销）

> 两轮调 transition 字段（v1 / v2）均未解决跳跃手感，根因是 R_Jump_2h 1.2s 与 SA 0.4s 节奏不对齐——transition 字段调参解不了素材-物理时长不匹配的根本矛盾。Group 14 引入 State Speed 后撤回。

- [x] 13.1 v1：定位 transition，三字段调整为 `exitTime=0.85` / `duration=0.10` / `offset=0.0`
- [x] 13.2 v1 持久化
- [x] 13.3 v1 全 5 transition 终态校对 ✅
- [x] 13.4 v1 Play Mode 用户自测：反馈仍卡顿，exitT=0.85 让 JumpStart 必须播 1.02s 才能离开，比物理滞空时间还长
- [x] 13.5 v2：把 `exitTime` 由 0.85 改为 0.20（让 JumpStart 只播 24% 起手就让位给 InAir）
- [x] 13.6 v2 持久化 + 终态确认
- [x] 13.7 v2 Play Mode 用户自测：反馈"角色被抬高后才在半空莫名其妙蹬地"——R_Jump_2h 24% 处的关键帧是"准备/屈膝"，蹬地帧（约 30~40%）被截掉
- [x] 13.8 **撤回**：Group 14 (D10) 解决根本问题后，三字段还原为 SA 原值（exitT=0.6637 / dur=0.4705 / offset=0.6088）✅

## 14. JumpStart State Speed = 3.0（apply 期 D10 v1，已撤销）

> 探索期"不调 State Speed"决策曾尝试反悔，引入 speed=3.0 + transition 字段调整。两轮 Play 自测均失败：
> - D10 v1（speed=3.0 + transition SA 原值）：用户反馈"接近顶点蹬腿"，因 SA 原 duration 比压缩后 JumpStart 还长，过渡尾段卡末帧
> - D10b（speed=3.0 + transition 0.85/0.10/0.0）：用户反馈"仍在顶点蹬腿"
>
> 深度分析揭示：R_Jump_2h 的"蹬地帧"在 clip 25% 处（动画师设计意图：先准备再蹬地），ThirdPersonController 物理是"瞬时起跳"，两者本质冲突——任何 speed 都无法把"动画蹬地帧"对齐到"物理起跳瞬间"。撤回所有 D10 改动，由 Group 15 方案 Y 换 motion 素材根本解决。

- [x] 14.1 撤回 Group 12 / 13：5 条 transition 全部还原 SA 原值
- [x] 14.2 设置 `JumpStart.speed = 3.0`，其他 3 个 State 保持 1.0
- [x] 14.3 持久化
- [x] 14.4 终态校对
- [x] 14.5 Play Mode 用户自测：反馈"顶点蹬腿"→ 触发 Group 14b
- [x] 14.6 **Group 15 方案 Y 落地后，撤回 D10**：JumpStart.speed 还原为 1.0 ✅

## 14b. JumpStart → InAir transition 三字段配套调整（apply 期 D10b，已撤销）

- [x] 14b.1 transition 三字段调整为 0.85 / 0.10 / 0.0
- [x] 14b.2 持久化
- [x] 14b.3 终态校对
- [x] 14b.4 Play Mode 用户自测：反馈"接近顶点处仍蹬腿"→ 触发深度根因分析 → 发现素材-物理本质冲突
- [x] 14b.5 **Group 15 方案 Y 落地后，撤回 D10b**：JumpStart→InAir 三字段还原为 SA 原值 ✅

## 15. JumpStart Motion 改为 R_Jump_AirR + Speed = 3.0（apply 期 D10 方案 Y + speed 联合，最终方案）

> 根本性解决跳跃链路问题：
> - 换 motion 解决"蹬地帧错位"（R_Jump_2h 25% 处蹬地帧与瞬时物理不对齐）
> - speed=3.0 解决"节奏不对齐"（R_Jump_AirR 1.167s 与 SA 0.4s 节奏不对齐导致落地卡顿）
> - 两者联合达成视觉与物理一致

- [x] 15.1 加载 controller，加载 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/Jumps/Jump/R_Jump_AirR.fbx` 主 clip `R_Jump_AirR`（1.167s）
- [x] 15.2 替换 `JumpStart.motion = R_Jump_AirR`（之前 R_Jump_2h）
- [x] 15.3 撤回 D10 v1：`JumpStart.speed = 1.0`
- [x] 15.4 撤回 D10b：`JumpStart→InAir` transition 三字段还原 SA 原值
- [x] 15.5 持久化
- [x] 15.6 终态校对：5 条 transition 全部 [SAME AS SA]，4 个 State Speed 全部 = 1.0，JumpStart.motion = R_Jump_AirR
- [x] 15.7 Play Mode 用户自测 (方案 Y v1)：反馈"落地卡顿"，截图显示落地瞬间 JumpStart 进度 ~70% 仍 active。诊断：R_Jump_AirR 1.167s × exitT 0.6637 = 0.775s 才触发 transition，比物理滞空 0.8s 还接近，整段空中 JumpStart 几乎不让位 → 触发 step 15.8
- [x] 15.8 **重启 D10 (speed=3.0)，与方案 Y 联合**：`JumpStart.speed = 1.0 → 3.0`（R_Jump_AirR 实际播放 1.167/3.0 = 0.389s 与 SA Jump.fbx 0.4s 等长，节奏与 SA 调参对齐）
- [x] 15.9 持久化 + 终态校对：5 条 transition [SAME AS SA]，JumpStart.speed=3.0、其他 3 State Speed=1.0，JumpStart.motion=R_Jump_AirR
- [x] 15.10 Play Mode 用户自测：方案 Y + speed 联合验收通过——整体无明显出错；"别扭"感记录到 `Docs/AboutTheAnimation.md`，归档到 Group 16 备查

## 16. 备查（方案 Y 落地后的未来改进方向）

> 方案 Y 是当前素材+物理组合下的合理折中，丢失"蹬地起跳"视觉表现。以下为未来若有好点子或新需求时可回头评估的改进方向：

- **R_Jump_2h 启用条件**：当前 R_Jump_2h.fbx 在 ThirdParty 资产库保留但 controller 未引用。未来引入"蓄力起跳"物理（按住 Space 蓄力一段时间再触发跳跃），R_Jump_2h 这种"准备+蹬地"的设计意图与物理对齐，可在 patch change 中重新启用
- **Animation Rigging 程序化蹬地**：用 Animation Rigging 在按 Space 后 0.05s 内强制叠加"弯腿+伸腿"动作，不依赖 fbx 关键帧——可绕开"素材时序与物理瞬时性矛盾"
- **寻找瞬时起跳专用素材**：类似 SA 自家 Jump.fbx 0.4s 那种短而利落的设计，从 Asset Store 或动捕库另寻
- **JumpStart State 完全去除**：从状态机层面移除 JumpStart，让 Idle Walk Run Blend → InAir 直接由 `Jump=true` cond 触发——但属"修改状态机拓扑"，破坏 Non-Goal 第一条，需更大范围 change

## Context

B1a (`unity-character-model-swap`, 归档 2026-05-26) 完成了 PlayerArmature 视觉模型嵌入 + Humanoid Avatar 切换，但留下"动画归属"偏差：Animator 组件的 `runtimeAnimatorController` 字段仍指向 `Assets/ThirdParty/Locomotion/StarterAssets/.../StarterAssetsThirdPerson.controller`，Base Layer 6 个 Motion 槽（Idle / Walk / Run / Jump / InAir / Land）仍是 SA 通用 Mecanim 动画，与 RifleGirl 风格不一致。

Phase 0 `phase0-femalerunner-animset-validate` (归档 2026-05-27) 已把 FemaleRunnerAnimset (FRA) 整理到 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/`，提供 RifleGirl 缺失的 Jump / InAir / Land 链路素材。

探索期 (2026-05-28) 已完成所有风险查证（详见 `Docs/TODO.md` B1b.1 段"风险查证总结"小节）。

**当前现状（探索期实测）：**

```
PlayerArmature (scene instance, no prefab override):
  Animator.runtimeAnimatorController = StarterAssetsThirdPerson.controller   ← 待切
  Animator.avatar                    = Humanoid_FAvatar  (RifleGirl)         ✅ B1a 已切
  Animator.parameters                = Speed/Jump/Grounded/FreeFall/MotionSpeed (5)
  ThirdPersonController.cs 内 Animator.StringToHash 5 参数硬编码字符串 ✅ 完全匹配
```

## Goals / Non-Goals

**Goals:**

- 让 Base Layer 全 6 槽 Motion 引用切到 RifleGirl/FemaleRunnerAnimset 风格素材
- Animator Controller 资产**独立 GUID**，与 SA 解耦（后续 B1b.2 在此基础上扩 UpperBodyAim 层）
- 不修改任何第三方文件（StarterAssets / CombatGirls / FemaleRunnerAnimset 全部只读）
- B1a 的全部 Play 行为契约（Avatar / Cloth / Geometry 禁用 / 移动响应）保持

**Non-Goals:**

- ❌ 不新增 / 不修改状态机拓扑（State / SubStateMachine 数量、Transition 连接关系、参数名）
- ⚠️ ~~不调整 Animator State 的 `Speed` 字段~~ → **explore 期决策在 apply 期部分反悔**（仅限 JumpStart）：实测发现 SA 状态机调参为 SA 自家 0.4s Jump.fbx 节奏专门调，FRA 的 1.167s R_Jump_AirR 必须调 `JumpStart.speed=3.0` 才能与 SA 节奏对齐；其余 3 个 State (Idle Walk Run Blend / InAir / JumpLand) 仍保持默认 1.0
- ❌ 不新增 Animator Layer（UpperBodyAim 是 B1b.2 范围）
- ❌ 不接入瞄准、射击、双相机（B1b.2 / B1b.3 范围）
- ❌ 不改 ThirdPersonController.cs（参数完全兼容，无需改）
- ❌ 不引入 Run 急停、双脚 BT、unarmed/armed 切换、跳跃高度分支等增强（已在 TODO 备查段记录）
- ❌ 不批量改 FRA fbx 的 `lastHumanDescriptionAvatarSource`（运行期不依赖该字段，CombatGirls 同悬空已旁证）

**Non-Goal 边界澄清（apply 期补充）**：

"不修改状态机拓扑"严格指**结构层面**——State / Transition 数量与连接关系、参数定义。**transition 内的素材相关调参字段**（`offset` / `exitTime` / `duration`）属于"素材-transition 配套调优"范畴：SA 原值是为 SA 自家 motion 调的，换素材后若旧值导致明显视觉/手感问题，**可调整以保证新素材正确播放**，不视为越界。本 change 因 InAir→JumpLand 的 offset=0.08 在 FRA ToRun 系列素材上引发"半空卡顿"，按此原则做最小修正（见 D7）。

**仍然不能动的 transition 字段**：触发条件 `conditions`（含参数名 / 比较模式 / 阈值）、目标 state、`hasExitTime` 的 true/false 切换（这才是结构层面）。

## Decisions

### D1 — Controller 复制策略：`AssetDatabase.CopyAsset` 而非 `AnimatorOverrideController`

**选择**：`AssetDatabase.CopyAsset(src, dst)` 把 SA controller 整体复制为项目自有 `UnomataPlayer.controller`，再在副本上替换 Motion。

**为何不用 AnimatorOverrideController**：
- AOC 仍依赖原始 SA controller 作为 base（运行时引用 SA 文件）
- 无法独立扩展（B1b.2 要在 controller 上加 Layer，AOC 不支持加 Layer）
- 第三方依赖未真正脱钩

**为何不手写新 controller**：
- 状态机拓扑很复杂（含 SubStateMachine、Idle Walk Run BlendTree 内嵌、多个 transition），手写易出错
- "拷贝 + 改 Motion 引用"风险面最小

**操作方式**：通过 `execute_code` 调 `UnityEditor.AssetDatabase.CopyAsset` API（不走文件系统 cp）。

### D2 — Motion 替换的实现路径：编辑 `.controller` 资产 SerializedObject

**选择**：用 `execute_code` 加载 `UnomataPlayer.controller` 为 `AnimatorController`，遍历 `layers[0].stateMachine`（Base Layer），按节点名定位到 `Idle Walk Run Blend` / `JumpStart` / `InAir` / `JumpLand`，写 `state.motion`（或 BlendTree 子 motion）字段。

**Idle Walk Run Blend 是 BlendTree**，需打开 `state.motion as BlendTree`，按 children 索引 0/2/6 写子 motion（threshold 已对齐 Speed 0/2/6，索引固定）。

**为何不直接编辑 .controller YAML**：
- YAML 内有 fileID 引用、序列化版本号，手工编辑易破环引用结构
- AnimatorController API 是官方支持的修改路径，安全

**为何不用 Animator Window 手动拖拽**：
- 不可重复（rebuild / 团队协作场景下无法保证一致）
- 与"用 UnityMCP 自动化"原则冲突

### D3 — Motion 选型最终锁定（探索期摸底 + apply 期实测拓扑修正）

**apply 期实测拓扑修正（2026-05-28）**：

探索期通过读 controller YAML 推断的拓扑与实际 `AnimatorController` API 加载结果不一致：

| 项目 | 探索期假设 (D3 v1) | apply 实测 (D3 v2，本节) |
|------|--------------------|--------------------------|
| Base Layer State 数 | 5 (含 Fly) | 4 |
| Fly 是否独立 State | 是（保留原 motion） | **不存在该 State**（控件 YAML 中 "Fly" 是相邻 fbx 名而非 State 名） |
| JumpLand 类型 | 单 AnimationClip | **BlendTree**（3 children，按 Speed 0/2/6 分站立/行走/奔跑落地） |

修正后的最终素材表：

| 槽位 | Motion | clip 名 | 时长 | loop |
|------|--------|---------|------|------|
| Idle Walk Run BlendTree[0] (Speed=0) | RifleGirl/Normal/R_Idle.fbx | `Idle` | 3.000s | true |
| Idle Walk Run BlendTree[1] (Speed=2) | RifleGirl/Normal/R_Walk.fbx | `Walk` | 1.133s | true |
| Idle Walk Run BlendTree[2] (Speed=6) | RifleGirl/Normal/R_Run.fbx | `Run` | 0.667s | true |
| JumpStart (AnimationClip) | **FRA/Jumps/Jump/R_Jump_AirR.fbx**（apply 期 D10 方案 Y 替换，详见 D10）| `R_Jump_AirR` | 1.167s | false |
| InAir (AnimationClip) | FRA/Jumps/Jump/R_Jump_AirL.fbx | `R_Jump_AirL` | 1.167s | false |
| JumpLand BlendTree[0] (Speed=0) | FRA/Jumps/Land/R_Land_2h.fbx | `R_Land_2h` | 1.167s | false |
| JumpLand BlendTree[1] (Speed=2) | FRA/Jumps/LandToRun/R_Land_ToRun1.fbx | `R_Land_ToRun1` | 0.600s | false |
| JumpLand BlendTree[2] (Speed=6) | FRA/Jumps/LandToRun/R_Land_ToRun3.fbx | `R_Land_ToRun3` | 0.867s | false |

**JumpLand BT 3 槽分速度选 motion 的理由**：

- **跳跃衔接走/跑直接影响手感**——比 Run 急停更显眼。SA 原拓扑就把 JumpLand 做成 BT 分速度区分（用了 JumpLand / Walk_N_Land / Run_N_Land 三个 SA 自带 clip），本 change 维持这一设计意图，全 3 槽切到 RifleGirl/FRA 风格。
- 站立落地（Speed=0）用 R_Land_2h 与 JumpStart 系列同源（1h/2h/3h 高度），保持视觉一致；不用 ToRun 系列（站立落地无前冲过渡需求）。
- 走步落地（Speed=2）用 R_Land_ToRun1 0.600s——FRA ToRun 系列中最短，匹配 R_Walk 1.133s 步频感知。
- 奔跑落地（Speed=6）用 R_Land_ToRun3 0.867s——比 ToRun1 长但带前冲感，匹配 R_Run 0.667s 高频步伐。
- JumpLand BT `HasExitTime=0`（SA 原设置不变），动画时长不会卡住状态过渡——Grounded=true 立即由 Animator Blend 接管过渡到 Idle Walk Run Blend，ToRun 系列时长 0.6~0.87s 的差异属"过渡期视觉"层面，不影响响应延迟。
- 备查素材未选用：`R_Land_ToRun2.fbx` 0.9s（与 ToRun3 同档定位较模糊，本期取舍只用 ToRun1/3 的两端）。

**InAir 选 AirL 而非 AirR**：随机锁定，Play 自测违和再换（不算改拓扑）。  
**Jump 选 2h 而非 1h/3h/4h**：FRA 全系列 Jump 时长 1.1～1.23s 差距小，2h 是中间档；高度分支扩展属备查项。

### D4 — `__preview__` 副本规避

每个 fbx 在 `LoadAllAssetsAtPath` 返回中含 `__preview__<name>` 同时长副本（Unity import 副产物）。引 motion 时按**主 clip 名**精确匹配（`R_Idle` / `Idle` / `R_Jump_2h` 等），跳过任何以 `__preview__` 开头的 clip。

### D5 — PlayerArmature.Animator.Controller 字段切换

**选择**：通过 `manage_components action=set_property` 把 PlayerArmature.Animator.runtimeAnimatorController 字段写为 `UnomataPlayer.controller` 的资产引用。

**前置确认（探索期完成）**：PlayerArmature 是场景纯实例（`parent: null` + scene root），无 prefab override 复杂度，直接写场景。

**保存场景**：归档前提示用户保存 SampleScene.unity。

### D6 — Avatar Source 悬空字段不处理

FRA 全部 fbx 的 `lastHumanDescriptionAvatarSource` 指向已删除的 Humanoid_FeRifle (guid `c0426d61788399a4aa9702989176ee68`)。Phase 0 已记录"未触发 Console 警告"，本 change 维持现状不批量改。

**理由**：该字段仅 import-time 复制源记录，运行期由 fbx 自带 `humanDescription` + `animationType: 3` 完成 Humanoid Retargeting；CombatGirls 自带动画也指向同一悬空 guid（B1a 已 Play 验证可跑），是可接受的现状。

### D7 — SwitchSocket AnimationEvent 红错处理（apply 期发现）

**问题现场**：apply 期 Play Mode 后 Console 持续输出**红色 Error**（非 warning）：

```
'PlayerArmature' AnimationEvent 'SwitchSocket' on animation 'Idle' has no receiver! Are you missing a component?
（Walk / Run 同样错误，每次 clip 循环触发一次）
```

**根因**：CombatGirls RifleGirl 的 R_Idle / R_Walk / R_Run fbx 内嵌 `SwitchSocket(string)` AnimationEvent（设计意图：动画 0s 处切换持枪挂点，参数为 `To_Hand_R_Socket` / `IK_ON_Left_Handle, To_Hand_R_Socket`）。PlayerArmature 上无任何脚本提供 `SwitchSocket(string)` 方法，每次 clip 播放都抛 Error。FRA 跳跃系列 fbx 无此事件（已实测）。

**选择**：在 `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimEventReceiver.cs` 新建空 stub MonoBehaviour，挂载到 PlayerArmature 上：

```csharp
public class PlayerAnimEventReceiver : MonoBehaviour
{
    /// <summary>占位接收器：吞掉 CombatGirls 动画自带的 SwitchSocket 事件。
    /// 当前不实现挂点切换逻辑（Phase 4 / B1b.2 接入持枪 IK 时再补）。</summary>
    public void SwitchSocket(string slot) { /* placeholder */ }
}
```

**为何不用其他方案**：
- 禁用 `Animator.fireEvents = false` → 副作用过大，未来 IK / 受击事件等也都不触发，否决
- import 时清掉 fbx 内嵌 events → 修改第三方文件，违反"不动 ThirdParty"约束，否决
- Avatar Mask 绕过 → AnimationEvent 不挂骨骼，Mask 无法过滤，否决

**边界澄清**：本 change 严格"换 motion"边界确实越了一点（新增脚本 + 场景挂载），但这是"换 motion 的直接副产物"——不切到 RifleGirl 素材就不会出此错误。归在本 change 比单开 patch 合理（Console spam 阻塞后续验证）。stub 同样为 B1b.2 持枪 IK 留好接入点（届时把 placeholder 替换为真实切换逻辑）。

### D8 — InAir → JumpLand transition offset 修正（已撤销）

**历史**：apply 期初轮 Play 自测反馈"落地半空卡顿"，曾把 `InAir → JumpLand` transition 的 `offset` 由 `0.0803` 改为 `0.0`，意图让 Land clip 从起手帧播。

**撤销原因**：D9 v2 阶段进一步排查发现"卡顿"主观感受的真实定位在 `JumpStart → InAir` 切换时段（用户截图证据），而非落地。D10 引入 State Speed 后已根本性解决跳跃链路节奏对齐，本字段恢复 SA 原值 `0.0803` 不影响新方案手感（Speed 改了 motion 实际时长，offset 0.0803 在新时长下视觉表现等效于 SA 原素材的 8% 偏移，在 SA 设计原意范围内）。

**最终值**：与 SA 原 controller 一致（`offset = 0.0803`）。

### D9 — JumpStart → InAir transition 调参（已撤销）

**历史**：apply 期 Play 自测先后两轮调过 `JumpStart → InAir` transition：

- **D9 v1**（exitT=0.85 / dur=0.10 / offset=0.0）：用户反馈"角色保持半空起跳姿态卡顿"——exitT 0.85 让 R_Jump_2h 必须播 1.02s 才能转，比整个 0.8s 物理滞空时间还长
- **D9 v2**（exitT=0.20 / dur=0.10 / offset=0.0）：用户反馈"角色被抬高后才在半空莫名其妙蹬地"——R_Jump_2h 24% 处的关键帧是"准备/屈膝"，蹬地帧（约 30~40%）被截掉，物理已经离地但动画还没演完蹬地

**根本原因诊断**：apply 期实测 SA 自家 Jump.fbx clip 长 0.400s，与 ThirdPersonController.cs 的 0.8s 物理滞空时序自洽。FRA 的 `R_Jump_2h.fbx` 是 1.200s 的"完整跳跃动画"，**3 倍于 SA 节奏**——为不同上下文（跑酷大跳/二段跳）设计，不是为"按键瞬间给 verticalVelocity"的轻量物理跳跃做的。**transition 字段调参解不了素材-物理时长不匹配的根本矛盾**。

**撤销原因**：见 D10——通过 State Speed 把 R_Jump_2h 实际播放时长压到 0.4s 与 SA 节奏对齐，transition 调参全部还原 SA 原值即自洽。

**最终值**：与 SA 原 controller 一致（exitT=0.6637 / dur=0.4705 / offset=0.6088）。

### D10 — JumpStart 视觉职责改为"滞空姿态预览"（D10/D10b 撤销，方案 Y）

**问题背景**：

D10 / D10b 两轮调 `JumpStart.speed` + transition 字段后用户反馈"角色仍在接近顶点处蹬腿"。深度分析揭示**根本矛盾**：

```
R_Jump_2h.fbx 的内部时序假设："先准备 → 蹬地 → 离地 → 顶点 → 下落"
ThirdPersonController 物理模型："按 Space 瞬间给 verticalVelocity"
```

R_Jump_2h 不论 speed 如何调整，"蹬地动作"始终在 clip 25% 关键帧处发生——而物理上"蹬地"是按下 Space 那一帧（瞬间发力，无准备阶段）。无法通过任何 transition / speed / offset 调参把"R_Jump_2h 25% 处的蹬地帧"对齐到"物理 0~0.05s 这个极短窗口"——除非 JumpStart 实际播放时长 < 0.2s（speed > 6.0），但这又导致 R_Jump_2h 后 75% 关键帧（顶点 + 下落）完全废弃。

**素材本身不适合"瞬间起跳物理"**——它是为"蓄力跳"或"跑酷大跳"这类有明确起跳预备阶段的物理设计的。

**修正（方案 Y + D10 联合，最终方案）**：

```
JumpStart.motion = R_Jump_AirR     （之前 R_Jump_2h，改为滞空姿态素材）
JumpStart.speed = 3.0              （让 R_Jump_AirR 1.167s 实际播 0.389s 与 SA Jump.fbx 0.4s 节奏对齐）
JumpStart→InAir transition: 全部 SA 原值
其他 transition: 保持 SA 原值
```

**为何同时需要"换 motion"和"speed=3.0"**：

apply 期 D10 v1（仅 speed=3.0，保留 R_Jump_2h）失败原因：R_Jump_2h 关键帧分布（蹬地帧 25%）与瞬时物理矛盾，3 倍速压缩反而把"蹬地动作"挤到 0.1s 处，但物理上此时角色已经离地，视觉错位。

apply 期方案 Y v1（仅换 motion=R_Jump_AirR，speed=1.0）失败原因：R_Jump_AirR 1.167s 实际播放与 SA Jump.fbx 0.4s 节奏不对齐，SA 状态机调参 `exitT=0.6637` × 1.167s = 0.775s 触发，比物理滞空 0.8s 还接近——整段空中时间 JumpStart 几乎不让位给 InAir，落地后状态机才慢悠悠播 JumpStart→InAir→JumpLand 链路，玩家感知"落地卡顿"。

**两者联合**：
- 换 motion 解决"蹬地帧错位"问题（R_Jump_AirR 整段是滞空姿态，无关键帧错位风险，3 倍速压缩无副作用）
- speed=3.0 解决"节奏不对齐"问题（实际播放 0.389s ≈ SA Jump.fbx 0.4s，状态机调参完全自洽）

**视觉职责重新定位**：JumpStart 不再尝试演"起跳动作"，而是"按 Space 后立即显示滞空姿态预览"——与 InAir 的 R_Jump_AirL 配对（左右脚朝前不同），过渡平滑。

```
按 Space → JumpStart 播 R_Jump_AirR 滞空姿态 (实际 0.39s)
       → SA 原 0.47s 过渡到 InAir R_Jump_AirL
       → Grounded → JumpLand
       → Idle Walk Run Blend
```

**优点**：
- 视觉与物理一致——两者都是"瞬间离地"模型，无虚假的"准备/蹬地"动作
- 状态机调参与 SA 100% 一致（5 条 transition 全 SA 原值，3 个 State Speed 默认 1.0）
- R_Jump_AirR 与 R_Jump_AirL 同源同设计，过渡天然契合（fbx 由同一动画师设计，关键帧规范一致）
- 节奏与 SA Jump.fbx 0.4s 等长，物理 0.8s 滞空时序完美对齐

**代价**：
- 丢失"蹬地起跳"的视觉表现——按 Space 后角色"无中生有"地腾空，缺少"脚踩地发力"的视觉提示
- R_Jump_2h 在本期被弃用（保留在 ThirdParty 中不被引用，可在后续 change 启用）
- JumpStart.speed=3.0 是唯一非 SA 默认值的 State Speed

**未来可改进的方向（备查，不在本 change 范围）**：
- 引入"蓄力跳"物理（按住 Space 蓄力一段时间再触发跳跃），届时 R_Jump_2h 这种"准备+蹬地"素材的设计意图与物理对齐，可恢复使用
- 改用 Animation Rigging 在 0.05s 内强制弯腿动作叠加（程序生成蹬地视觉），不依赖 fbx 关键帧
- 寻找"瞬间起跳"专用素材（短而利落，类似 SA 自家 Jump.fbx 0.4s 的设计）

**5 条 transition 最终状态（全部 SA 原值）**：

| transition | hasExit | exitTime | duration | offset | cond | 与 SA 相比 |
|------------|---------|----------|----------|--------|------|-----------|
| Idle Walk Run Blend → InAir | false | 0.9466 | 0.0375 | 0.2304 | FreeFall If | 一致 |
| Idle Walk Run Blend → JumpStart | false | 0.0357 | 0.0703 | 0.0000 | Jump If | 一致 |
| InAir → JumpLand | false | 0.3015 | 0.0976 | 0.0803 | Grounded If | 一致 |
| JumpLand → Idle Walk Run Blend | true | 0.3995 | 0.4340 | 0.3629 | — | 一致 |
| JumpStart → InAir | true | 0.6637 | 0.4705 | 0.6088 | — | 一致 |

**4 个 State Speed 最终状态**：

| State | Speed | Motion | 与 SA 相比 |
|-------|-------|--------|-----------|
| Idle Walk Run Blend | 1.0 | BlendTree (R_Idle / R_Walk / R_Run) | speed 一致, motion 替换 (D3) |
| JumpStart | **3.0** | **R_Jump_AirR** | **speed 改 + motion 替换为滞空素材**（D10 方案 Y + speed 联合） |
| InAir | 1.0 | R_Jump_AirL | speed 一致, motion 替换 (D3) |
| JumpLand | 1.0 | BlendTree (R_Land_2h / R_Land_ToRun1 / R_Land_ToRun3) | speed 一致, motion 替换 (D3) |

**为何 explore 期"不调 State Speed"决策最终需要部分反悔**：D10 v1 (R_Jump_2h + speed=3.0) 的反复调试证明 speed 调整在 R_Jump_2h 素材上不能解决根本矛盾——必须先换 motion 素材到 R_Jump_AirR（无关键帧错位风险），再配合 speed=3.0 让节奏与 SA 对齐。explore 期"不调 State Speed"的整体方向正确，apply 期实测发现需要"motion 替换 + speed 调整"双管齐下，单调一项均不足以解决问题。

## Risks / Trade-offs

- **R1：R_Jump_2h 1.2s 起跳手感偏拖** → ~~JumpStart `HasExitTime=1` 必须播完才转 InAir，FRA 全系列没有更短素材；本期不缓解~~ → 已由 D10 (JumpStart.speed=3.0) 解决
- **R2：FRA fbx Avatar 悬空字段未来可能引发警告** → 当前 Console 零警告（Phase 0 已确认），若未来 Unity 版本升级触发新警告，再开 patch change 批量改 Avatar Source 指向 Humanoid_F
- **R3：BlendTree 子 motion 索引依赖于 SA 原 controller 的 threshold 排列** → SA controller 由第三方维护可能随 update 改变 threshold，目前是 Speed=0/2/6 三档；apply 阶段已打印实际 children threshold 验证 ✅
- **R4：MotionSpeed 参数对动画播放速率的影响** → ThirdPersonController.cs 把 input magnitude 写到 MotionSpeed，BlendTree 用 MotionSpeed 调速（第二乘子）；新动画素材时长不同（R_Walk 1.133s vs SA Walk_N 估 1.0s），可能造成步频感知差异；不在本 change 范围调整，留 Play 自测反馈
- **R5：SA transition 调参与新素材不适配（apply 期实测，已解）** → 探索期判断"SA transition 调参为通用"——apply 期实测发现 SA 调参为 SA 自家 0.4s/2.6s 素材专门调，前提崩塌。先后两轮调 transition 字段（D8 / D9 v1 / D9 v2）均未根本解决跳跃手感，最终通过 D10 引入 State Speed 把素材实际时长对齐 SA 节奏，所有 transition 字段还原 SA 原值即自洽
- **R6：PlayerAnimEventReceiver placeholder 副作用** → stub 当前空实现，未来 B1b.2 接入持枪 IK 时需要按 R_Walk / R_Run 的 stringParameter（`IK_ON_Left_Handle, To_Hand_R_Socket`）做实际挂点切换；当前 placeholder 不会引入逻辑错误，只是"占位待实现"
- **R7：JumpStart.speed=3.0 + R_Jump_AirR motion 联合方案的视觉代价** → R_Jump_AirR 是滞空姿态，3 倍速压缩到 0.39s 后整段都是滞空姿态切换——无关键帧错位风险（不同于 R_Jump_2h 的"蹬地帧 25%"问题）。代价是按 Space 后角色"无中生有腾空"，缺少"脚踩地发力"视觉提示。当前可接受，理由：(a) 视觉与瞬时物理一致；(b) 节奏与 SA 设计意图对齐；(c) R_Jump_2h 在素材库保留，未来若引入"蓄力起跳"物理可恢复使用——见 design 末段"未来可改进的方向"
- **R8：走/跑/落地音效失声（apply 期发现）** → SA 的脚步声机制依赖 fbx 内嵌的 `OnFootstep` AnimationEvent + ThirdPersonController.cs 的 OnFootstep 方法；RifleGirl 与 FRA 的 fbx 内嵌的是 `SwitchSocket` 事件而非 `OnFootstep`，换素材后所有走/跑/落地音效失效。**不阻塞动画播放与跳跃手感诊断**，记录到 tasks Group 10 已知遗留段，后续 patch change（B1b.x 或 B1b.2 顺手）处理
- **R9：方案 Y 丢失"蹬地起跳"视觉表现** → JumpStart 用 R_Jump_AirR 滞空姿态素材后，按 Space 起跳无"脚踩地发力"的视觉提示，玩家可能感知"无中生有腾空"。当前可接受，理由：(a) 视觉与瞬时物理一致，无虚假"蹬地"动作；(b) R_Jump_2h 在素材库保留，未来若引入"蓄力起跳"物理（按住 Space 蓄力）可恢复使用——见 design 末段"未来可改进的方向"
- **R10：R_Jump_2h 当前未被任何 State 引用** → 资产仍存在 `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/.../R_Jump_2h.fbx` 中，B1b.1 controller 不引用。若未来加入蓄力跳或别的物理模型，可在 patch change 中重新启用

## Migration Plan

1. apply 阶段创建 `Assets/_Project/Animations/Player/` 目录（若不存在）
2. `AssetDatabase.CopyAsset` 复制 SA controller 到项目自有路径
3. 编辑副本：替换 6 个 Motion 引用（按 D2/D3 方案）
4. 切换 PlayerArmature.Animator.runtimeAnimatorController 字段（按 D5）
5. Play Mode 验证：站立 / WASD / Shift 奔跑 / 空格跳跃 / 落地，全 6 状态播 RifleGirl 风格动画
6. 提示用户保存 SampleScene 并 git commit
7. 归档阶段同步 `character-controller` spec、`Docs/DEVELOPMENT_PLAN.md` Phase 2.1 C2 任务勾选、`Docs/TODO.md` B1b.1 段落归档标注

**回滚**：删除 `Assets/_Project/Animations/Player/UnomataPlayer.controller` + .meta，把 PlayerArmature.Animator.runtimeAnimatorController 改回 `StarterAssetsThirdPerson.controller`，保存场景。

# DEVELOPMENT_PLAN.md — 开发阶段规划

> 本文档为两人并行开发的进度对齐参考。
> 阶段划分以"可独立运行验证"为交付标准，不绑定具体日期。

---

## 人员分工

| 角色 | 负责范围 |
|------|---------|
| **你（A）** | `Core` 层纯C#开发：接龙规则、牌组、计时、Combo预留、得分结算 |
| **队友（B）** | Unity端：TPS主线、骇入触发、副线UI、双线联动对接 |

---

## 阶段总览

```
Phase 0  环境准备（并行，今天完成）
Phase 1  Core层开发（A独立推进）
Phase 2  Unity TPS基础（B独立推进）
Phase 3  副线UI（B，依赖Phase1接口冻结）
Phase 4  双线联动对接（A+B，依赖Phase1+Phase2）
Phase 5  难度曲线与数值调整（A+B）
Phase 6  打磨与验证（A+B）
```

---

## Phase 0 — 环境准备

**并行完成，今天结束**

### A（你）
- [x] 项目目录结构建立
- [x] Git 仓库 + 远程同步
- [x] Agent Rules
- [x] GAME_DESIGN.md
- [x] INTERFACE.md
- [x] 搭建 `CardChainCore` 控制台项目（.NET 8 独立工程，Phase 4 时复制源码迁入 Unity）
  - [x] 在 `CardChainCore/` 下建 `CardChainCore.sln`
  - [x] 建 `src/Unomata.Core/Unomata.Core.csproj`（`net8.0`，`<Nullable>enable</Nullable>`，零第三方依赖）
  - [x] 建 `tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`（xUnit + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk，引用 `Unomata.Core`）
  - [x] 建 `console/Unomata.Core.Console/Unomata.Core.Console.csproj`（`net8.0`，引用 `Unomata.Core`，`Program.cs` 占位 `Hello`）
  - [x] `dotnet build` 三个项目均成功
  - [x] `dotnet test` 跑通空测试套件（0 passed / 0 failed）
  - [x] `dotnet run --project console/Unomata.Core.Console` 输出占位文本

### B（队友）
- [x] Unity Hub 新建 2022.3 LTS + URP 项目，放入现有仓库 `Assets/` 目录
- [x] Package Manager 安装 QFramework（已验证在 Unity 2022.3 LTS 下完全可用）
- [x] 导入 Starter Assets Third Person Controller（已导入至 Assets/ThirdParty/StarterAssets/）
- [x] **验证 CombatGirls 动画能否 Retarget 到 Starter Assets 骨骼**：结论 **方案B**，两者均 Humanoid Rig，Mecanim 自动重定向；需在 Phase 2 添加上半身动画层
- [x] 提交初始 Unity 工程
- [x] 导入剩余第三方资产：Behavior Designer / Mech Pack / Sci fi 2in1 / FORGE3D Sci-Fi Effects / Sci-Fi Weapons-Bullet Hell SFX
- [x] 第三方资产二层目录整理（含已有三包 CombatGirls / StarterAssets / MagicaCloth2 同步迁移；`Assets/Gizmos/` 例外不动），详见 change `phase0-third-party-assets-validate`
- [x] 第三方资产 URP 材质兼容性检查（Mech Pack / SciFiArena / SciFiEffects 三个高风险包）
- [x] 第三方资产 B 档最小可用性验证（5 个 Sandbox 场景，每包跑通最小 demo，不接业务）
- [x] 敲定敌人 BT 框架选型 = Opsive Behavior Designer（已导入并验证），同步 `DEPENDENCIES.md` 与 Phase 2.4 C2 任务行
- [x] FemaleRunnerAnimset（RifleGirl 跳跃动画补充包）二层目录迁移 + 副作用清理（覆盖 CombatGirls 材质/脚本回滚、manifest.json 新增包评估），详见 change `phase0-femalerunner-animset-validate`

---

## Phase 1 — Core 层开发（A独立）

**交付标准：控制台项目可完整运行一次骇入流程，所有事件正确触发**

### 任务清单
- [x] `CardData` + `CardType` / `CardColor` / `ChainDirection` 枚举：纯数据，含 `CardData.Empty` 静态实例 *(A1 cardchain-types, 2026-05-26)*
- [x] `HackDifficultyConfig`：难度参数数据类（OptionCount / TargetChainCount / TotalTime / SolvableRate / WildAppearRate） *(A3 cardchain-deck-generator, 2026-05-27)*
- [x] 选项生成器（按 `INTERFACE.md` 第五节"发牌算法"实现 Option F 合法位扩展守卫版）： *(A3 cardchain-deck-generator, 2026-05-27)*
  - [x] deck 构成：40 Number + 8 Reverse = 48 张，**王牌不进 deck**
  - [x] `SolvableRate` 决定本轮是否抽 1 张合法牌（轮级有解概率，下界语义）
  - [x] `WildAppearRate` 独立判定是否塞 1 张王牌
  - [x] 剩余位填非法牌；选项内不重复，跨轮可重
  - [x] **合法位扩展守卫**：`(null,null,*)` 状态非法池为空、`(C,null,*)` 状态非法池可能不足，缺口转为合法位补齐
  - [x] 反转牌仅作为合法/非法牌候选自然出现，不强塞
  - [x] `Empty` 永不出现在选项中
  - [x] 计算并返回 `isDeadlock` 标志（选项中无任一合法牌；`lastColor==null` 状态恒 false）
  - [x] 选项数组 Fisher-Yates 洗牌，位置不可预测（详见 `GAME_DESIGN.md` 3.5.4）
- [ ] `HackSession` 内部状态机：
  - [ ] `SessionState`：lastColor / lastNumber / direction
  - [ ] `IsValidNext()` 严格 ±1 升降序判定（lastColor==null 任意 ∨ 同色任意 ∨ 异色严格 ±1；反转后 lastColor!=null+lastNumber==null 异色全非法）
  - [ ] `ApplyPrev()` 数字/反转/王牌的状态更新与方向翻转
  - [ ] 反转牌 +1 maxPot、王牌 +4 maxPot（满档前生效，满档后冻结）
  - [ ] 满档单向 latch：`chain >= maxPot` 首次成立后冻结
  - [ ] 溢出计数：满档后每多接一张合法牌 +1
- [ ] `HackSession`：完整会话逻辑
  - [ ] 计时（`Tick` 驱动）
  - [ ] 选牌验证（基于 `IsValidNext`）
  - [ ] `Surrender()` API：玩家主动弃牌或 Unity 端死局窗口超时调用
  - [ ] 事件触发：OnNewRound（含 isDeadlock 参数）/ OnChainSuccess / OnChainFailed / OnTimeUp / OnMaxReached / OnOverflow / OnDirectionChanged / OnSessionEnd
  - [ ] CurrentCard 初始为 `CardData.Empty`，开局任意牌合法
  - [ ] `HackResult` 生成（含 BasePot / MaxPot / IsMaxReached / Reason）
- [ ] `HackResult`：`DamageReductionFactor = chain / basePot`，无上限 clamp
- [x] `EndReason`：`TimeUp / WrongCard / Surrender` *(A1 cardchain-types, 2026-05-26)*
- [x] `ComboType` 枚举（预留：None / SameColorTwice / SameDirectionTwice，不实现逻辑） *(A1 cardchain-types, 2026-05-26)*
- [ ] xUnit 测试覆盖关键判定：严格 ±1 升降序边界、反转后异色全非法、连续两张同色 Reverse 合法、王牌穿透、合法位扩展守卫、满档 latch、溢出计数、死局判定、Surrender 状态机
- [ ] 控制台测试程序：模拟完整骇入流程输出日志（含死局响应）

### 验收方式
控制台输出示例：
```
[HackSession] Start | basePot=8 Time=12.0s Options=3 Direction=Asc SolvableRate=0.7 WildRate=0.05
[Round 1] Current: Empty                  | Options: Red-5 / Yellow-Rev / Blue-3   | Deadlock=false
[Input] Select 0 (Red-5) → ✓ chain=1 maxPot=8
[Round 2] Current: Red-5 (Asc)            | Options: Red-Rev / Yellow-2 / Blue-9   | Deadlock=false
[Input] Select 0 (Red-Rev) → ✓ chain=2 maxPot=9 Direction=Desc
[Round 3] Current: Red-Rev (Desc)         | Options: Yellow-7 / Green-8 / Wild     | Deadlock=false
[Input] Select 2 (Wild) → ✓ chain=3 maxPot=13
[Round 4] Current: Wild                   | Options: Blue-9 / Green-2 / Yellow-5   | Deadlock=true
[FakePlayer] 立即 Surrender (死局突破)
[OnSessionEnd] chain=3 basePot=8 maxPot=13 factor=0.375 reason=Surrender
```

---

## Phase 2 — Unity TPS 基础（B独立）

**交付标准：角色可在场景中移动、瞄准、射击，敌人可被击中扣血，波次管理器可触发**

**可与 Phase 1 完全并行**

### 架构规范（本 Phase 全局约束）
- **QF 骨架先行**：所有业务逻辑模块开发前，先完成 Phase 2.0 QF 架构骨架注册；后续各 Phase 的 System/Model/Command 直接在骨架上追加，**禁止**绕过 QF 在 MonoBehaviour 间直接调用业务逻辑
- 敌人模型采用 Mech Pack（Phase 0 已导入到 `Assets/ThirdParty/Characters/Enemy/MechPack/`，含 mech_defender / mech_walker / robot_dog 三种）
- 敌人 AI 采用 Opsive **Behavior Designer**（Phase 0 已导入到 `Assets/ThirdParty/AI/BehaviorDesigner/`，资产包形式不走 Package Manager）

---

### B0 — QF Architecture 骨架 ✅ 已完成（change: `unity-qf-skeleton`，归档 2026-05-26）

> **已完成，可开始后续 Phase 2 子任务**

- [x] 在 `GameApp.cs` 完整注册 Phase 2 所需的 Model 与 System 骨架（按 RegisterModel → RegisterSystem 顺序）：
  - `PlayerModel`：HP / MaxHp 属性（`BindableProperty<float>`，float 便于 Phase 4 伤害减免计算）
  - `WaveModel`：当前波次数 / 存活敌人数（`AliveCount`，B3c 扩展敌人列表）
  - `PlayerSystem`：持有 PlayerModel，`TakeDamage(float)` 扣血（Mathf.Max 防负值）
  - `WaveSystem`：持有 WaveModel，`OnStartWave()` / `OnEnemyKilled()` 骨架（B3c 填充）
- [x] Command 骨架声明（只定义类，`OnExecute` 空实现）：`StartHackCommand` / `SelectCardCommand` / `HealCommand` / `DamagePlayerCommand`
- [x] `QFrameworkValidator.cs` 更新：覆盖 Phase 2 System/Model 路径验证，Play Mode 输出 `[QF验证通过] Phase2 骨架 System/Model 链路正常`

---

### Phase 2.1 — 角色控制器（方案B补丁，约 4 个 changes）

> **交付标准**：RifleGirl 模型在场景中正确运动，MagicaCloth2 布料物理正常，基础动画素材统一为 CombatGirls 风格，持枪上半身动画正确叠加，瞄准相机切换有效，输入入口 QF 化（Model 单一来源）

#### 技术选型决策（已确认）

| 问题 | 决策 | 原因 |
|------|------|------|
| 换模型方式 | 嵌入 `Rifle_Full_Body.prefab` 作为子对象 | CombatGirls 有分件结构（15个部件），布料部件（Rifle_Dress / Rifle_Jacket）挂有 MagicaCloth2 组件，单替换 Mesh 会丢失布料物理 |
| 相机方案 | 两台 Cinemachine 虚拟相机 Priority 切换 | 标准 Cinemachine 模式，参数隔离，过渡由 Brain 自动处理 |
| 3D UI 相机 | 不新增，使用 World Space Canvas | 符合 GAME_DESIGN 设计意图（全息投影感），Main Camera 自动渲染 World Space Canvas，无需额外相机 |
| 上半身Layer 非瞄准状态 | 权重 = 0（完全走 base layer Blend Tree） | 非瞄准时不干预下半身主动画，瞄准时才叠加持枪动画 |
| 基础动画素材归属 | Base Layer 全部 Motion 替换为 RifleGirl 动画 | StarterAssets 仅提供骨架与控制器逻辑（移动/跳跃状态机），动画素材统一 CombatGirls 风格；通过复制 controller 到 `_Project/` 实现，不修改第三方文件 |
| 输入接入策略 | Adapter 方案：PlayerController 接管 PlayerInput 回调，PlayerInputModel 为状态唯一源，StarterAssetsInputs 退化为 ThirdPersonController.cs 的下游 Adapter（单向同步） | 既符合 QF 单一来源规范，又避免 fork 第三方 ThirdPersonController.cs |

#### B1a：视觉模型嵌入 + 场景清理 ✅ 已完成（change: `unity-character-model-swap`，归档 2026-05-26）
- [x] 在 `PlayerArmature` 根对象下将 StarterAssets 原视觉子对象（`Geometry/Armature_Mesh`）**禁用**（保留备份，不删除）
- [x] 将 `Assets/ThirdParty/CombatGirls/RifleGirl/Prefab/Rifle_Full_Body.prefab` 作为子对象嵌入 `PlayerArmature` 根下，位置归零对齐
- [x] 在 `PlayerArmature` 根对象的 `Animator` 组件上，将 Avatar 切换为 RifleGirl 的 Humanoid Avatar（`Humanoid_FAvatar`，来自 `Humanoid_F.fbx`）；RifleGirl 内部 Animator 已禁用防双冲突
- [x] 删除 SampleScene 中多余的 Main Camera / AudioListener，保持场景只有一个 AudioListener
- [x] 验证 MagicaCloth2 布料物理（Rifle_Dress、Rifle_Jacket）在 Play Mode 下正常模拟
- [x] Play Mode 验证：角色正确显示，材质无紫色（URP Toon Shader 已转换），移动动画通过 Humanoid Retargeting 正常播放

> **B1a 遗留偏差**：仅切换了 Avatar，Controller 仍指向 StarterAssets 自带版本，Base Layer 动画来源未脱离 StarterAssets。该偏差由下一个 change（B1b.1）修复。

#### B1b.1：基础动画归属修复 ✅ 已完成（change: `unity-character-base-anim-swap`，归档 2026-05-28）

> 修复 B1a 遗留的动画归属偏差，把 Base Layer 动画素材统一切到 RifleGirl + FemaleRunnerAnimset 风格。Apply 期发现"换素材"远比预想复杂，跳跃链路反复调试 7 轮才得到可接受方案，详细诊断与未来改进方向见 `Docs/AboutTheAnimation.md`。

**前置摸底（已完成）：**
- [x] 扫描 RifleGirl + FemaleRunnerAnimset 可用基础动画清单（含 clip 时长实测）
- [x] 对照 SA controller 状态机节点，锁定 Motion 替换方案；缺失素材由 FRA 补
- [x] 锁定 armed idle 选型（R_Idle）

**Controller 替换（已完成）：**
- [x] `AssetDatabase.CopyAsset` 复制 `StarterAssetsThirdPerson.controller` 到 `Assets/_Project/Animations/Player/UnomataPlayer.controller`（独立 GUID）
- [x] Base Layer 内 8 个 Motion 槽全部替换：Idle/Walk/Run BlendTree → R_Idle/R_Walk/R_Run；JumpStart → **R_Jump_AirR**（apply 期方案 Y 改用滞空姿态素材）；InAir → R_Jump_AirL；JumpLand BT 三槽 → R_Land_2h/R_Land_ToRun1/R_Land_ToRun3
- [x] 状态机拓扑 + 5 条 transition 字段全部保持 SA 原值；唯一 State Speed 调整：JumpStart.speed=3.0（与方案 Y 联合，让 R_Jump_AirR 实际播放与 SA 节奏对齐）
- [x] `PlayerArmature` 根 Animator Controller 切换到 `UnomataPlayer.controller`
- [x] 第三方文件未改动（`git status -- Assets/ThirdParty/` 输出空）
- [x] 新增 `PlayerAnimEventReceiver.cs`（吞 RifleGirl 内嵌 SwitchSocket 事件避免 Console 红错）

**Play Mode 验收（已通过）：**
- [x] 站立/移动/奔跑/跳跃/落地全部播放 RifleGirl 风格动画
- [x] Console 0 红错 0 警告（SwitchSocket 已 stub 处理）
- [x] 原 StarterAssets controller 文件未被改动

**已知遗留（不阻塞 B1b.1 归档，由后续 change 处理）：**
- ~~走/跑/落地音效失声（SA 的脚步声依赖 fbx 内嵌 OnFootstep 事件，RifleGirl/FRA fbx 无此事件）→ 下一个 change 处理~~ → ✅ 已由 B1c.1 (`unity-character-footstep-events`) 解决（归档 2026-05-28）；跑步听感节奏不均问题转 B1c.2 治理
- 跳跃手感仍有"别扭"感但无明显错误 → 详见 `Docs/AboutTheAnimation.md` "未来彻底解决的方向" 段

#### B1b.2：上半身瞄准动画层 + 双相机切换（change: `unity-character-aim-layer`，待开始）

> 在 B1b.1 产出的 `UnomataPlayer.controller` 上新增 UpperBodyAim Layer + 接入 Cinemachine 双相机。输入暂用临时驱动器，B1b.3 替换。

**Avatar Mask：**
- [ ] 新建 `Assets/_Project/Animations/Player/UpperBody.mask`（Humanoid Mask，Spine 以上 + 双臂，下半身 + Root 不勾）

**动画层（在 `UnomataPlayer.controller` 上扩展）：**
- [ ] 新增 Layer `UpperBodyAim`：Override，Weight = 0，绑定 `UpperBody.mask`
- [ ] 新增参数：`IsAiming` (bool)
- [ ] `UpperBodyAim` 状态机：`Empty` ↔ `AimMove`（`IsAiming` 触发）；`AimMove` 含 9-motion 2D Cartesian BlendTree（`R_AimIdle` 中心 + 8 方向 `R_AimWalk_F/B/L/R/FL/FR/BL/BR`）
- [ ] BlendTree 输入参数复用 base layer 移动参数（启动 change 前确认参数名）
- [ ] Layer Weight 由脚本驱动（`Animator.SetLayerWeight` + `Mathf.MoveTowards`），约 0.15s 完成 0↔1

**Cinemachine 双相机：**
- [ ] `PlayerFollowCamera`（已有，Priority = 10）保持不变
- [ ] 新建 `PlayerAimCamera`（VirtualCamera，Priority = 0）：
  - Body：Framing Transposer / 3rd Person Follow（依 Cinemachine 版本，B1b.2 启动前确认），右肩偏移（X ≈ 0.5）
  - FOV = 40（Phase 5 平衡时调整）
- [ ] `AimStateChangedEvent` 触发 `PlayerAimCamera.Priority = 15 / 0`，由 Brain 自动过渡

**QF 数据流（不含输入入口）：**
- [ ] `PlayerModel.IsAiming`（`BindableProperty<bool>`）
- [ ] `SetAimStateCommand` → `PlayerSystem.SetAiming(bool)` → 写 Model + `SendEvent<AimStateChangedEvent>`
- [ ] `AnimatorAimBridge` / `CameraAimBridge`（独立 MB，IController，订阅 Event）
- [ ] 临时输入：`TempAimInputDriver`（读 `Input.GetMouseButton(1)` 发 Command，B1b.3 删除）

**Play Mode 验收：**
- [ ] 普通移动/瞄准移动上半身动画正确叠加，下半身位移由 base layer 驱动
- [ ] 双相机切换平滑无抖动
- [ ] Console 零红色错误

#### B1b.3：输入入口 QF 化（change: `unity-player-input-qf-bridge`，待开始）

> 把输入入口从 `StarterAssetsInputs` 接管到 `PlayerController`，让 `PlayerInputModel` 成为状态唯一源。`StarterAssetsInputs` 退化为 Adapter 缓冲，不再绑 PlayerInput 回调。同时清理 B1b.2 的临时输入驱动器。

**新建：**
- [ ] `Player/PlayerInputModel.cs`：`Move` / `Jump` / `Sprint` / `IsAiming` / `Fire` 全部 `BindableProperty<>`
- [ ] `Player/PlayerController.cs`（IController）：订阅 PlayerInput Action 回调 → `SendCommand<SetXxxInputCommand>`
- [ ] `Player/SAInputAdapter.cs`（`[DefaultExecutionOrder(-10)]`）：Update 内单向 Model → `StarterAssetsInputs` 字段同步
- [ ] `Commands/SetMoveInputCommand` / `SetJumpInputCommand` / `SetSprintInputCommand` / `SetFireInputCommand`

**修改：**
- [ ] `GameApp.cs` 注册 `PlayerInputModel`（PlayerModel 之后）
- [ ] `PlayerSystem.OnInit` 订阅 `PlayerInputModel.IsAiming` 变化（Model→System 单向数据流）
- [ ] 场景：`PlayerArmature` 上 `PlayerInput` Behavior 改为 `Invoke C# Events`/`Invoke Unity Events`，回调由 `StarterAssetsInputs.OnXxx` 改连到 `PlayerController.OnXxx`

**清理：**
- [ ] 删除 B1b.2 留下的 `TempAimInputDriver.cs`

**Play Mode 验收：**
- [ ] 输入全部走 QF 链路（PlayerInput → Command → Model → System / Adapter）
- [ ] 临时禁用 `PlayerController` → 角色完全无响应（验证唯一入口）
- [ ] B1b.2 视觉验收行为保持一致（动画层/双相机切换无回退）

> **方案 B 补丁清单回顾**（原 Phase 0 验证结论）：两者均 Humanoid Rig，Mecanim 自动重定向；Phase 2.1 通过 B1a～B1b.3 四个 change 完整对接，把 PlayerArmature 从"骨架重定向 SA 动画 + SA 输入"过渡到"完整 RifleGirl 动画素材 + QF 化输入入口"。

---

### Phase 2.2 — 角色音频补回 + Audio QF 化（约 2 个 changes）

> **交付标准**：B1b.1 换素材丢失的走/跑/落地音效恢复；后续把音频系统从 ThirdPersonController 私有方法收编进 QF AudioSystem，为枪声/命中音/UI 音预留接入点

> **背景**：B1b.1 把 SA 自家素材换成 RifleGirl/FRA 后，原 fbx 内嵌的 `OnFootstep`/`OnLand` AnimationEvent 不再触发，所有走/跑/落地音效失声。详见 `Docs/AboutTheAnimation.md` 末段。

#### B1c.1：脚步/落地音效补回（fbx events 注入）✅ 已完成（change: `unity-character-footstep-events`，归档 2026-05-28）
- [x] 通过 `ModelImporter.clipAnimations[].events` API（写入 .meta，不改 fbx 二进制）为 5 个 clip 注入事件：
  - `R_Walk.fbx` / `R_Run.fbx` 加 `OnFootstep`（程序化标定 LF/RF Y 最低点：R_Walk=[0.2864, 0.7990]、R_Run=[0.2714, 0.7889]）
  - `R_Land_2h.fbx` / `R_Land_ToRun1.fbx` / `R_Land_ToRun3.fbx` 加 `OnLand`（time=0 进入即触发）
- [x] 全部事件 `messageOptions = DontRequireReceiver`，避免 QF 化前后切换时不同 receiver 报错
- [x] 沿用 SA 现有 clips：`FootstepAudioClips[10]` + `LandingAudioClip` 仍由 ThirdPersonController 持有并自播（其私有 `OnFootstep`/`OnLand` 自动收 SendMessage）
- [x] Play Mode 验收：走/跑/落地音效恢复 ✓；走路听感可接受；**跑步听感"间隔不一致"留 B1c.2 治理**（根因：SA 10 段脚步音 wav 长度 0.264~0.346s 参差不齐 + R_Run 0.333s 触发间距重叠最长音 + TPC `Random.Range` 抽段，非事件相位问题）

#### B1c.2：Audio QF 化骨架（迁移音频出口至 AudioSystem）✅ 已完成（change: `unity-audio-system-qf`，归档 2026-05-28）

> **实施说明**：apply 期评估后放弃方案 A（AnimationEvent 接力 PlayerAnimEventReceiver → SendCommand），改用**方案 B（AudioBridge.Update 相位驱动）**。根因：PlayerArmature 有两个 Animator（PlayerArmature 跑 UnomataPlayer、Rifle_Full_Body 跑 Rifle_Controller），AnimationEvent SendMessage 只路由到事件所属 Animator 的同 GameObject，多 Animator 架构下极难可靠接收；且 BlendTree weight=0 时事件 clip info 为空、time=0 落地事件被状态机过渡吞掉。方案 B 完全绕开 AnimationEvent 机制，更健壮。
>
> **方案 B 关键实现**：
> - `AudioBridge`（MonoBehaviour + IController）的 `Update()` 每帧读 `Animator.GetCurrentAnimatorStateInfo(0)` + `GetCurrentAnimatorClipInfo(0)`
> - 脚步相位阈值（B1c.1 程序化标定）：Walk LF=0.2864/RF=0.7990，Run LF=0.2714/RF=0.7889
> - dominant clip weight > 0.5f 过滤，Idle clip 排除，防静止误触发
> - `_wasInWalkRunBlend` 标志：状态切入首帧重置 prevNormalizedTime，防落地后误触发
> - 落地音：检测首帧进入 JumpLand 状态触发
> - **此模式适用于后续所有动画驱动音效**（敌人移动音等），无需依赖 fbx AnimationEvent

- [x] `Audio/AudioModel`：持脚步 clips 数组（`AudioClip[]`）、落地 clip（`AudioClip`）、`MasterVolume BindableProperty<float>` 预留
- [x] `Audio/AudioSystem`：暴露 `PlayFootstep(Vector3 pos)` / `PlayLand(Vector3 pos)` / `Play(SoundId, Vector3 pos)` 接口，内部 `SendEvent` 对应 QF Event
- [x] `Audio/AudioEvents.cs`：`FootstepPlayedEvent` / `LandPlayedEvent` / `SoundPlayedEvent` struct
- [x] `Audio/SoundId.cs`：`Footstep / Land / GunShot / HitSurface / HitEnemy / UIClick` 枚举
- [x] `Commands/PlayFootstepCommand` / `PlayLandCommand`：AbstractCommand，构造器接收 Vector3 pos
- [x] `AudioBridge`（方案 B）：Update 相位驱动脚步音 + 首帧 JumpLand 状态落地音；`Start()` 仅订阅 SoundPlayedEvent；不再依赖 AnimationEvent
- [x] `PlayerAnimEventReceiver` 退回普通 MonoBehaviour（仅保留 SwitchSocket 占位），不实现 IController，不转发 PlayFootstepCommand / PlayLandCommand
- [x] R_Walk/R_Run/R_Land_* 五个 fbx .meta 内 OnFootstep/OnLand AnimationEvent 全部清除（由 B1c.1 注入、B1c.2 清除）
- [x] GameApp 注册 AudioModel + AudioSystem（RegisterModel → RegisterSystem 顺序）
- [x] TPC Inspector `FootstepAudioClips`/`LandingAudioClip` 字段迁空（音频出口完全移出 TPC）
- [x] 场景新建 `Audio` GameObject，挂 AudioBridge + 2 AudioSource；10 段 SA 脚步音预筛 ≤0.313s（筛后 5 段）写入 `_filteredFootstepClips`，落地音单独绑定
- [x] **跑步脚步声节拍治理（B1c.1 遗留）**：方案 B 相位驱动本质上解决了节奏不均问题（触发时机由相位标定控制，不再受 wav 长度差异影响）
- [x] Play Mode 验收：脚步/落地音正常 ✓；跑步节奏均匀 ✓；静止无杂音 ✓；Disable AudioBridge 后无声 ✓

---

### Phase 2.3 — 射击系统（约 2 个 changes）

> **交付标准**：玩家可开枪，准星命中目标有视觉反馈，命中 Enemy 触发受击逻辑

#### B2a：射击输入 + Raycast 命中检测
- [ ] 射击输入（LMB / 手柄 RT），瞄准模式下开启连发
- [ ] 从相机中心发射 Raycast，检测 Layer `Enemy`
- [ ] 命中后调用敌人受击接口（`IDamageable.TakeDamage(float)`）
- [ ] 非瞄准状态下射击（腰射）精度降低（散布角偏移）

#### B2b：命中特效占位
- [ ] 命中普通表面：DecalProjector 弹孔或简单 ParticleSystem 占位
- [ ] 命中敌人：受击特效占位（红色粒子 / 闪烁），击杀时触发死亡临时效果
- [ ] HUD 占位：准星动态扩散（射击后短暂扩大）

---

### Phase 2.4 — 敌人基础 + 波次管理器（约 3~4 个 changes）

> **交付标准**：敌人可生成、可受击扣血、死亡；波次管理器可触发多波并推进

#### B3a：敌人 Prefab 骨架（模型占位）
- [ ] Enemy Prefab（初期用 Capsule 占位，敌人模型选定后替换）
- [ ] `EnemyController` MonoBehaviour（实现 `IController`，接入 QF）：
  - `float MaxHp` / `float CurrentHp`（`BindableProperty`）
  - `float DamageReductionFactor`（Phase 4 联动时由 Linking 层写入）
  - `IDamageable.TakeDamage(float raw)` → 实际伤害 = `raw × (1 - DamageReductionFactor)`
- [ ] 死亡处理：播放临时死亡效果，通知 WaveSystem 敌人已消灭，销毁 GameObject

#### B3b：敌人 AI（Behavior Tree）
- [ ] BT 包：Opsive **Behavior Designer**（Phase 0 补充工作已导入并验证，位于 `Assets/ThirdParty/AI/BehaviorDesigner/`），无需走 Package Manager
- [ ] 实现最小 BT：`Idle → Detect Player → Chase → Attack (Melee)` 状态
  - Idle：内置 `Actions/Idle`
  - Detect：内置 `Conditionals/Physics/`（OverlapSphere / Raycast）+ Tag/Layer 比较
  - Chase：内置 `Tasks/Unity/NavMeshAgent/SetDestination` 或 `Transform/MoveTowards`（NavMesh 在 Phase 6 升级）
  - Attack：自写一个 `MeleeAttack` Action（继承 Opsive `Action`），近战范围内定时调用 `this.SendCommand<DamagePlayerCommand>(damage)`
- [ ] 攻击对玩家造成 HP 伤害（通过 `this.SendCommand<DamagePlayerCommand>(damage)`，不直接操作 PlayerModel）

#### B3c：波次管理器（对接 WaveSystem/WaveModel）
- [ ] `WaveConfig` ScriptableObject：每波敌人数量 / 种类 / SpawnPoints 引用
- [ ] `WaveSystem.OnInit` 读取 WaveModel，`OnStartWave()` 按配置生成敌人 Prefab
- [ ] 监听全灭（敌人死亡时 WaveModel 更新存活数 → 归零触发 `OnWaveClear`）
- [ ] `OnWaveClear` → 延迟 3 秒 → 推进波次并发下一波（波次数写入 WaveModel）

> **注意**：`OnChainFailed` 扣血、SyncRate 增减逻辑**不在此 change 实现**，等 Phase 4 接入 HackSystem 后统一处理。

---

### Phase 2.5 — 骇入触发检测（约 1~2 个 changes）

> **交付标准**：按骇入键准星命中有效目标时，Console 打印目标信息；不接 HackSession

#### B4：HackTrigger 组件
- [ ] `HackTrigger` MonoBehaviour（实现 `IController`）：
  - 监听骇入键（默认 F / 手柄 LB），无骇入中时触发检测
  - 0.2~0.3 秒防误触冷却（同时防止骇入结束瞬间再触发）
  - Raycast 检测准星命中范围内最近 Enemy（Layer `Enemy`）
  - 命中 → `Debug.Log($"[HackTrigger] 命中 {target.name}，等待 Phase 4 接入")` 占位
  - 未命中 / 已有骇入中 → 无反应
- [ ] `StartHackCommand` 在此 change 打通至 HackSystem（HackSystem 此阶段仍为空实现，仅打 Log）

---

### 注意（全 Phase 2）
- 敌人需要暴露 `float DamageReductionFactor` 属性，Phase 4 联动时由 Linking 层写入
- 骇入触发逻辑只做检测，**不接 HackSession**，等 Phase 4
- 敌人模型待选定，Phase 2.4 C1 先用 Capsule 占位；选定后在 Phase 2.4 C1 归档前完成 Prefab 替换，或单独一个补丁 change 完成外观替换
- SyncRate 系统（PlayerSystem 受伤下降）在 Phase 4 实现，Phase 2 只做 HP 扣减

---

## Phase 3 — 副线 UI（B，依赖 Phase 1 接口冻结）

**交付标准：骇入 UI 可在编辑器中手动驱动，正确显示当前牌、选项、倒计时、接龙计数**

**依赖：INTERFACE.md 接口冻结（Phase 1 开始后接口即冻结）**

### 任务清单
- [ ] 世界空间 Canvas 搭建（悬浮于玩家附近）
- [ ] 当前牌显示组件
- [ ] 选项牌列表组件（支持3~5个动态布局）
- [ ] 倒计时进度条
- [ ] 接龙计数 / 满档进度显示
- [ ] 骇入激活/关闭的 UI 动画（展开/折叠）
- [ ] 满档特效、失败特效占位

---

## Phase 4 — 双线联动对接（A+B）

**交付标准：完整游戏循环可运行，骇入结果正确影响目标敌人的伤害减免**

**依赖：Phase 1 + Phase 2 均完成**

### 任务清单
- [ ] A：将 Core 源码复制到 `Assets/_Project/Scripts/Core/`
  - [ ] 复制 `CardChainCore/src/Unomata.Core/*.cs`（仅 .cs，不含 .csproj）至 `Assets/_Project/Scripts/Core/`
  - [ ] 在该目录建 `Unomata.Core.asmdef`，`noEngineReferences=true`、`autoReferenced=true`
  - [ ] Unity 编译通过，Console 零红色错误
  - [ ] `tests/` 与 `console/` 保留在 `CardChainCore/`，不迁入 Unity
- [ ] B：`HackTrigger` 组件接入 `HackSession`（创建、驱动、订阅事件）
- [ ] B：`SyncRateModel` + `SyncRateSystem`（QFramework 分层），处理拾取/受伤/击杀对 SyncRate 的影响
- [ ] B：触发骇入时按 `SolvableRate = 0.5 + 0.45 × SyncRate` 生成 config
- [ ] B：弃牌键复用骇入键（含 0.2~0.3 秒防误触冷却）；任意时刻按下 → `session.Surrender()`
- [ ] B：死局反应窗口实现——监听 `OnNewRound(..., isDeadlock=true)` 启动倒计时；窗口内主动弃牌 → SyncRate 奖励；超时 Unity 主动 `Surrender()` 不奖励
- [ ] B：`Linking` 层——将 `OnSessionEnd` 的 `DamageReductionFactor` 写入目标敌人（factor > 1 时附加额外受击加成）
- [ ] B：`OnChainFailed` → Phase 1 占位扣固定血量；Phase 5 平衡按 `GAME_DESIGN.md` 3.7.2 候选方案敲定
- [ ] B：`OnOverflow` → 生命回复技能充能+1
- [ ] A+B：联调，验证所有事件通路正确（含死局突破奖励链路）

---

## Phase 5 — 难度曲线与数值调整（A+B）

**交付标准：10波以上游戏体验流畅，难度递进明显**

### 任务清单
- [ ] 确定骇入效果持续时间数值
- [ ] 敲定 `OnChainFailed` 惩罚机制（`GAME_DESIGN.md` 3.7.2 三个候选版本之一）
- [ ] 确定满档额外伤害加成数值
- [ ] 确定生命回复技能缓存上限
- [ ] 确定 SyncRate 增量数值（道具拾取 / 击杀掉落 / 死局突破）
- [ ] 确定 SyncRate 受伤下降比例（方式 c：按伤害量 / 玩家最大血量）
- [ ] 确定死局反应窗口时长（`DEADLOCK_WINDOW_SEC`）
- [ ] 确定 `WildAppearRate` 数值
- [ ] 确定 `SyncRate → SolvableRate` 映射公式（暂定 `0.5 + 0.45 × x` 是否需调整）
- [ ] 波次 → 难度参数的映射曲线调整（OptionCount / TotalTime / TargetChainCount）
- [ ] 多轮游玩测试，收集体感反馈

---

## Phase 6 — 打磨与验证（A+B）

**交付标准：能够回答 GAME_DESIGN.md 第六章的三个验证目标**

### 任务清单
- [ ] VFX 特效替换（命中、受击、骇入波）
- [ ] 音效占位（可无）
- [ ] UI 科幻风格精修
- [ ] Bug 修复
- [ ] 录制游玩视频，记录验证结论

---

## 关键依赖关系

```
Phase 0 ──→ Phase 1（A）
         └─→ Phase 2（B）

Phase 1 接口冻结 ──→ Phase 3（B可开始）

Phase 1 完成
Phase 2 完成  ──→ Phase 4（联调）

Phase 4 ──→ Phase 5 ──→ Phase 6
```

B 在等 Phase 1 完成之前，Phase 2 和 Phase 3 可以完全并行推进。

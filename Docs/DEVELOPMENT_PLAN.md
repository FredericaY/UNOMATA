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
- [x] 敲定敌人 BT 框架选型 = Opsive Behavior Designer（已导入并验证），同步 `DEPENDENCIES.md` 与 Phase 2.3 C2 任务行

---

## Phase 1 — Core 层开发（A独立）

**交付标准：控制台项目可完整运行一次骇入流程，所有事件正确触发**

### 任务清单
- [x] `CardData` + `CardType` / `CardColor` / `ChainDirection` 枚举：纯数据，含 `CardData.Empty` 静态实例 *(Change 1 cardchain-types, 2026-05-26)*
- [x] `HackDifficultyConfig`：难度参数数据类（OptionCount / TargetChainCount / TotalTime / SolvableRate / WildAppearRate） *(Change 3 cardchain-deck-generator, 2026-05-27)*
- [x] 选项生成器（按 `INTERFACE.md` 第五节"发牌算法"实现 Option F 合法位扩展守卫版）： *(Change 3 cardchain-deck-generator, 2026-05-27)*
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
- [x] `EndReason`：`TimeUp / WrongCard / Surrender` *(Change 1 cardchain-types, 2026-05-26)*
- [x] `ComboType` 枚举（预留：None / SameColorTwice / SameDirectionTwice，不实现逻辑） *(Change 1 cardchain-types, 2026-05-26)*
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

### Phase 2.0 — QF Architecture 骨架 ✅ 已完成（change: `unity-qf-skeleton`，归档 2026-05-26）

> **已完成，可开始后续 Phase 2 子任务**

- [x] 在 `GameApp.cs` 完整注册 Phase 2 所需的 Model 与 System 骨架（按 RegisterModel → RegisterSystem 顺序）：
  - `PlayerModel`：HP / MaxHp 属性（`BindableProperty<float>`，float 便于 Phase 4 伤害减免计算）
  - `WaveModel`：当前波次数 / 存活敌人数（`AliveCount`，B3c 扩展敌人列表）
  - `PlayerSystem`：持有 PlayerModel，`TakeDamage(float)` 扣血（Mathf.Max 防负值）
  - `WaveSystem`：持有 WaveModel，`OnStartWave()` / `OnEnemyKilled()` 骨架（B3c 填充）
- [x] Command 骨架声明（只定义类，`OnExecute` 空实现）：`StartHackCommand` / `SelectCardCommand` / `HealCommand` / `DamagePlayerCommand`
- [x] `QFrameworkValidator.cs` 更新：覆盖 Phase 2 System/Model 路径验证，Play Mode 输出 `[QF验证通过] Phase2 骨架 System/Model 链路正常`

---

### Phase 2.1 — 角色控制器（方案B补丁，约 2 个 changes）

> **交付标准**：RifleGirl 模型在场景中正确运动，MagicaCloth2 布料物理正常，持枪上半身动画正确叠加，瞄准相机切换有效

#### 技术选型决策（已确认）

| 问题 | 决策 | 原因 |
|------|------|------|
| 换模型方式 | 嵌入 `Rifle_Full_Body.prefab` 作为子对象 | CombatGirls 有分件结构（15个部件），布料部件（Rifle_Dress / Rifle_Jacket）挂有 MagicaCloth2 组件，单替换 Mesh 会丢失布料物理 |
| 相机方案 | 两台 Cinemachine 虚拟相机 Priority 切换 | 标准 Cinemachine 模式，参数隔离，过渡由 Brain 自动处理 |
| 3D UI 相机 | 不新增，使用 World Space Canvas | 符合 GAME_DESIGN 设计意图（全息投影感），Main Camera 自动渲染 World Space Canvas，无需额外相机 |
| 上半身Layer 非瞄准状态 | 权重 = 0（完全走 StarterAssets Blend Tree） | 非瞄准时不干预下半身主动画，瞄准时才叠加持枪动画 |

#### C1：视觉模型嵌入 + 场景清理 ✅ 已完成（change: `unity-character-model-swap`，归档 2026-05-26）
- [x] 在 `PlayerArmature` 根对象下将 StarterAssets 原视觉子对象（`Geometry/Armature_Mesh`）**禁用**（保留备份，不删除）
- [x] 将 `Assets/ThirdParty/CombatGirls/RifleGirl/Prefab/Rifle_Full_Body.prefab` 作为子对象嵌入 `PlayerArmature` 根下，位置归零对齐
- [x] 在 `PlayerArmature` 根对象的 `Animator` 组件上，将 Avatar 切换为 RifleGirl 的 Humanoid Avatar（`Humanoid_FAvatar`，来自 `Humanoid_F.fbx`）；RifleGirl 内部 Animator 已禁用防双冲突
- [x] 删除 SampleScene 中多余的 Main Camera / AudioListener，保持场景只有一个 AudioListener
- [x] 验证 MagicaCloth2 布料物理（Rifle_Dress、Rifle_Jacket）在 Play Mode 下正常模拟
- [x] Play Mode 验证：角色正确显示，材质无紫色（URP Toon Shader 已转换），移动动画通过 Humanoid Retargeting 正常播放

#### C2：上半身动画层 + 双相机瞄准切换（合并）

**动画层（Animator Controller）：**
- [ ] 在 `StarterAssetsThirdPerson.controller`（或复制为项目自有 Controller）中新增 Layer `UpperBodyAim`
  - Blending：Override，默认 Weight = 0
  - 绑定 Avatar Mask（上半身：Spine 以上全部骨骼，下半身权重=0）
- [ ] `UpperBodyAim` Layer 状态机：`Empty` → （瞄准触发）→ `AimIdle`（对接 `R_AimIdle`）+ 移动 blend tree（`R_AimWalk_F` / `R_AimWalk_B` / `R_AimWalk_L` / `R_AimWalk_R`）
- [ ] 瞄准输入触发时，通过 Animator 参数将 `UpperBodyAim` Layer 权重渐变到 1；退出瞄准时渐变回 0

**Cinemachine 双相机：**
- [ ] `PlayerFollowCamera`（已有，Priority = 10）：普通跟随，保持 StarterAssets 默认设置
- [ ] 新建 `PlayerAimCamera`（VirtualCamera，Priority 默认 = 0）：
  - Body：Framing Transposer，肩膀偏移（Camera Offset X ≈ 0.5 右肩）
  - Aim：Composer，Center On Activate
  - FOV = 40（普通跟随 FOV = 60，具体数值 Phase 5 平衡）
- [ ] 瞄准输入（RMB / 手柄 LT）时：`PlayerAimCamera.Priority = 15`，`PlayerFollowCamera.Priority = 10` → Brain 自动过渡
- [ ] 退出瞄准：`PlayerAimCamera.Priority = 0`，回到 `PlayerFollowCamera`
- [ ] Play Mode 验证：普通移动与瞄准移动的上半身动画正确叠加，两相机切换平滑，无画面抖动

> **方案 B 补丁清单回顾**（原 Phase 0 验证结论）：两者均 Humanoid Rig，Mecanim 自动重定向；需以上两个 change 完成对接。

---

### Phase 2.2 — 射击系统（约 2 个 changes）

> **交付标准**：玩家可开枪，准星命中目标有视觉反馈，命中 Enemy 触发受击逻辑

#### C1：射击输入 + Raycast 命中检测
- [ ] 射击输入（LMB / 手柄 RT），瞄准模式下开启连发
- [ ] 从相机中心发射 Raycast，检测 Layer `Enemy`
- [ ] 命中后调用敌人受击接口（`IDamageable.TakeDamage(float)`）
- [ ] 非瞄准状态下射击（腰射）精度降低（散布角偏移）

#### C2：命中特效占位
- [ ] 命中普通表面：DecalProjector 弹孔或简单 ParticleSystem 占位
- [ ] 命中敌人：受击特效占位（红色粒子 / 闪烁），击杀时触发死亡临时效果
- [ ] HUD 占位：准星动态扩散（射击后短暂扩大）

---

### Phase 2.3 — 敌人基础 + 波次管理器（约 3~4 个 changes）

> **交付标准**：敌人可生成、可受击扣血、死亡；波次管理器可触发多波并推进

#### C1：敌人 Prefab 骨架（模型占位）
- [ ] Enemy Prefab（初期用 Capsule 占位，敌人模型选定后替换）
- [ ] `EnemyController` MonoBehaviour（实现 `IController`，接入 QF）：
  - `float MaxHp` / `float CurrentHp`（`BindableProperty`）
  - `float DamageReductionFactor`（Phase 4 联动时由 Linking 层写入）
  - `IDamageable.TakeDamage(float raw)` → 实际伤害 = `raw × (1 - DamageReductionFactor)`
- [ ] 死亡处理：播放临时死亡效果，通知 WaveSystem 敌人已消灭，销毁 GameObject

#### C2：敌人 AI（Behavior Tree）
- [ ] BT 包：Opsive **Behavior Designer**（Phase 0 补充工作已导入并验证，位于 `Assets/ThirdParty/AI/BehaviorDesigner/`），无需走 Package Manager
- [ ] 实现最小 BT：`Idle → Detect Player → Chase → Attack (Melee)` 状态
  - Idle：内置 `Actions/Idle`
  - Detect：内置 `Conditionals/Physics/`（OverlapSphere / Raycast）+ Tag/Layer 比较
  - Chase：内置 `Tasks/Unity/NavMeshAgent/SetDestination` 或 `Transform/MoveTowards`（NavMesh 在 Phase 6 升级）
  - Attack：自写一个 `MeleeAttack` Action（继承 Opsive `Action`），近战范围内定时调用 `this.SendCommand<DamagePlayerCommand>(damage)`
- [ ] 攻击对玩家造成 HP 伤害（通过 `this.SendCommand<DamagePlayerCommand>(damage)`，不直接操作 PlayerModel）

#### C3：波次管理器（对接 WaveSystem/WaveModel）
- [ ] `WaveConfig` ScriptableObject：每波敌人数量 / 种类 / SpawnPoints 引用
- [ ] `WaveSystem.OnInit` 读取 WaveModel，`OnStartWave()` 按配置生成敌人 Prefab
- [ ] 监听全灭（敌人死亡时 WaveModel 更新存活数 → 归零触发 `OnWaveClear`）
- [ ] `OnWaveClear` → 延迟 3 秒 → 推进波次并发下一波（波次数写入 WaveModel）

> **注意**：`OnChainFailed` 扣血、SyncRate 增减逻辑**不在此 change 实现**，等 Phase 4 接入 HackSystem 后统一处理。

---

### Phase 2.4 — 骇入触发检测（约 1~2 个 changes）

> **交付标准**：按骇入键准星命中有效目标时，Console 打印目标信息；不接 HackSession

#### C1：HackTrigger 组件
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
- 敌人模型待选定，Phase 2.3 C1 先用 Capsule 占位；选定后在 Phase 2.3 C1 归档前完成 Prefab 替换，或单独一个补丁 change 完成外观替换
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

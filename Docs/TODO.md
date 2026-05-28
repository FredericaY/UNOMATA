# TODO.md — Phase 1 & Phase 2 Change 拆分清单

> 本文档将 `DEVELOPMENT_PLAN.md` 中开发任务，按"单测可独立验证 + 依赖逻辑链"拆分为若干 OpenSpec change。
> 每个 change 完成后通过 `/opsx:archive` 归档，将 delta spec 同步到主 specs，再开始下一个。

---

## 拆分原则

- 单 change 改动控制在 200~600 行级（≈ 1 人天）
- 每个 change 都能独立 `dotnet build` + `dotnet test` 通过（Core 层）/ Unity Console 零红错（Unity 端）
- 严格线性依赖，前置 change 未归档前不开新 change
- 每个 change 的测试覆盖必须落到归档 spec 的 Scenario

---

# Phase 1 — Core 层 Change 清单（A 端）

## 依赖图

```
A1 → A2 → A3 → A4 → A5 → A6 → A7
```

---

## A1 — `cardchain-types` ✅ 已归档 (2026-05-26)

归档位置：`openspec/changes/archive/2026-05-26-cardchain-types/`
主 spec：`openspec/specs/cardchain-types/spec.md`

---

## A2 — `cardchain-validator` ✅ 已归档 (2026-05-26)

归档位置：`openspec/changes/archive/2026-05-26-cardchain-validator/`

验收：`dotnet test` → 109 通过（含 A1 的 17 个 + A2 新增 92 个）

---

## A3 — `cardchain-deck-generator` ✅ 已归档 (2026-05-27)

归档位置：`openspec/changes/archive/2026-05-27-cardchain-deck-generator/`

验收：`dotnet test` → 139 通过（109 旧 + 30 新）

---

## A4 — `hacksession-skeleton`

**职责**：`HackSession` 骨架——构造/计时/事件订阅、单轮选牌循环（不含 maxPot/latch/overflow）。

### 范围
- [ ] `class HackSession`：
  - [ ] 构造 `HackSession(HackDifficultyConfig config)`
  - [ ] 公开属性：`IsActive` / `ChainCount` / `TimeRemaining` / `CurrentCard` / `CurrentOptions` / `CurrentDirection` / `BasePot`
  - [ ] 8 个事件签名声明（OnNewRound 含 isDeadlock 参数）
  - [ ] `Start()`：初始化 state，CurrentCard = Empty，触发首轮 OnNewRound
  - [ ] `Tick(float deltaTime)`：减少 TimeRemaining
  - [ ] `SelectOption(int)`：合法 → ApplyPrev + chain++ + OnChainSuccess + 下一轮 OnNewRound；非法 → OnChainFailed
  - [ ] `Surrender()` 方法签名声明（空实现，A6 完善）
  - [ ] `TargetId`、`OnComboTriggered` 占位（v1 不实现）
- [ ] xUnit 覆盖：
  - [ ] Start 触发首轮 OnNewRound，CurrentCard.Type=Empty
  - [ ] Start 后 IsActive=true
  - [ ] Tick 正确减少 TimeRemaining
  - [ ] SelectOption 合法：chain+1、OnChainSuccess 触发、下一轮 OnNewRound 触发
  - [ ] SelectOption 非法：OnChainFailed 触发
  - [ ] SelectOption 越界索引抛异常或忽略（约定其一）

### 不含
- maxPot / latch / overflow 逻辑（A5）
- HackResult 与 OnSessionEnd（A6）
- TimeUp 触发结束（A6）

### 依赖
A3（OptionGenerator 用于生成每轮选项）

---

## A5 — `hacksession-rewardpot`

**职责**：双层奖励池 + 满档单向 latch + 溢出计数 + 方向切换事件。

### 范围
- [ ] `MaxPot` / `IsMaxLatched` / `OverflowCount` 属性
- [ ] 反转牌 prev：`IsMaxLatched=false` 时 `MaxPot += 1`
- [ ] 王牌 prev：`IsMaxLatched=false` 时 `MaxPot += 4`
- [ ] 接牌后判定顺序：chain++ → 牌效结算 → 满档判定（latch + OnMaxReached）→ 溢出判定（OnOverflow）
- [ ] `OnDirectionChanged` 在反转牌 prev 后触发，参数为新方向
- [ ] xUnit 覆盖：反转/王牌 MaxPot 增长、满档冻结、OnMaxReached 只触发一次、OnOverflow 累加、接牌顺序边界

### 依赖
A4（HackSession 骨架）

---

## A6 — `hacksession-result-and-end`

**职责**：会话结束的所有路径 + `HackResult` 计算 + `Surrender()` 完整实现。

### 范围
- [ ] `class HackResult`：`ChainCount` / `BasePot` / `MaxPot` / `OverflowCount` / `IsMaxReached` / `Reason`
- [ ] `DamageReductionFactor` 计算属性（`chain / basePot`，basePot=0 兜底 0）
- [ ] `Surrender()` 完整实现：任何状态合法，触发 `OnSessionEnd(Surrender)`
- [ ] `Tick`：`TimeRemaining <= 0` → OnTimeUp + OnSessionEnd(TimeUp)
- [ ] `SelectOption` 非法路径：OnChainFailed + OnSessionEnd(WrongCard)
- [ ] `IsActive` 结束后为 false，后续调用忽略或抛异常
- [ ] xUnit 覆盖：三种结束路径、DamageReductionFactor 边界、OnSessionEnd 只触发一次

### 依赖
A5（MaxPot/Latch 状态需被 HackResult 读取）

---

## A7 — `cardchain-console-demo`

**职责**：`Unomata.Core.Console` 主程序，跑通完整骇入流程并输出日志。

### 范围
- [ ] `Program.cs`：构造 config + 订阅 8 个事件 + 主循环 + 退出条件（OnSessionEnd）
- [ ] `class FakePlayer`：优先选第一张合法选项；死局立即 Surrender；满档后继续接
- [ ] 至少三种结束路径演示模式（TimeUp / Surrender / WrongCard）

### 验收
- [ ] `dotnet run --project console/Unomata.Core.Console` 输出与 DEVELOPMENT_PLAN 示例一致风格的日志
- [ ] 至少一次跑出 `Deadlock=true` + `[FakePlayer] 立即 Surrender`

### 依赖
A6（HackSession 已完整）

---

# Phase 2 — Unity TPS 基础 Change 清单（B 端）

> 对应 `DEVELOPMENT_PLAN.md` Phase 2，QF 骨架先行（B0），后续各 change 在骨架上追加，禁止绕过 QF。

## 依赖图

```
B0(QF骨架)
  ├──→ B1a(模型换装)
  │      └──→ B1b.1(基础动画归属修复)
  │                └──→ B1b.2(瞄准动画层+双相机)
  │                          └──→ B1b.3(输入QF化-Adapter方案)
  │                                    └──→ B1c.1(脚步落地音补回)
  │                                              └──→ B1c.2(Audio QF化骨架)
  │                                                        └──→ B2a(射击Raycast)
  │                                                                  └──→ B2b(命中特效)
  ├──→ B3a(敌人骨架)
  │      └──→ B3b(敌人AI BT)
  │                └──→ B3c(波次管理器)
  └──→ B4(骧入触发)       ← 依赖 B3a（需要可检测的 Enemy）
```

---

## B0 — `unity-qf-skeleton` ✅ 已归档 (2026-05-26)

归档位置：`openspec/changes/archive/2026-05-26-unity-qf-skeleton/`

---

## B1a — `unity-character-model-swap` ✅ 已归档 (2026-05-26)

归档位置：`openspec/changes/archive/2026-05-26-unity-character-model-swap/`

---

## B1b.1 — `unity-character-base-anim-swap` ✅ 已归档 (2026-05-28)

归档位置：`openspec/changes/archive/2026-05-28-unity-character-base-anim-swap/`
疑难备忘：`Docs/AboutTheAnimation.md`

---

## B1c.1 — `unity-character-footstep-events` ✅ 已归档 (2026-05-28)

归档位置：`openspec/changes/archive/2026-05-28-unity-character-footstep-events/`

---

## B1c.2 — `unity-audio-system-qf` ✅ 已归档 (2026-05-28)

归档位置：`openspec/changes/archive/2026-05-28-unity-audio-system-qf/`

> 关键决策：采用方案 B（AudioBridge.Update 相位驱动），不依赖 AnimationEvent。后续所有动画驱动音效均采用此模式。

---

## B1b.2 — `unity-character-aim-layer` ⬜ 待开始（依赖 B1b.1）

**职责**：在 `UnomataPlayer.controller` 上新增 UpperBodyAim 动画层，实现瞄准模式下上半身动画叠加 + Cinemachine 双相机 Priority 切换。输入暂用临时驱动器，B1b.3 替换。

### 范围

**Avatar Mask：**
- [ ] 新建 `Assets/_Project/Animations/Player/UpperBody.mask`（Humanoid Mask 模式）
  - 勾选：Spine / Chest / UpperChest / Neck / Head / 双臂全骨骼
  - 不勾：Hips / 双腿 / Root

**Animator Controller（在 B1b.1 产出的 `UnomataPlayer.controller` 上扩展）：**
- [ ] 新增 Layer `UpperBodyAim`：Override 模式，默认 Weight = 0，绑定 `UpperBody.mask`
- [ ] 新增 Animator 参数：`IsAiming` (bool)
- [ ] `UpperBodyAim` 状态机：
  - `Empty` 状态（默认）
  - `AimMove` 状态，含 9 motion BlendTree（2D Cartesian）：中心 `R_AimIdle` + 8 方向 `R_AimWalk_F/B/L/R/FL/FR/BL/BR`
  - `Empty ↔ AimMove` 由 `IsAiming` bool 触发
  - BlendTree 输入参数复用 base layer 已有的移动参数（前置任务确认参数名）
- [ ] Layer Weight 渐变由脚本驱动（`Animator.SetLayerWeight` + `Mathf.MoveTowards`），约 0.15s 完成 0↔1

**Cinemachine 双相机：**
- [ ] `PlayerFollowCamera`（已有，Priority = 10）保持不变
- [ ] 新建 `PlayerAimCamera`（VirtualCamera，默认 Priority = 0）：
  - Body：Framing Transposer / 3rd Person Follow（依 Cinemachine 版本，B1b.2 启动前确认），Camera Offset X ≈ 0.5（右肩）
  - FOV = 40（Phase 5 平衡时调整）
- [ ] 切换：`AimStateChangedEvent` 触发 `PlayerAimCamera.Priority = 15 / 0`，由 Brain 自动过渡

**QF 数据流（不含输入入口，输入留给 B1b.3）：**
- [ ] `PlayerModel` 增加 `BindableProperty<bool> IsAiming`
- [ ] `Commands/SetAimStateCommand.cs`：调 `PlayerSystem.SetAiming(bool)`
- [ ] `PlayerSystem.SetAiming(bool)`：写 `PlayerModel.IsAiming.Value`，`SendEvent<AimStateChangedEvent>`
- [ ] `Player/AimStateChangedEvent.cs`：`struct AimStateChangedEvent { bool IsAiming; }`
- [ ] `Player/AnimatorAimBridge.cs`（独立 MB，IController）：`RegisterEvent<AimStateChangedEvent>` → `Animator.SetBool("IsAiming", ...)` + Layer Weight 渐变
- [ ] `Camera/CameraAimBridge.cs`（独立 MB，IController）：`RegisterEvent<AimStateChangedEvent>` → 切 `PlayerAimCamera.Priority`

**临时输入驱动（B1b.3 删除）：**
- [ ] `Player/TempAimInputDriver.cs`：每帧读 `Input.GetMouseButton(1)` 状态变化，`SendCommand<SetAimStateCommand>(bool)`，文件头注释 `// TEMP: B1b.3 替换`

**Play Mode 验收：**
- [ ] 普通移动与瞄准移动上半身动画正确叠加（下半身位移不受影响）
- [ ] 双相机切换平滑无抖动，肩位偏移正确
- [ ] Console 零红色错误

### 依赖
B1b.1（`UnomataPlayer.controller` 已就位且 Base Layer 已切 RifleGirl 动画）

---

## B1b.3 — `unity-player-input-qf-bridge` ⬜ 待开始（依赖 B1b.2）

**职责**：把输入入口从 `StarterAssetsInputs` 接管到 QF 化的 `PlayerController`，让 Model 成为输入状态唯一来源。`StarterAssetsInputs` 退化为 Adapter，仅作为 ThirdPersonController.cs 的下游消费缓冲。

### 设计要点（Adapter 方案）

```
PlayerInput → PlayerController (IController) → SendCommand → PlayerInputModel
                                                               │
                              ┌────────────────────────────────┼────────────────────────────┐
                              ▼                                ▼                            ▼
                  PlayerSystem 订阅 IsAiming         SAInputAdapter (LateUpdate)    其他 System 直接读
                  → SendEvent<AimStateChanged>        Model → StarterAssetsInputs
                                                              字段单向同步
                                                              ↓ 读字段
                                                      ThirdPersonController.cs（第三方，不改）
```

### 范围

**新建文件：**
- [ ] `Player/PlayerInputModel.cs`：`BindableProperty<Vector2> Move` / `BindableProperty<bool> Jump` / `BindableProperty<bool> Sprint` / `BindableProperty<bool> IsAiming` / `BindableProperty<bool> Fire`
- [ ] `Player/PlayerController.cs`（IController）：持 `PlayerInput` 引用，订阅 InputAction 回调 → `SendCommand<SetXxxInputCommand>`
- [ ] `Player/SAInputAdapter.cs`（`[DefaultExecutionOrder(-10)]`）：Update 把 Model 字段单向写到 `StarterAssetsInputs`
- [ ] `Commands/SetMoveInputCommand.cs` / `SetJumpInputCommand.cs` / `SetSprintInputCommand.cs` / `SetFireInputCommand.cs`

**修改：**
- [ ] `GameApp.cs`：`RegisterModel<PlayerInputModel>`（PlayerModel 之后）
- [ ] `PlayerSystem.OnInit`：订阅 `PlayerInputModel.IsAiming` 变化（替代 B1b.2 中 Command 直接调 `SetAiming` 的链路）
- [ ] 场景：`PlayerArmature` 上 `PlayerInput` Behavior 改为 `Invoke C# Events`，回调由 `StarterAssetsInputs.OnXxx` 改连到 `PlayerController.OnXxx`

**清理：**
- [ ] 删除 B1b.2 留下的 `TempAimInputDriver.cs`

**Play Mode 验收：**
- [ ] 移动 / 跳跃 / 冲刺 / 瞄准全部走 QF 链路
- [ ] 临时禁用 `PlayerController` → 角色完全无响应
- [ ] `StarterAssetsInputs` PlayerInput 回调已断开（Inspector 检查）
- [ ] Console 零红色错误
- [ ] B1b.2 视觉验收全部行为保持一致

### 依赖
B1b.2

### 风险点
- `SAInputAdapter` 必须早于 `ThirdPersonController.Update`，用 `[DefaultExecutionOrder(-10)]`
- PlayerInput Behavior 必须改为 `Invoke C# Events`/`Invoke Unity Events` 才能切断 SA 默认路由
- 直接复用 StarterAssets 的 `.inputactions` asset，不新建 action map

---

## B2a — `unity-shooting-raycast` ⬜ 待开始（依赖 B1c.2）

**职责**：射击输入 + Raycast 命中检测 + 调用敌人受击接口。

### 范围
- [ ] `ShootingController`（实现 `IController`）：监听射击输入（LMB / RT）
  - 瞄准模式：连发（rate 占位常量）；非瞄准（腰射）：单发 + 散布偏移
  - 从相机中心发射 Raycast（`Physics.Raycast`），检测 Layer `Enemy`
  - 命中 → 获取 `IDamageable` 接口，调用 `TakeDamage(float baseDamage)`
- [ ] 定义 `IDamageable` 接口：`void TakeDamage(float rawDamage)`
- [ ] Unity Tags & Layers 添加 `Enemy` Layer

### 依赖
B1c.2（输入 QF 化、瞄准状态可查、AudioSystem 可接枪声）

---

## B2b — `unity-shooting-vfx` ⬜ 待开始（依赖 B2a）

**职责**：命中特效占位（弹孔、受击粒子、死亡效果、准星扩散）。

### 范围
- [ ] 命中普通表面：DecalProjector 弹孔占位（或简单 ParticleSystem）
- [ ] 命中敌人：受击特效占位（红色粒子闪烁）
- [ ] 击杀：死亡临时效果占位（Dissolve / 淡出，Phase 6 替换）
- [ ] HUD：准星动态扩散（`CrosshairController`，接入 QF UI 事件）

### 依赖
B2a

---

## B3a — `unity-enemy-scaffold` ⬜ 待开始（依赖 B0）

**职责**：敌人 Prefab 骨架，暴露 HP + DamageReductionFactor 属性，实现受击与死亡逻辑。

### 范围
- [ ] Enemy Prefab（Capsule 占位，敌人模型选定后替换）
  - `Collider` 设置为 Layer `Enemy`
- [ ] `EnemyController`（实现 `IController`）：
  - `float MaxHp` / `float CurrentHp`
  - `float DamageReductionFactor = 0.95f`（Phase 4 联动时由 Linking 层写入）
  - 实现 `IDamageable.TakeDamage(float raw)`：`实际伤害 = raw * (1 - DamageReductionFactor)`，扣减 CurrentHp
  - 死亡：CurrentHp ≤ 0 → 死亡效果占位 → `SendCommand<EnemyDiedCommand>(gameObject)` → 销毁
- [ ] `EnemyDiedCommand`（骨架，`OnExecute` 通知 WaveSystem，B3c 填充）

### 依赖
B0（QF骨架）、B2a（IDamageable 接口已定义）

---

## B3b — `unity-enemy-ai-bt` ⬜ 待开始（依赖 B3a）

**职责**：敌人 Behavior Tree AI——Idle / Detect / Chase / Attack 最小状态机。

### 范围
- [ ] BT 行为树：
  - `Idle`：站立
  - `Detect`：OverlapSphere 检测玩家（半径占位常量）
  - `Chase`：直线追踪（无 NavMesh，Phase 6 可升级）
  - `Attack`：近战范围内定时攻击，通过 `SendCommand<DamagePlayerCommand>(damage)` 伤害玩家
- [ ] `DamagePlayerCommand.OnExecute`：调用 `PlayerSystem.TakeDamage(damage)`

### 依赖
B3a

---

## B3c — `unity-wave-manager` ⬜ 待开始（依赖 B3b）

**职责**：波次管理器——生成敌人、监听全灭、推进波次，接入 WaveSystem/WaveModel。

### 范围
- [ ] `WaveConfig` ScriptableObject：每波敌人数量 / 种类（Prefab 引用）/ SpawnPoints
- [ ] `WaveSystem.OnStartWave()`：按 WaveConfig 生成敌人，WaveModel.AliveCount = 敌人数
- [ ] `EnemyDiedCommand.OnExecute`（完善）：`WaveModel.AliveCount--`，归零触发 `OnWaveClear`
- [ ] `WaveSystem` 监听 `OnWaveClear`：延迟 3 秒 → `WaveModel.WaveNumber++` → 下一波
- [ ] `WaveStarterController`（实现 `IController`）：游戏开始时 `SendCommand<StartWaveCommand>()` 触发首波
- [ ] Play Mode 验证：消灭本波所有敌人 → 延迟 → 下一波生成，Console 输出波次推进日志

### 依赖
B3b

---

## B4 — `unity-hack-trigger` ⬜ 待开始（依赖 B3a、B0）

**职责**：骧入触发检测组件——Raycast 检测有效目标，打 Log 占位，为 Phase 4 接入 HackSession 预留接口。

### 范围
- [ ] `HackTrigger`（实现 `IController`）：
  - 监听骧入键（默认 F / 手柄 LB）
  - 0.2~0.3 秒防误触冷却
  - 无骧入进行中：Raycast 检测最近 Enemy（Layer `Enemy`）
    - 命中 → `Debug.Log($"[HackTrigger] 命中 {target.name}，等待 Phase 4 接入")` + `SendCommand<StartHackCommand>(target)`
    - 未命中 → 无反应
  - 已有骧入进行中：键入无反应
- [ ] `StartHackCommand.OnExecute`（完善骨架）：打 Log，HackSystem 空实现接收
- [ ] Play Mode 验证：对 Capsule 敌人按 F，Console 输出命中日志；快按防误触有效

### 依赖
B3a（Enemy Layer 已设置）、B0（StartHackCommand 已声明）

---

## 整体推进节奏

```
Phase 1（A端，Core层）:  A1 → A2 → A3 → A4 → A5 → A6 → A7
Phase 2（B端，Unity）:   B0 → B1a → B1b.1 → B1b.2 → B1b.3 → B1c.1 → B1c.2 → B2a → B2b
                         B0 → B3a → B3b → B3c
                         B0 → B4（依赖 B3a）
```

每个 change 流程：

```
/opsx:new <change-name>      # 生成 proposal + design + tasks + delta specs
→ 实现 + 测试通过
/opsx:apply                  # 标记 tasks 完成
/opsx:verify                 # 验证实现匹配 spec
/opsx:archive                # 归档, 同步 delta → 主 specs, 更新本文档勾选
```

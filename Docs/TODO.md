# TODO.md — Phase 1 Change 拆分清单

> 本文档将 `DEVELOPMENT_PLAN.md` 中 **Phase 1 Core 层开发**任务清单，按"单测可独立验证 + 依赖逻辑链"拆分为 7 个 OpenSpec change。
> 每个 change 完成后通过 `/opsx:archive` 归档，将 delta spec 同步到主 specs，再开始下一个。

---

## 拆分原则

- 单 change 改动控制在 200~600 行级（≈ 1 人天）
- 每个 change 都能独立 `dotnet build` + `dotnet test` 通过
- 严格线性依赖，前置 change 未归档前不开新 change
- 每个 change 的测试覆盖必须落到归档 spec 的 Scenario

---

## 依赖图

```
        Change 1 ─→ Change 2 ─→ Change 3 ─→ Change 4 ─→ Change 5 ─→ Change 6 ─→ Change 7
        types       validator    deck-gen    skeleton     rewardpot   result-end   demo
```

---

## Change 1 — `cardchain-types` ✅ 已归档 (2026-05-26)

**职责**：所有 Core 层数据类型与枚举的纯定义，不含逻辑。
**归档位置**：`openspec/changes/archive/2026-05-26-cardchain-types/`
**主 spec**：`openspec/specs/cardchain-types/spec.md`

### 范围
- [x] `enum CardType { Number, Reverse, Wild, Empty }`
- [x] `enum CardColor { Red, Blue, Green, Yellow }`
- [x] `enum ChainDirection { Ascending, Descending }`
- [x] `enum EndReason { TimeUp, WrongCard, Surrender }`
- [x] `enum ComboType { None, SameColorTwice, SameDirectionTwice }`（预留，无逻辑）
- [x] `class CardData`：`Type` / `Color?` / `Number?` 三字段 + `static readonly CardData Empty`
- [x] xUnit：枚举值齐全、`CardData.Empty.Type == Empty`、不同 Type 的 nullable 字段约定（17 个测试用例全部通过）

### 验收数据
- `dotnet build` → 0 警告、0 错误
- `dotnet test` → 17 通过、0 失败、0 跳过
- `dotnet run` → 占位文本正常输出
- `grep UnityEngine` → 0 匹配
- csproj 依赖 → 仍为零

---

## Change 2 — `cardchain-validator` ✅ 已实施 (2026-05-26，待归档)

**职责**：纯函数层的接龙合法性判定与状态更新（`IsValidNext` / `ApplyPrev`）。

### 范围
- [x] `internal class SessionState`：`LastColor` / `LastNumber` / `Direction` 三字段
- [x] `internal static class CardChainRules`（或同等命名）：
  - [x] `IsValidNext(CardData next, SessionState state) → bool`
  - [x] `ApplyPrev(CardData prev, SessionState state)`（in-place 修改 state）
- [x] xUnit 覆盖：
  - [x] `lastColor == null` 时任意数字合法（开局 / 王牌后等价 Wild 后状态）
  - [x] 同色任意数字合法（含异色边界覆盖）
  - [x] 异色升序：`N' == lastNumber + 1` 才合法（严格 ±1）
  - [x] 异色降序：`N' == lastNumber - 1` 才合法（严格 ±1）
  - [x] 异色同数字非法（旧规则废除验证）
  - [x] 反转牌后 `lastColor != null + lastNumber == null` 时异色数字全部非法
  - [x] 边界 `(C, 9, Asc)` / `(C, 0, Desc)` 异色无解（N'==10 / N'==-1 不存在）
  - [x] 王牌作为 next 永远合法（任何 state）
  - [x] 反转牌：同色合法、异色非法、`lastColor == null` 时任意反转合法
  - [x] **连续两张同色 Reverse 合法**（每色 2 张 Reverse 设计）
  - [x] `ApplyPrev` 数字牌：更新 lastColor/lastNumber，不切方向
  - [x] `ApplyPrev` 反转牌：更新 lastColor、清 lastNumber、翻转方向
  - [x] `ApplyPrev` 王牌：清 lastColor 和 lastNumber，不切方向
  - [x] 开局起手：state=(null, null, Ascending)，任意牌合法
  - [x] GAME_DESIGN 3.5.3 完整序列重放（覆盖开局/同色覆盖/严格±1试探/反转后异色非法/王牌后重置/连续两张同色Reverse）

### 验收数据
- `dotnet build CardChainCore.sln` → 0 警告、0 错误
- `dotnet test CardChainCore.sln` → 109 通过、0 失败、0 跳过（含 Change 1 的 17 个 + Change 2 新增 92 个）

### 依赖
Change 1（CardData/枚举）

---

## Change 3 — `cardchain-deck-generator` ✅ 已实施 (2026-05-27，待归档)

**职责**：选项生成器（按 `INTERFACE.md` 第五节"发牌算法"Option F 版）+ 难度参数 config。

### 范围
- [x] `class HackDifficultyConfig`：`OptionCount` / `TargetChainCount` / `TotalTime` / `SolvableRate` / `WildAppearRate` 五字段
- [x] `internal class OptionGenerator`（或同等命名）：
  - [x] `Generate(state, config, random) → (CardData[] options, bool isDeadlock)`
  - [x] deck 构成：40 Number + 8 Reverse = 48 张逻辑牌池，**王牌不进 deck**
  - [x] `SolvableRate` 决定是否抽 1 张合法牌（下界语义）
  - [x] `WildAppearRate` 独立判定塞王牌
  - [x] 剩余位填非法牌
  - [x] **合法位扩展守卫**：非法池规模 < 所需非法位数时，缺口转为合法位
  - [x] 选项内不重复（同轮）
  - [x] `Empty` 永不出现在选项中
  - [x] 反转牌不强塞，仅作为合法/非法牌候选自然出现
  - [x] **选项数组洗牌**（Fisher-Yates，与 GAME_DESIGN 3.5.4 对应）
- [x] 抽样池：48 张牌的逻辑代表（`OptionGenerator.Deck` 静态缓存，每色每数字 1 张 Number + 每色 2 张 Reverse）
- [x] 注入式随机源（`System.Random`）便于测试
- [x] xUnit 覆盖：
  - [x] 选项数量始终 = `OptionCount`
  - [x] 选项内不重复
  - [x] `Empty` 不出现
  - [x] `SolvableRate=1, WildAppearRate=0` 时永远有合法牌且无王牌
  - [x] `SolvableRate=0, WildAppearRate=0` 时一般 state 永远无合法牌（isDeadlock=true）
  - [x] **`(null, null, *)` 状态恒 isDeadlock=false**（合法位扩展守卫触发）
  - [x] **`(C, null, *)` 状态边界**——OptionCount=5 时 illegalPool=36 充裕，守卫不触发；OptionCount=20 时触发兜底
  - [x] `WildAppearRate=1` 时永远塞 1 张王牌
  - [x] 王牌不算入合法/非法牌池（多 seed 验证 deck 抽样不返回 Wild）
  - [x] 大样本统计：N=10000 跑 SolvableRate=0.5/0.7、WildRate=0.05，容差 ±3%
  - [x] 固定 seed 可重放
  - [x] 洗牌位置分布检测（弱断言）
  - [x] state 不被修改（纯函数）

### 验收数据
- `dotnet build` → 0 警告、0 错误
- `dotnet test` → **139 通过**、0 失败、0 跳过（109 旧 + 30 新）
- `grep UnityEngine` → 0 匹配
- `OptionGenerator.cs` 160 行，`HackDifficultyConfig.cs` 26 行，单文件 < 300 ✓

### 依赖
Change 1（CardData）+ Change 2（IsValidNext 用于"合法/非法"判定）

---

## Change 4 — `hacksession-skeleton`

**职责**：`HackSession` 骨架——构造/计时/事件订阅、单轮选牌循环（不含 maxPot/latch/overflow）。

### 范围
- [ ] `class HackSession`：
  - [ ] 构造 `HackSession(HackDifficultyConfig config)`
  - [ ] 公开属性：`IsActive` / `ChainCount` / `TimeRemaining` / `CurrentCard` / `CurrentOptions` / `CurrentDirection` / `BasePot`（已能算）
  - [ ] 8 个事件签名声明（OnNewRound 含 isDeadlock 参数）
  - [ ] `Start()`：初始化 state，CurrentCard = Empty，触发首轮 OnNewRound
  - [ ] `Tick(float deltaTime)`：减少 TimeRemaining
  - [ ] `SelectOption(int)`：合法 → ApplyPrev + chain++ + OnChainSuccess + 下一轮 OnNewRound；非法 → OnChainFailed
  - [ ] `Surrender()`：方法签名声明（实现可放 Change 6，本期空实现也可）
  - [ ] `TargetId`、`OnComboTriggered` 占位（v1 不实现）
- [ ] xUnit 覆盖：
  - [ ] Start 触发首轮 OnNewRound，CurrentCard.Type=Empty
  - [ ] Start 后 IsActive=true
  - [ ] Tick 正确减少 TimeRemaining
  - [ ] SelectOption 合法：chain+1、OnChainSuccess 触发、下一轮 OnNewRound 触发
  - [ ] SelectOption 非法：OnChainFailed 触发、IsActive 处理（细节见 Change 6）
  - [ ] SelectOption 越界索引抛异常或忽略（约定其一）

### 不含
- maxPot / latch / overflow 逻辑（Change 5）
- HackResult 与 OnSessionEnd（Change 6）
- TimeUp 触发结束（Change 6）

### 依赖
Change 3（OptionGenerator 用于生成每轮选项）

---

## Change 5 — `hacksession-rewardpot`

**职责**：双层奖励池 + 满档单向 latch + 溢出计数 + 方向切换事件。

### 范围
- [ ] `MaxPot` 属性 + `IsMaxLatched` 属性 + `OverflowCount` 属性
- [ ] 反转牌 prev 时，`IsMaxLatched=false` 则 `MaxPot += 1`
- [ ] 王牌 prev 时，`IsMaxLatched=false` 则 `MaxPot += 4`
- [ ] 接牌后判定顺序：
  1. `chain += 1`
  2. 牌效结算（含 maxPot 增长）
  3. 满档判定：未 latch 且 `chain >= MaxPot` → 设 latch=true、MaxPot 冻结、触发 `OnMaxReached(MaxPot_frozen)`
  4. 溢出判定：已 latch 且 `chain > MaxPot_frozen` → `OverflowCount++`、触发 `OnOverflow(OverflowCount)`
- [ ] `OnDirectionChanged` 在反转牌 prev 后触发，参数为新方向
- [ ] xUnit 覆盖：
  - [ ] 反转牌使 MaxPot+1（满档前）
  - [ ] 王牌使 MaxPot+4（满档前）
  - [ ] 满档后反转/王牌不再增加 MaxPot（latch 冻结）
  - [ ] 满档 latch 单向：进入后即便 chain<MaxPot 仍保持 latch
  - [ ] OnMaxReached 只触发一次
  - [ ] OnOverflow 在每次满档后接合法牌时累加触发
  - [ ] OnDirectionChanged 仅反转牌触发，王牌不触发
  - [ ] 接牌顺序验证："差一张满档时来王牌" → 先 maxPot+=4 后判定（不立即满档）
  - [ ] 边界：basePot=10, 用 1 王牌 + 1 反转 → MaxPot=15，满档时 chain=15

### 依赖
Change 4（HackSession 骨架）

---

## Change 6 — `hacksession-result-and-end`

**职责**：会话结束的所有路径 + `HackResult` 计算 + `Surrender()` 完整实现。

### 范围
- [ ] `class HackResult`：`ChainCount` / `BasePot` / `MaxPot` / `OverflowCount` / `IsMaxReached` / `Reason` 字段
- [ ] `DamageReductionFactor` 计算属性（`chain / basePot`，无 clamp，basePot=0 兜底返回 0）
- [ ] `Surrender()` 完整实现：任何状态下调用合法，结束会话，触发 `OnSessionEnd(reason=Surrender)`
- [ ] `Tick` 中 `TimeRemaining <= 0` → 触发 `OnTimeUp` + `OnSessionEnd(reason=TimeUp)`
- [ ] `SelectOption` 非法路径完善：触发 `OnChainFailed` + `OnSessionEnd(reason=WrongCard)`
- [ ] `IsActive` 在结束后设为 false；之后任何方法调用应忽略或抛异常（约定其一并测试）
- [ ] xUnit 覆盖：
  - [ ] 时间到 → OnTimeUp + OnSessionEnd(TimeUp)，不扣血标记
  - [ ] 接错牌 → OnChainFailed + OnSessionEnd(WrongCard)
  - [ ] Surrender 任意状态合法（含未 Start、Start 后未 SelectOption、满档后等）
  - [ ] Surrender 触发 OnSessionEnd(Surrender)
  - [ ] HackResult.DamageReductionFactor：`chain=0 → 0.0`、`chain=basePot → 1.0`、`chain>basePot → >1.0`、`basePot=0 → 0.0`
  - [ ] HackResult 携带正确的 BasePot/MaxPot/IsMaxReached 快照
  - [ ] OnSessionEnd 后 IsActive=false，再调 Tick/SelectOption/Surrender 安全
  - [ ] 同一会话只触发一次 OnSessionEnd

### 依赖
Change 5（MaxPot/Latch 状态需被 HackResult 读取）

---

## Change 7 — `cardchain-console-demo`

**职责**：`Unomata.Core.Console` 主程序，跑通完整骇入流程并输出日志。

### 范围
- [ ] `Program.cs`：构造 config（固定参数 / 命令行参数二选一）
- [ ] `class FakePlayer`（演示用 AI 决策）：
  - [ ] 优先选第一张合法选项
  - [ ] 死局立即调 `Surrender()`
  - [ ] 满档后继续接（验证溢出充能）
- [ ] 主循环：
  - [ ] 订阅所有 8 个事件并打印日志
  - [ ] Tick 循环（固定 dt 模拟时间流逝，或按 FakePlayer 决策驱动）
  - [ ] 退出条件：`OnSessionEnd` 触发后退出
- [ ] 日志格式参照 `DEVELOPMENT_PLAN.md` Phase 1 验收示例
- [ ] 至少跑通三种结束路径的演示模式（环境变量 / 参数切换）：
  - [ ] 自然超时（TimeUp）
  - [ ] 死局突破（Surrender）
  - [ ] 接错牌（WrongCard，FakePlayer 故意选非法牌）

### 验收
- [ ] `dotnet run --project console/Unomata.Core.Console` 能输出与 DEVELOPMENT_PLAN 示例一致风格的日志
- [ ] 至少一次跑出 `Deadlock=true` + `[FakePlayer] 立即 Surrender (死局突破)`

### 依赖
Change 6（HackSession 已完整）

---

## 整体推进节奏

```
Phase 1（A端，Core层）:  Change 1 → Change 2 → Change 3 → Change 4 → Change 5 → Change 6 → Change 7
Phase 2（B端，Unity）:   Change B0 → B1 → B2 → B3a → B3b → B3c → B4     （可与 Phase 1 并行）
```

每个 change 流程：

```
/opsx:new <change-name>      # 生成 proposal + design + tasks + delta specs
→ 实现 + 测试通过
/opsx:apply                  # 标记 tasks 完成
/opsx:verify                 # 验证实现匹配 spec
/opsx:archive                # 归档, 同步 delta → 主 specs, 更新本文档勾选
```

完成后逐项把上方 `[ ]` 改为 `[x]` 并标注归档日期。

---

# Phase 2 — Unity TPS 基础：Change 拆分清单

> 本节对应 `DEVELOPMENT_PLAN.md` Phase 2（2.0~2.4），按 B 端 Unity 开发拆分为 9 个 change。
> QF 骨架先行（B0），后续各 change 在骨架上追加，禁止绕过 QF。

## 依赖图

```
B0(QF骨架)
  ├──→ B1a(模型换装)
  │      └──→ B1b(动画层+相机)
  │                └──→ B2a(射击Raycast)
  │                          └──→ B2b(命中特效)
  ├──→ B3a(敌人骨架)
  │      └──→ B3b(敌人AI BT)
  │                └──→ B3c(波次管理器)
  └──→ B4(骧入触发)       ← 依赖 B3a（需要可检测的 Enemy）
```

---

## B0 — `unity-qf-skeleton` ✅ 已归档 (2026-05-26)

**职责**：完善 GameApp Architecture 骨架——注册 Phase 2 全部 System/Model，声明 Command 骨架，为后续所有 change 建立 QF 接入基础。
**归档位置**：`openspec/changes/archive/2026-05-26-unity-qf-skeleton/`
**主 specs**：`openspec/specs/player-system/spec.md` / `openspec/specs/wave-system-scaffold/spec.md`（新增）/ `openspec/specs/qframework-integration/spec.md`（更新）

### 已确认技术决策
| 决策项 | 结论 |
|--------|------|
| `PlayerModel.HP` 类型 | `BindableProperty<float>`（方便后续伤害减免计算） |
| `WaveModel` 敌人列表 | B0 只做 `AliveCount`（`BindableProperty<int>`），等 B3c 再扩展 |
| Commands 目录 | 统一放 `Assets/_Project/Scripts/Gameplay/Commands/` |

### 新建文件
- [x] `Player/PlayerModel.cs`：`BindableProperty<float> HP`、`BindableProperty<float> MaxHp`
- [x] `Player/PlayerSystem.cs`：`OnInit` 取 PlayerModel 引用，`TakeDamage(float raw)` 方法（扣 HP，防止低于零）
- [x] `Wave/WaveModel.cs`：`BindableProperty<int> WaveNumber`、`BindableProperty<int> AliveCount`
- [x] `Wave/WaveSystem.cs`：`OnInit` 取 WaveModel 引用，`OnStartWave()` / `OnEnemyKilled()` 骨架（空实现，B3c 填充）
- [x] `Commands/StartHackCommand.cs`（空 `OnExecute`）
- [x] `Commands/SelectCardCommand.cs`（空 `OnExecute`）
- [x] `Commands/HealCommand.cs`（空 `OnExecute`）
- [x] `Commands/DamagePlayerCommand.cs`（空 `OnExecute`，B3b 填充 `PlayerSystem.TakeDamage`）

### 修改文件
- [x] `GameApp.cs`：填充 `Init()`，严格按顺序：`RegisterModel<PlayerModel>` → `RegisterModel<WaveModel>` → `RegisterSystem<PlayerSystem>` → `RegisterSystem<WaveSystem>`
- [x] `Tests/QFrameworkValidator.cs`：扩展 Phase2 验证链路——`PlayerModel.HP` 写读、`PlayerSystem.TakeDamage` 触发 HP 变化、`WaveSystem 可 GetModel<WaveModel>()`

### 验收
- Unity Console 零红色错误 ✅
- Play Mode 输出 `[QF验证通过] Phase2 骨架 System/Model 链路正常` ✅
- `GameApp.cs` 注册结构符合 ARCHITECTURE.md 分层图 ✅

### 依赖
Phase 0 QF 链路验证（已归档 ✅）

---

## B1a — `unity-character-model-swap` ✅ 已归档 (2026-05-26)

**职责**：将 StarterAssets PlayerArmature 的视觉模型替换为 CombatGirls RifleGirl，保留 MagicaCloth2 布料物理。
**归档位置**：`openspec/changes/archive/2026-05-26-unity-character-model-swap/`
**主 spec**：`openspec/specs/character-controller/spec.md`（新增6条 Requirement）

### 关键实现细节
- [x] `PlayerArmature/Geometry`（原 StarterAssets 视觉子对象）`SetActive(false)` 禁用保留
- [x] `Rifle_Full_Body.prefab` 嵌入为子对象，local pos/rot = 0；内部 Animator 已禁用（防双冲突）
- [x] `PlayerArmature` 根 `Animator` Avatar 切换为 `Humanoid_FAvatar`（来自 `Humanoid_F.fbx`，`CopyFromOther` 设置）
- [x] 删除多余 `Main Camera`，场景只保留唯一 `MainCamera`（含 CinemachineBrain）
- [x] Play Mode 验证通过：零红色错误、材质无紫色、MagicaCloth2 布料激活

### 依赖
B0（QF骨架）、Phase 0 资产整理（已归档 ✅）

---

## B1b — `unity-character-aim-layer` ⬜ 待开始（依赖 B1a）

**职责**：添加上半身持枪动画层，实现瞄准模式下动画层切换 + Cinemachine 双相机 Priority 切换。

### 范围

**Animator Controller（复制为项目自有版本，不直接改 StarterAssets 原文件）：**
- [ ] 新增 Layer `UpperBodyAim`：Override 模式，默认 Weight = 0，绑定上半身 Avatar Mask
- [ ] `UpperBodyAim` 状态机：`AimIdle`（`R_AimIdle`）+ 移动 Blend Tree（`R_AimWalk_F/B/L/R`），由 `IsAiming` bool 参数控制 Weight
- [ ] 瞄准时 Layer Weight 渐变到 1，退出瞄准渐变回 0（`Animator.SetLayerWeight`）

**Cinemachine 双相机：**
- [ ] `PlayerFollowCamera`（已有，Priority = 10）保持不变
- [ ] 新建 `PlayerAimCamera`（VirtualCamera，默认 Priority = 0）：
  - Body：Framing Transposer，Camera Offset X ≈ 0.5（右肩）
  - FOV = 40（Phase 5 平衡时调整）
- [ ] 瞄准输入（RMB / 手柄 LT）：`PlayerAimCamera.Priority = 15`；退出瞄准：`Priority = 0`

**输入处理（接入 QF，使用 PlayerController 组件）：**
- [ ] `PlayerController`（实现 `IController`）：监听 Aim 输入，`SendCommand<SetAimStateCommand>(bool)` 或通过 event 通知 PlayerSystem

- [ ] Play Mode 验证：普通移动与瞄准移动上半身动画正确叠加，双相机切换平滑无抖动

### 依赖
B1a（RifleGirl 已嵌入，Humanoid Avatar 已切换）

---

## B2a — `unity-shooting-raycast` ⬜ 待开始（依赖 B1b）

**职责**：射击输入 + Raycast 命中检测 + 调用敌人受击接口。

### 范围
- [ ] `ShootingController`（实现 `IController`）：监听射击输入（LMB / RT）
  - 瞄准模式：连发（rate 占位常量）；非瞄准（腰射）：单发 + 散布偏移
  - 从相机中心发射 Raycast（`Physics.Raycast`），检测 Layer `Enemy`
  - 命中 → 获取 `IDamageable` 接口，调用 `TakeDamage(float baseDamage)` 占位
- [ ] 定义 `IDamageable` 接口（`Assets/_Project/Scripts/Gameplay/`）：`void TakeDamage(float rawDamage)`
- [ ] Layer 设置：在 Unity Tags & Layers 中添加 `Enemy` Layer

### 依赖
B1b（相机已建立，瞄准状态可查询）

---

## B2b — `unity-shooting-vfx` ⬜ 待开始（依赖 B2a）

**职责**：命中特效占位（弹孔、受击粒子、死亡效果、准星扩散）。

### 范围
- [ ] 命中普通表面：DecalProjector 弹孔占位（或简单 ParticleSystem）
- [ ] 命中敌人：受击特效占位（红色粒子闪烁）
- [ ] 击杀：死亡临时效果占位（Dissolve / 淡出，Phase 6 替换）
- [ ] HUD：准星动态扩散（射击后短暂扩大，`CrosshairController`，接入 QF UI 事件）

### 依赖
B2a（射击逻辑已通）

---

## B3a — `unity-enemy-scaffold` ⬜ 待开始（依赖 B0）

**职责**：敌人 Prefab 骨架，暴露 HP + DamageReductionFactor 属性，实现受击与死亡逻辑。

### 范围
- [ ] Enemy Prefab（Capsule 占位；敌人模型选定后替换，可独立 patch change）
  - `Collider` 设置为 Layer `Enemy`
- [ ] `EnemyController`（实现 `IController`）：
  - `float MaxHp` / `float CurrentHp`
  - `float DamageReductionFactor = 0.95f`（Phase 4 联动时由 Linking 层写入）
  - 实现 `IDamageable.TakeDamage(float raw)`：`实际伤害 = raw * (1 - DamageReductionFactor)`，扣减 CurrentHp
  - 死亡：CurrentHp ≤ 0 → 播放死亡效果占位 → `this.SendCommand<EnemyDiedCommand>(gameObject)` → 销毁
- [ ] `EnemyDiedCommand`（骨架，`OnExecute` 通知 WaveSystem，B3c 填充）

### 依赖
B0（QF骨架，WaveSystem 已声明）、B2a（IDamageable 接口已定义）

---

## B3b — `unity-enemy-ai-bt` ⬜ 待开始（依赖 B3a）

**职责**：敌人 Behavior Tree AI——Idle / Detect / Chase / Attack 最小状态机。

### 范围
- [ ] 确认 BT 包方案，添加到 `Packages/manifest.json`，更新 `DEPENDENCIES.md`
- [ ] BT 行为树：
  - `Idle`：站立
  - `Detect`：OverlapSphere 检测玩家（半径占位常量）
  - `Chase`：直线追踪（无 NavMesh，Phase 6 可升级）
  - `Attack`：近战范围内定时攻击，通过 `this.SendCommand<DamagePlayerCommand>(damage)` 对玩家造成伤害
- [ ] `DamagePlayerCommand.OnExecute`：调用 `PlayerSystem.TakeDamage(damage)`

### 依赖
B3a（EnemyController 已有 HP/DRF 骨架）

---

## B3c — `unity-wave-manager` ⬜ 待开始（依赖 B3b）

**职责**：波次管理器——生成敌人、监听全灭、推进波次，接入 WaveSystem/WaveModel。

### 范围
- [ ] `WaveConfig` ScriptableObject：每波敌人数量 / 种类（Prefab 引用）/ SpawnPoints
- [ ] `WaveSystem.OnStartWave()`：按 WaveConfig 生成敌人 Prefab，WaveModel.AliveCount = 敌人数
- [ ] `EnemyDiedCommand.OnExecute`（完善）：WaveModel.AliveCount--，归零时触发 `OnWaveClear` 事件
- [ ] `WaveSystem` 监听 `OnWaveClear`：延迟 3 秒 → WaveModel.WaveNumber++ → 触发下一波
- [ ] `WaveStarterController`（实现 `IController`）：挂场景，游戏开始时 `SendCommand<StartWaveCommand>()` 触发首波
- [ ] Play Mode 验证：消灭本波所有敌人 → 延迟 → 下一波生成，Console 输出波次推进日志

### 依赖
B3b（敌人会死亡并发出 EnemyDiedCommand）

---

## B4 — `unity-hack-trigger` ⬜ 待开始（依赖 B3a，B0）

**职责**：骧入触发检测组件——Raycast 检测有效目标，打 Log 占位，为 Phase 4 接入 HackSession 预留接口。

### 范围
- [ ] `HackTrigger`（实现 `IController`）：
  - 监听骧入键（默认 F / 手柄 LB）
  - 0.2~0.3 秒防误触冷却
  - 无骧入进行中：Raycast 检测最近 Enemy（Layer `Enemy`）
    - 命中 → `Debug.Log($"[HackTrigger] 命中 {target.name}，等待 Phase 4 接入")` + `SendCommand<StartHackCommand>(target)` 骨架调用
    - 未命中 → 无反应
  - 已有骧入进行中（Phase 4 后由 HackSystem 维护状态）：键入无反应
- [ ] `StartHackCommand.OnExecute`（完善骨架）：打 Log，HackSystem 空实现接收
- [ ] Play Mode 验证：对 Capsule 敌人按 F，Console 输出命中日志；连续快按 F 防误触有效

### 依赖
B3a（Enemy Layer 已设置，敌人 Prefab 存在）、B0（StartHackCommand 已声明）

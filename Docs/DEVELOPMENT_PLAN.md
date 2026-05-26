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

---

## Phase 1 — Core 层开发（A独立）

**交付标准：控制台项目可完整运行一次骇入流程，所有事件正确触发**

### 任务清单
- [ ] `CardData` + `CardType` / `CardColor` / `ChainDirection` 枚举：纯数据，含 `CardData.Empty` 静态实例
- [ ] `HackDifficultyConfig`：难度参数数据类（OptionCount / TargetChainCount / TotalTime / SolvableRate / WildAppearRate）
- [ ] 选项生成器（按 `INTERFACE.md` 第五节"发牌算法"实现）：
  - [ ] `SolvableRate` 决定本轮是否抽 1 张合法牌（轮级有解概率）
  - [ ] `WildAppearRate` 独立判定是否塞 1 张王牌
  - [ ] 剩余位填非法牌；选项内不重复，跨轮可重
  - [ ] 反转牌仅作为合法牌候选自然出现，不强塞
  - [ ] `Empty` 永不出现在选项中
  - [ ] 计算并返回 `isDeadlock` 标志（选项中无任一合法牌）
- [ ] `HackSession` 内部状态机：
  - [ ] `SessionState`：lastColor / lastNumber / direction
  - [ ] `IsValidNext()` 严格升降序判定（同色 OR 方向匹配）
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
- [ ] `EndReason`：`TimeUp / WrongCard / Surrender`
- [ ] `ComboType` 枚举（预留：None / SameColorTwice / SameDirectionTwice，不实现逻辑）
- [ ] xUnit 测试覆盖关键判定：升降序边界、反转切方向、王牌穿透、满档 latch、溢出计数、死局判定、Surrender 状态机
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

### 任务清单
- [ ] 人物控制器（基于 Starter Assets PlayerArmature，**方案B 补丁**：见下方详细说明）
- [ ] 相机系统（普通跟随 + 瞄准状态切换，Cinemachine）
- [ ] 射击系统（Raycast 命中检测 + 命中特效占位）
- [ ] 敌人基础（血量 + 伤害减免属性 + 简单 AI 状态机）
- [ ] 波次管理器（生成敌人 + 监听全灭 + 推进波次）
- [ ] 骇入触发检测（Raycast 检测有效目标，暂时只打 Log）
- [ ] QFramework Architecture 搭建（GameApp 入口，skeleton 已在 Assets/_Project/Scripts/Gameplay/GameApp.cs）

### 方案 B 补丁清单（角色模型对接）
> Phase 0 验证结论：CombatGirls + StarterAssets 两者均 Humanoid Rig，基础兼容，需以下改动：

1. **替换视觉模型**：将 `PlayerArmature` 中 `Geometry/Armature_Mesh` 替换为 `Rifle_Full_Body.FBX`，并在 Animator 组件上切换到 RifleGirl 的 Avatar
2. **添加上半身动画层**：在 `StarterAssetsThirdPerson.controller` 中添加新 Layer，绑定 Avatar Mask（上半身），将持枪动画（`R_AimIdle`、`R_AimWalk_F` 等）映射到该层
3. **修复双 AudioListener**：SampleScene 中删除多余的 Main Camera（或其 AudioListener），保持场景只有一个

### 注意
- 敌人需要暴露 `float DamageReductionFactor` 属性，Phase 4 联动时写入
- 骇入触发逻辑只做检测，**不接 HackSession**，等 Phase 4

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

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
- [ ] 搭建 `CardChainCore` 控制台项目（.NET 8，用于开发验收）

### B（队友）
- [ ] Unity Hub 新建 2022.3 LTS + URP 项目，放入现有仓库 `Assets/` 目录
- [ ] Package Manager 安装 QFramework
- [ ] 导入 Starter Assets Third Person Controller
- [ ] **验证 CombatGirls 动画能否 Retarget 到 Starter Assets 骨骼**（最高优先级，早发现早换方案）
- [ ] 提交初始 Unity 工程

---

## Phase 1 — Core 层开发（A独立）

**交付标准：控制台项目可完整运行一次骇入流程，所有事件正确触发**

### 任务清单
- [ ] `CardData`：牌的数据结构 + `CanFollow()` 匹配逻辑
- [ ] `CardDeck`：牌组生成，按 `ValidCardRatio` 控制合法牌比例
- [ ] `HackDifficultyConfig`：难度参数数据类
- [ ] `HackSession`：完整会话逻辑
  - [ ] 计时（`Tick` 驱动）
  - [ ] 选牌验证
  - [ ] 接龙计数 + 满档检测
  - [ ] 溢出计数
  - [ ] 所有事件触发
  - [ ] `HackResult` 生成
- [ ] `HackResult`：结算数据类 + `DamageReductionFactor` 计算
- [ ] `ComboType` 枚举（预留，不实现逻辑）
- [ ] 控制台测试程序：模拟完整骇入流程输出日志

### 验收方式
控制台输出示例：
```
[HackSession] Start | Target=8 Time=12.0s Options=3
[Round 1] Current: Red-5 | Options: Red-3 / Blue-5 / Green-7
[Input] Select index 1 (Blue-5) → Match: SameNumber ✓ Chain=1
[Round 2] Current: Blue-5 | Options: ...
...
[SessionEnd] Chain=8/8 Overflow=2 Factor=1.0 Reason=TimeUp
```

---

## Phase 2 — Unity TPS 基础（B独立）

**交付标准：角色可在场景中移动、瞄准、射击，敌人可被击中扣血，波次管理器可触发**

**可与 Phase 1 完全并行**

### 任务清单
- [ ] 人物控制器（基于 Starter Assets，替换 CombatGirls 模型）
- [ ] 相机系统（普通跟随 + 瞄准状态切换，Cinemachine）
- [ ] 射击系统（Raycast 命中检测 + 命中特效占位）
- [ ] 敌人基础（血量 + 伤害减免属性 + 简单 AI 状态机）
- [ ] 波次管理器（生成敌人 + 监听全灭 + 推进波次）
- [ ] 骇入触发检测（Raycast 检测有效目标，暂时只打 Log）
- [ ] QFramework Architecture 搭建（GameApp 入口）

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
- [ ] B：`HackTrigger` 组件接入 `HackSession`（创建、驱动、订阅事件）
- [ ] B：`Linking` 层——将 `OnSessionEnd` 的 `DamageReductionFactor` 写入目标敌人
- [ ] B：`OnChainFailed` → 扣玩家血量
- [ ] B：`OnOverflow` → 生命回复技能充能+1
- [ ] A+B：联调，验证所有事件通路正确

---

## Phase 5 — 难度曲线与数值调整（A+B）

**交付标准：10波以上游戏体验流畅，难度递进明显**

### 任务清单
- [ ] 确定骇入效果持续时间数值
- [ ] 确定接错牌扣血量
- [ ] 确定满档额外伤害加成数值
- [ ] 确定生命回复技能缓存上限
- [ ] 波次→难度参数的映射曲线调整
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

## ADDED Requirements

### Requirement: HackDifficultyConfig 难度参数数据类

系统 SHALL 提供 `HackDifficultyConfig` 公开数据类，承载单次骇入的难度参数。

字段定义：

| 字段 | 类型 | 含义 |
|------|------|------|
| `OptionCount` | `int` | 每轮选项数量（典型 3 或 5） |
| `TargetChainCount` | `int` | 目标接龙次数（basePot，影响伤害减免计算） |
| `TotalTime` | `float` | 总倒计时秒数 |
| `SolvableRate` | `float` | 本轮选项中至少存在 1 张合法牌的概率（下界语义，0~1） |
| `WildAppearRate` | `float` | 本轮塞 1 张王牌的独立概率（0~1） |

#### Scenario: 创建 config 并读取字段

- **WHEN** 调用 `new HackDifficultyConfig { OptionCount = 5, TargetChainCount = 8, TotalTime = 12f, SolvableRate = 0.7f, WildAppearRate = 0.05f }`
- **THEN** 所有字段读取值与构造时一致

#### Scenario: 公开度

- **WHEN** Unity 端代码引用 `HackDifficultyConfig`
- **THEN** 类型可见（`public`），可在程序集外构造

### Requirement: OptionGenerator 单轮选项生成

系统 SHALL 提供 `OptionGenerator.Generate(SessionState state, HackDifficultyConfig config, Random random)` 静态方法，返回 `(CardData[] options, bool isDeadlock)` 元组。

方法严格按 `INTERFACE.md` 第五节"发牌算法 Option F：合法位扩展守卫版"伪代码执行：

1. 构造 48 张逻辑 deck（40 Number + 8 Reverse，**Wild 不进 deck**）
2. 按 `IsValidNext(card, state)` 分入 `legalPool` / `illegalPool`
3. 用 `random` 判定 `isSolvable`（命中 `SolvableRate`）与 `hasWild`（命中 `WildAppearRate`）
4. 计算初始 `legalSlot = isSolvable ? 1 : 0`，`illegalSlot = OptionCount - hasWildSlot - legalSlot`
5. 触发合法位扩展守卫：若 `illegalSlot > illegalPool.Count`，差额转为合法位
6. 极端兜底：`legalSlot = min(legalSlot, legalPool.Count)`
7. 从两池无放回抽样填入选项数组（Wild 若命中也加入）
8. Fisher-Yates 洗牌选项数组
9. 计算 `isDeadlock = options.All(opt => !IsValidNext(opt, state))`

#### Scenario: 选项数量恒等

- **WHEN** 任意合法 state 与 config（`OptionCount ∈ {3, 4, 5}`）调用 Generate
- **THEN** 返回的 `options` 数组长度 == `config.OptionCount`（合法池极端枯竭情形除外，需另测）

#### Scenario: 选项内不重复

- **WHEN** 调用 Generate
- **THEN** `options` 数组任意两元素不相等（同色同号 Number 不会出现两次；Reverse 与 Wild 同理）

#### Scenario: Empty 永不出现

- **WHEN** 调用 Generate
- **THEN** `options` 中无 `CardType.Empty` 元素

#### Scenario: Wild 不进入 deck

- **WHEN** `WildAppearRate=0` 的多次调用
- **THEN** `options` 中永不出现 `CardType.Wild`（即 Wild 不会从 deck 抽样路径泄漏）

#### Scenario: SolvableRate=1 + WildAppearRate=0 一般 state

- **WHEN** state=`(Red, 5, Asc)` config.SolvableRate=1.0 WildAppearRate=0
- **THEN** `options` 中至少包含 1 张合法牌，`isDeadlock=false`，无 Wild

#### Scenario: SolvableRate=0 + WildAppearRate=0 一般 state

- **WHEN** state=`(Red, 5, Asc)`（一般中盘，非法池充裕）SolvableRate=0 WildAppearRate=0
- **THEN** `options` 全为非法牌，`isDeadlock=true`

#### Scenario: WildAppearRate=1 永远塞王牌

- **WHEN** WildAppearRate=1.0 的任意调用
- **THEN** `options` 中恰好 1 张 Wild，且 `isDeadlock=false`（Wild 永远合法）

### Requirement: 合法位扩展守卫触发

系统 SHALL 在非法池规模不足时，将差额选项位强制转为合法位，确保 `(null, null, *)` 等小池状态恒有解。

守卫逻辑：`if illegalSlot > illegalPool.Count: deficit = illegalSlot - illegalPool.Count; illegalSlot -= deficit; legalSlot += deficit`

#### Scenario: (null, null, *) 状态恒不死局

- **WHEN** state=`(null, null, Asc)`（开局或 Wild 后），任意 SolvableRate ∈ [0, 1]，WildAppearRate=0
- **THEN** `isDeadlock=false` 永远成立（legalPool=48，illegalPool=0，所有非法位被守卫转为合法位）

#### Scenario: (C, null, *) 反转后状态部分守卫触发

- **WHEN** state=`(Red, null, Desc)`（反转后），OptionCount=5，SolvableRate=0，WildAppearRate=0
- **THEN** illegalPool 仅 6 张异色 Reverse，5 个非法位中部分位被守卫转为合法位（实际 illegalSlot=5 但池只有 6，刚好够；OptionCount=7 时才需守卫——本场景作为边界对照）

#### Scenario: 反转后 OptionCount=5 SolvableRate=1

- **WHEN** state=`(Red, null, Desc)` OptionCount=5 SolvableRate=1.0 WildAppearRate=0
- **THEN** legalSlot=1 illegalSlot=4，illegalPool=6 充裕，标准算法正常返回，isDeadlock=false

#### Scenario: 一般中盘标准算法

- **WHEN** state=`(Red, 5, Asc)` OptionCount=3 SolvableRate=0.7 WildAppearRate=0.05
- **THEN** illegalPool ≈ 33 张充裕，守卫不触发，标准算法正常返回

### Requirement: 选项数组随机洗牌

系统 SHALL 在返回前对 `options` 数组使用同一注入 `Random` 进行 Fisher-Yates 洗牌，使合法/非法/王牌的位置不可预测。

#### Scenario: 固定 seed 可重放

- **WHEN** 两次调用 Generate，传入相同 state、config、`new Random(42)`
- **THEN** 返回的 `options` 数组逐元素相等，`isDeadlock` 相同

#### Scenario: 不同 seed 位置变化

- **WHEN** 同一 state + config，分别用 `new Random(1)` 与 `new Random(2)` 多次调用
- **THEN** 在保留合法/非法/Wild 数量分布的前提下，元素位置存在差异（覆盖大样本时位置分布近似均匀）

### Requirement: 大样本概率收敛

系统 SHALL 在固定 seed + N=10000 次采样下，使实际有解率（`!isDeadlock` 占比）收敛于配置的 `SolvableRate`，绝对误差 ≤ 3%。

适用范围：仅一般中盘 state（非法池充裕，守卫不触发）。

#### Scenario: SolvableRate=0.5 收敛

- **WHEN** state=`(Red, 5, Asc)` SolvableRate=0.5 WildAppearRate=0，跑 10000 次（seed=12345）
- **THEN** `isDeadlock=false` 占比 ∈ [0.47, 0.53]

#### Scenario: SolvableRate=0.7 收敛

- **WHEN** 同上 state，SolvableRate=0.7
- **THEN** `isDeadlock=false` 占比 ∈ [0.67, 0.73]

#### Scenario: WildAppearRate=0.05 收敛

- **WHEN** state=任意，WildAppearRate=0.05，SolvableRate=0，跑 10000 次
- **THEN** 含 Wild 的 options 占比 ∈ [0.02, 0.08]

### Requirement: deck 构成与 Wild 隔离

系统 SHALL 使用固定的 48 张逻辑 deck：4 颜色 × 10 数字 = 40 张 Number，每色 2 张 Reverse = 8 张 Reverse，Wild 不进 deck。

#### Scenario: deck 总规模

- **WHEN** state=`(null, null, Asc)`（任意牌合法）
- **THEN** legalPool 规模 == 48（所有 deck 牌都合法），illegalPool 规模 == 0

#### Scenario: 每个 (Color, Number) 组合唯一

- **WHEN** 检查 deck 内的 Number 牌
- **THEN** 任意 `(Color, Number)` 组合恰好出现 1 张（共 40 个唯一组合）

#### Scenario: 每色 2 张 Reverse

- **WHEN** 检查 deck 内的 Reverse 牌
- **THEN** 每个颜色恰好 2 张 Reverse（共 8 张）

### Requirement: 反转牌不强塞

系统 SHALL 不专门为反转牌保留选项位，反转牌仅作为合法/非法池的自然抽样结果出现。

#### Scenario: 反转牌出现概率受 SolvableRate 影响

- **WHEN** SolvableRate=0 一般中盘 state（反转牌大多在非法池，部分同色反转在合法池）
- **THEN** 选项中 Reverse 占比与 deck 中 Reverse 比例（8/48）+ pool 分布相符，不被强塞

### Requirement: 极端兜底——合法池不足

系统 SHALL 在 `legalSlot > legalPool.Count` 极端情形下不抛异常，使用 `legalSlot = min(legalSlot, legalPool.Count)` 兜底，返回选项数 `< OptionCount` 的数组（实际场景几乎不会触发）。

#### Scenario: 反转后状态合法池规模

- **WHEN** state=`(Red, null, Desc)`，遍历 deck 标记合法
- **THEN** legalPool 规模 == 12（10 张 Red Number + 2 张 Red Reverse；异色 Number 与异色 Reverse 全非法）

#### Scenario: 合法池规模为 0 + WildAppearRate=0

- **WHEN** 构造极端 config（如 OptionCount=20，反转后 state legalPool=12），SolvableRate=1 WildAppearRate=0
- **THEN** Generate 不抛异常，返回选项数组（长度 ≤ OptionCount）

### Requirement: 纯函数无副作用

系统 SHALL 使 `OptionGenerator.Generate` 不修改入参 state，不持有任何静态可变状态。

#### Scenario: state 不被修改

- **WHEN** 传入 state 调用 Generate
- **THEN** 调用前后 state 各字段值完全相同

### Requirement: 单元测试覆盖

系统 SHALL 提供 xUnit 测试覆盖以下核心场景：选项数量恒等、不重复、Empty 不出现、Wild 不进 deck、SolvableRate 边界（0/1）、WildAppearRate 边界（0/1）、`(null,null,*)` 守卫、`(C,null,*)` 守卫、一般中盘、固定 seed 重放、大样本概率收敛、纯函数无副作用、极端合法池兜底。

#### Scenario: 测试套件全部通过

- **WHEN** 执行 `dotnet test CardChainCore.sln`
- **THEN** Change 1（17）+ Change 2（92）+ 本 change 测试全部通过，0 失败 0 跳过

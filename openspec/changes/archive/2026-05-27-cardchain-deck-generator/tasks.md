## 1. HackDifficultyConfig 数据类

- [x] 1.1 创建 `CardChainCore/src/Unomata.Core/HackDifficultyConfig.cs`，定义 `public class HackDifficultyConfig`，五字段（OptionCount / TargetChainCount / TotalTime / SolvableRate / WildAppearRate），public 可读写，零方法
- [x] 1.2 添加 XML doc 注释说明每字段含义与典型范围
- [x] 1.3 `dotnet build CardChainCore.sln` 通过，零警告零错误

## 2. OptionGenerator 实现

- [x] 2.1 创建 `CardChainCore/src/Unomata.Core/OptionGenerator.cs`，定义 `internal static class OptionGenerator`
- [x] 2.2 实现 `internal static IReadOnlyList<CardData> BuildDeck()` 工具：返回固定 48 张牌（4 颜色 × 10 数字 + 每色 2 张 Reverse），只构造一次缓存为 `static readonly`
- [x] 2.3 实现 `internal static (CardData[] options, bool isDeadlock) Generate(SessionState state, HackDifficultyConfig config, Random random)` 主方法：
  - 2.3.1 遍历 deck，按 `CardChainRules.IsValidNext(card, state)` 分入 `legalPool` / `illegalPool` 两个 List
  - 2.3.2 用 `random.NextDouble() < config.SolvableRate` 判定 `isSolvable`
  - 2.3.3 用 `random.NextDouble() < config.WildAppearRate` 判定 `hasWild`
  - 2.3.4 计算初始 `legalSlot = isSolvable ? 1 : 0`，`hasWildSlot = hasWild ? 1 : 0`，`illegalSlot = OptionCount - hasWildSlot - legalSlot`
  - 2.3.5 守卫：`if illegalSlot > illegalPool.Count`，差额 `deficit` 从 illegalSlot 转入 legalSlot
  - 2.3.6 兜底：`legalSlot = Math.Min(legalSlot, legalPool.Count)`
  - 2.3.7 实现 `SampleWithoutReplacement(List<CardData> pool, int k, Random random)` 工具：Fisher-Yates 部分洗牌取前 k 张（不修改原 pool 或拷贝后修改）
  - 2.3.8 填充 options：先抽 legalSlot 张合法，再抽 illegalSlot 张非法，最后追加 Wild（若 hasWild）
  - 2.3.9 对 options 数组执行 Fisher-Yates 全洗牌（同一 random）
  - 2.3.10 计算 `isDeadlock = options.All(opt => !CardChainRules.IsValidNext(opt, state))`
  - 2.3.11 返回 `(options, isDeadlock)`
- [x] 2.4 添加 XML doc 注释，引用 INTERFACE.md 第五节

## 3. xUnit 测试 — 基本契约

- [x] 3.1 创建 `CardChainCore/tests/Unomata.Core.Tests/OptionGeneratorTests.cs`
- [x] 3.2 测试：选项数量 == OptionCount（OptionCount ∈ {3, 4, 5}，多 state 多 config 组合）
- [x] 3.3 测试：选项内不重复（迭代多 seed 验证）
- [x] 3.4 测试：Empty 永不出现在 options
- [x] 3.5 测试：Wild 不进入 deck（WildAppearRate=0 时 options 中无 Wild，多 seed 验证）
- [x] 3.6 测试：state 不被 Generate 修改（前后字段比对）
- [x] 3.7 测试：deck 总规模——`(null,null,Asc)` state 下 legalPool == 48
- [x] 3.8 测试：deck 内每个 (Color, Number) 组合恰好 1 张

## 4. xUnit 测试 — 概率边界

- [x] 4.1 测试：SolvableRate=1 + 一般 state，options 至少 1 张合法，isDeadlock=false
- [x] 4.2 测试：SolvableRate=0 + 一般 state（充裕非法池），options 全非法，isDeadlock=true
- [x] 4.3 测试：WildAppearRate=1，options 恰好 1 张 Wild，isDeadlock=false
- [x] 4.4 测试：WildAppearRate=0，多 seed 多次调用 options 中无 Wild

## 5. xUnit 测试 — 守卫触发

- [x] 5.1 测试：state=`(null,null,Asc)` SolvableRate=0 WildAppearRate=0 OptionCount=5，isDeadlock=false（守卫强制全合法）
- [x] 5.2 测试：state=`(null,null,*)` 多 SolvableRate ∈ {0, 0.3, 0.7, 1.0}，isDeadlock 恒 false
- [x] 5.3 测试：state=`(Red,null,Desc)` OptionCount=5 SolvableRate=0 WildAppearRate=0，illegalPool=36 充裕（异色 30 Number + 异色 6 Reverse），守卫不触发，options 全非法
- [x] 5.4 测试：state=`(Red,null,Desc)` OptionCount=5 SolvableRate=1，legalSlot=1 illegalSlot=4，守卫不触发，标准输出
- [x] 5.5 测试：一般中盘 `(Red,5,Asc)` OptionCount=3 SolvableRate=0.7，illegalPool 充裕（≈33），守卫不触发

## 6. xUnit 测试 — 洗牌与可重放

- [x] 6.1 测试：相同 state + config + `new Random(42)` 两次调用，options 逐元素相等，isDeadlock 相同
- [x] 6.2 测试：不同 seed 同 state config，多次采样下 options 各位置 Wild/合法/非法分布近似均匀（弱断言：3 种类别都至少出现一次在每个位置上，N=1000 样本）

## 7. xUnit 测试 — 大样本概率收敛

- [x] 7.1 测试：state=`(Red,5,Asc)` SolvableRate=0.5 WildAppearRate=0，N=10000，seed=12345，`isDeadlock=false` 占比 ∈ [0.47, 0.53]
- [x] 7.2 测试：同上 state，SolvableRate=0.7，N=10000，`isDeadlock=false` 占比 ∈ [0.67, 0.73]
- [x] 7.3 测试：state=`(Red,5,Asc)` SolvableRate=0 WildAppearRate=0.05，N=10000，含 Wild 占比 ∈ [0.02, 0.08]

## 8. xUnit 测试 — 极端边界

- [x] 8.1 测试：构造极端 config（OptionCount=20 在反转后 state legalPool=12），Generate 不抛异常，返回选项数 ≤ OptionCount
- [x] 8.2 测试：同色反转牌 `(Red,null,Desc)` 状态下池规模核对——legalPool == 12（10 张 Red Number 同色 + 2 张 Red Reverse 同色；异色 Number/Reverse 全非法）

## 9. 验证与归档准备

- [x] 9.1 `dotnet build CardChainCore.sln` 零警告零错误
- [x] 9.2 `dotnet test CardChainCore.sln` 全部通过（109 旧 + 30 新 = 139 通过）
- [x] 9.3 `grep UnityEngine CardChainCore/` 零匹配
- [x] 9.4 检查 OptionGenerator.cs (160 行) 与 HackDifficultyConfig.cs (26 行) 行数总计 < 300（单文件 < 300）
- [x] 9.5 更新 `Docs/TODO.md` Change 3 勾选状态（实施完成但未归档时标 "✅ 已实施 (日期，待归档)"）
- [x] 9.6 运行 `openspec validate cardchain-deck-generator` 通过

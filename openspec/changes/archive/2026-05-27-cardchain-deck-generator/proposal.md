## Why

Phase 1 Change 2 已落地接龙合法性纯函数（`IsValidNext` / `ApplyPrev`），但 `HackSession` 还无法生成每轮选项。本 change 实现 `INTERFACE.md` 第五节"发牌算法 Option F：合法位扩展守卫版"——按 `SolvableRate` / `WildAppearRate` 双重概率组合合法/非法/王牌选项，并在非法池不足时触发缺口转合法的扩展守卫。这是 `HackSession` 骨架（Change 4）的前置：没有选项生成器，骇入流程的"出牌→选择"循环无法启动。

## What Changes

- 新增 `HackDifficultyConfig`：`OptionCount` / `TargetChainCount` / `TotalTime` / `SolvableRate` / `WildAppearRate` 五字段纯数据类
- 新增 `internal class OptionGenerator`：单一公开方法 `Generate(SessionState state, HackDifficultyConfig config, Random random) → (CardData[] options, bool isDeadlock)`
- 实现 48 张逻辑 deck 的合法/非法分桶（40 Number + 8 Reverse，**王牌不进 deck**）
- 实现合法位扩展守卫：非法池不足时缺口位强制转为合法位，覆盖 SolvableRate 配置
- 实现 `WildAppearRate` 独立判定塞王牌（占 1 选项位，不进 deck）
- 实现选项数组最终洗牌（`shuffle(options)`），保证 UI 位置不可预测
- 新增 xUnit 测试覆盖：选项数量恒等、不重复、Empty 不出现、概率边界、守卫触发、大样本统计收敛、王牌注入

## Capabilities

### New Capabilities
- `cardchain-deck-generator`: 单轮选项生成——按难度配置生成 OptionCount 张选项牌，区分合法/非法/王牌，处理非法池不足时的扩展守卫，输出洗牌后的选项数组与 isDeadlock 标志

### Modified Capabilities
（无；本 change 不修改 `cardchain-types` / `cardchain-validator` 现有 requirement，仅消费它们的公开 API）

## Impact

- **代码**：新增 `CardChainCore/src/Unomata.Core/HackDifficultyConfig.cs` 与 `OptionGenerator.cs`
- **测试**：新增 `CardChainCore/tests/Unomata.Core.Tests/OptionGeneratorTests.cs`，预计 25~35 个测试用例
- **依赖**：仅消费 Change 1 的 `CardData` / 枚举与 Change 2 的 `IsValidNext`，零新增第三方依赖
- **文档**：`Docs/GAME_DESIGN.md` 已在 3.5.4 落档"选项呈现顺序必须随机化"；本 change 实现侧将 spec 落地到 `openspec/specs/cardchain-deck-generator/`
- **下游**：解锁 Change 4 `hacksession-skeleton`——`HackSession` 每轮调用本 change 的 `OptionGenerator.Generate` 生成 `CurrentOptions`
- **不影响**：Unity 端、QFramework 注册、现有归档 spec

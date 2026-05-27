## Context

Phase 1 接龙规则核心逻辑（`IsValidNext` / `ApplyPrev`）已落地（Change 2，109 测试通过）。本 change 在其上构建**单轮选项生成器**，是 `HackSession` 主循环的关键依赖。

参考资料：
- `Docs/INTERFACE.md` 第五节"发牌算法"——已固化 Option F 合法位扩展守卫版伪代码与四种轮次组合表
- `Docs/GAME_DESIGN.md` 3.3 卡池构成、3.5 匹配规则、3.5.4 选项呈现顺序（昨日新增）
- `Docs/TODO.md` Change 3 验收项

约束：
- Core 层零 `UnityEngine` 依赖，纯 .NET 8
- 注入式 `System.Random`，固定 seed 可重放
- 性能：n=48，单次 Generate 不构成瓶颈，可忍受少量 List 分配

## Goals / Non-Goals

**Goals:**
- 实现 `INTERFACE.md` 第五节伪代码的**逐行对应**——任何阅读 INTERFACE 的人能直接 diff 代码与伪代码
- 在非法池不足时正确触发扩展守卫，对 `(null,null,*)` / `(C,null,*)` 等小池状态保证 `isDeadlock=false` 的正确语义
- 可测试性：所有概率分支与守卫分支都能用固定 seed 复现
- 提供大样本统计测试，验证概率收敛于配置（容差 ±3%）

**Non-Goals:**
- ❌ 不实现 `HackSession` 主循环（Change 4）
- ❌ 不实现 maxPot / latch / overflow（Change 5）
- ❌ 不实现 `Surrender` / `HackResult`（Change 6）
- ❌ 不优化抽样性能（n=48 太小，提前优化无意义）
- ❌ 不引入"上界 SolvableRate"（即不对一般 state 加上 cap，按 INTERFACE 定义"下界语义"实现）

## Decisions

### D1：deck 抽样实现 = 双 List 分桶

**决策**：每次 `Generate` 调用时，遍历 48 张笛卡尔积牌 → 按 `IsValidNext` 分入 `legalPool` / `illegalPool` 两个 `List<CardData>`，再从池里抽样。

**替代方案**：
- B：yield + 随机过滤——延迟分配，但合法/非法不分桶时无法做扩展守卫的池规模判断
- C：缓存按 state 哈希预计算——n=48 无需缓存，徒增复杂度

**为何 A 优于 B/C**：分桶天然支持守卫的"非法池规模 < 所需非法位"判定；性能成本在 n=48 完全可忽略；测试易写。

### D2：deck 笛卡尔积枚举顺序固定

**决策**：deck 枚举顺序固定为 `for color in {Red,Blue,Green,Yellow}: for number in 0..9: yield Number; yield Number;`（每种 Number 两张以满足 40 张），然后 `for color: yield Reverse; yield Reverse;`（每色 2 张 Reverse，共 8 张）。

**理由**：固定顺序+ `Random` 注入即可保证测试可重放；同时让 `legalPool` / `illegalPool` 内的元素相对顺序确定，便于断言。

**注意**：每色每数字 2 张这点要严格落实，对应 GAME_DESIGN 3.3 "10×4×1 Number 共 40 张"的设计——这意味着 deck 实际是 **40 张 Number，每个 (Color, Number) 组合 1 张**，不是 2 张。重新核对：

```
Number: 4 颜色 × 10 数字 × 1 张 = 40 张  ✓
Reverse: 4 颜色 × 2 张 = 8 张             ✓
合计 48 张  ✓
```

修正：每个 `(Color, Number)` 组合**只有 1 张**，每色 2 张 Reverse。枚举：

```csharp
for (var color in CardColor.values) {
    for (int n = 0; n <= 9; n++) yield Number(color, n);
    yield Reverse(color); yield Reverse(color);
}
```

### D3：洗牌 = Fisher-Yates，使用同一注入 Random

**决策**：在选项填充完毕、计算 `isDeadlock` **之前**做 Fisher-Yates 洗牌；`isDeadlock` 只看选项内容（"是否任一合法"），与位置无关。

**替代方案**：洗牌后再扫描判 deadlock vs 洗牌前算好缓存——本质等价，前者代码更直白。

**理由**：与 INTERFACE 第五节伪代码 `shuffle(options); isDeadlock = ...` 顺序一致；洗牌使用同一 `random` 参数保证可重放。

### D4：Wild 注入路径与 deck 严格隔离

**决策**：
- `WildAppearRate` 命中 → 在 `options` 中插入 `new CardData(Type=Wild, Color=null, Number=null)`，**不**进入 `legalPool` / `illegalPool` 任何分桶
- 占用 1 个选项位（`illegalSlot = OptionCount - (hasWild ? 1 : 0) - legalSlot`）
- Wild 永远合法（由 IsValidNext 保证），但 isDeadlock 计算无需特判（"任一合法"自然涵盖）

**理由**：避免 Wild 在 deck 中污染合法/非法池规模统计（若 Wild 计入 legalPool，会让"`(null,null,*)` 状态合法池规模"错误增大）。

### D5：合法位扩展守卫 = 单向 deficit 转换

**决策**：

```
illegalSlot = OptionCount - hasWildSlot - legalSlot  // 初始基线
if illegalSlot > illegalPool.Count:
    deficit       = illegalSlot - illegalPool.Count
    illegalSlot  -= deficit
    legalSlot    += deficit
legalSlot = min(legalSlot, legalPool.Count)  // 极端兜底（合法池也不够）
```

**关键不变量**：`hasWildSlot + legalSlot + illegalSlot == OptionCount` 始终成立（除非合法池极端枯竭，此时 OptionCount 实际无法填满，作为 bug 信号）。

**替代方案**：双向再平衡（合法不够也能转非法）——非必要，本场景合法池恒足（`(null,null,*)` 时 legalPool=48）。

### D6：选项内不重复约束 = `sample_without_replacement`

**决策**：从 legalPool / illegalPool 抽样使用无放回（Fisher-Yates 部分洗牌取前 K 张）；Wild 仅塞 1 张，无重复风险。

**风险**：legalPool 与 illegalPool 之间不会出现同一张牌（IsValidNext 二分），故无需跨池去重。

### D7：随机源类型 = `System.Random`

**决策**：方法签名 `Generate(SessionState state, HackDifficultyConfig config, Random random)`。

**替代方案**：`IRandomSource` 接口——增加抽象层，xUnit 测试反而要 mock；`System.Random` 已是 BCL 标准，`new Random(seed)` 即固定。

**未来如需替换**（Phase 4 Unity 端 deterministic seed 同步）：再加一层 wrapper，本 change 不预投。

### D8：API 公开度

**决策**：
- `HackDifficultyConfig` → `public`（Unity 端会 new 它）
- `SessionState` → 已是 `internal`（Change 2 决定，不改）
- `OptionGenerator` → `internal`（仅 `HackSession` 调用，不暴露给 Unity）
- `OptionGenerator.Generate` → `internal static`（无状态，纯函数）

测试项目通过 `InternalsVisibleTo` 访问（Change 2 已设置，沿用）。

## Risks / Trade-offs

- **[Risk] 大样本统计测试在 CI 慢机器上偶发抖动**
  → Mitigation：固定 seed + N=10000 跑测；容差 ±3%（绝对值）；如未来抖动再调到 ±5%。

- **[Risk] `(C, 9, Asc)` / `(C, 0, Desc)` 边界状态合法池规模微变**（异色无解，仅同色 + 反转构成合法池），可能与 INTERFACE 表里 "~36 张" 估算偏差
  → Mitigation：测试用具体 state 算出确切池规模，断言精确数值，不依赖估算。

- **[Risk] 合法池极端枯竭 `legalSlot > legalPool.Count`**（INTERFACE 标 "极端罕见"）：Generate 返回选项数 < OptionCount
  → Mitigation：D5 的 `min` 兜底已处理，写测试覆盖（构造 state 让 legalPool 接近 0）；运行期不抛异常，让 HackSession 据 isDeadlock 决策。

- **[Trade-off] 每次 Generate 都重新分桶 48 张**（O(48) 遍历）
  → 接受：n=48 微秒级，每帧最多 1 次 Generate（一轮一次），完全无影响。

- **[Trade-off] Random 是引用类型，参数传递需注意线程安全**
  → 接受：Core 层无多线程预设，HackSession 单线程驱动。

## Migration Plan

无破坏性变更。本 change 纯新增：

1. 新增文件 → `dotnet build` 应通过
2. 新增测试 → `dotnet test` 应增加 ~30 项通过
3. 现有 109 测试不受影响
4. 无需任何回滚路径——失败仅影响 Change 4 的开始

## Open Questions

无。Q1~Q5 已在探索阶段敲定，Q4 的"必须洗牌"已落档到 `Docs/GAME_DESIGN.md` 3.5.4。

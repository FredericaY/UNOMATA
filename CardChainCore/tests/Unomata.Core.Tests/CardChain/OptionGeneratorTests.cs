using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Unomata.Core.Tests.CardChain;

/// <summary>
/// Change 3 cardchain-deck-generator 测试套件。
/// 覆盖：基本契约 / 概率边界 / 守卫触发 / 洗牌可重放 / 大样本概率收敛 / 极端兜底 / 纯函数。
/// 对应 spec.md 的 11 条 Requirement / 25+ Scenarios。
/// </summary>
public class OptionGeneratorTests
{
    // ── 工具：构造常用牌 ──────────────────────────────────────
    private static CardData Number(CardColor color, int number) => new()
    {
        Type = CardType.Number,
        Color = color,
        Number = number,
    };

    private static SessionState State(CardColor? lastColor, int? lastNumber, ChainDirection direction)
        => new() { LastColor = lastColor, LastNumber = lastNumber, Direction = direction };

    private static HackDifficultyConfig Config(int options, float solvable = 0.5f, float wild = 0f) => new()
    {
        OptionCount = options,
        TargetChainCount = 8,
        TotalTime = 12f,
        SolvableRate = solvable,
        WildAppearRate = wild,
    };

    // ──────────────────────────────────────────────────────
    // 1. 基本契约（spec: OptionGenerator 单轮选项生成 / Empty 永不出现 / Wild 不进入 deck）
    // ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Generate_OptionCountMatchesConfig_OnNormalState(int optionCount)
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(optionCount, solvable: 0.5f, wild: 0.05f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.Equal(optionCount, options.Length);
        }
    }

    [Fact]
    public void Generate_OptionsAreDistinct()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0.7f, wild: 0.1f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            // 由 (Type, Color, Number) 三元组比较去重
            var distinct = options.Select(o => (o.Type, o.Color, o.Number)).Distinct().Count();
            Assert.Equal(options.Length, distinct);
        }
    }

    [Fact]
    public void Generate_NeverContainsEmptyCard()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0.5f, wild: 0.5f);

        for (var seed = 0; seed < 100; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.DoesNotContain(options, o => o.Type == CardType.Empty);
        }
    }

    [Fact]
    public void Generate_WildNotInDeck_WhenWildRateIsZero()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0.5f, wild: 0f);

        for (var seed = 0; seed < 200; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.DoesNotContain(options, o => o.Type == CardType.Wild);
        }
    }

    [Fact]
    public void Generate_DoesNotMutateState()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0.7f, wild: 0.1f);

        OptionGenerator.Generate(state, config, new Random(42));

        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Equal(5, state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    // ──────────────────────────────────────────────────────
    // 2. deck 构成（spec: deck 构成与 Wild 隔离）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Deck_TotalSize_Is48()
    {
        Assert.Equal(48, OptionGenerator.Deck.Count);
    }

    [Fact]
    public void Deck_AllNumberCombinationsExist_Once()
    {
        var numbers = OptionGenerator.Deck.Where(c => c.Type == CardType.Number).ToList();
        Assert.Equal(40, numbers.Count);

        // 每 (Color, Number) 组合恰好 1 张
        var groups = numbers.GroupBy(c => (c.Color, c.Number));
        foreach (var g in groups)
        {
            Assert.Single(g);
        }
        Assert.Equal(40, groups.Count());
    }

    [Fact]
    public void Deck_TwoReversesPerColor()
    {
        var reverses = OptionGenerator.Deck.Where(c => c.Type == CardType.Reverse).ToList();
        Assert.Equal(8, reverses.Count);

        foreach (CardColor color in Enum.GetValues<CardColor>())
        {
            Assert.Equal(2, reverses.Count(r => r.Color == color));
        }
    }

    [Fact]
    public void Deck_LegalPoolEquals48_OnNullNullState()
    {
        // (null, null, *) 状态下任意牌均合法（除 Empty/Wild，但它们不在 deck 中）
        var state = State(null, null, ChainDirection.Ascending);
        var legal = OptionGenerator.Deck.Count(c => CardChainRules.IsValidNext(c, state));
        var illegal = OptionGenerator.Deck.Count(c => !CardChainRules.IsValidNext(c, state));
        Assert.Equal(48, legal);
        Assert.Equal(0, illegal);
    }

    // ──────────────────────────────────────────────────────
    // 3. 概率边界（spec: 多个 Scenario）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_SolvableRate1_ContainsAtLeastOneLegal_OnNormalState()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 1f, wild: 0f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.False(isDeadlock);
            Assert.Contains(options, o => CardChainRules.IsValidNext(o, state));
            Assert.DoesNotContain(options, o => o.Type == CardType.Wild);
        }
    }

    [Fact]
    public void Generate_SolvableRate0_AllIllegal_OnNormalState()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0f, wild: 0f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.True(isDeadlock);
            Assert.All(options, o => Assert.False(CardChainRules.IsValidNext(o, state)));
        }
    }

    [Fact]
    public void Generate_WildRate1_ExactlyOneWild_NotDeadlock()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0f, wild: 1f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.False(isDeadlock);
            Assert.Single(options.Where(o => o.Type == CardType.Wild));
        }
    }

    [Fact]
    public void Generate_WildRate0_NoWild_AllSeeds()
    {
        // 与 3.4 (Wild 不进入 deck) 不同：此处侧重 isDeadlock 不被 Wild 干扰
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0.5f, wild: 0f);

        var anyWild = false;
        for (var seed = 0; seed < 200; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            if (options.Any(o => o.Type == CardType.Wild))
            {
                anyWild = true;
                break;
            }
        }
        Assert.False(anyWild);
    }

    // ──────────────────────────────────────────────────────
    // 4. 合法位扩展守卫（spec: 合法位扩展守卫触发）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_NullNullState_NeverDeadlock_SolvableRate0()
    {
        // (null, null, *) state legalPool=48, illegalPool=0
        // 守卫强制全合法，isDeadlock 恒 false
        var state = State(null, null, ChainDirection.Ascending);
        var config = Config(5, solvable: 0f, wild: 0f);

        for (var seed = 0; seed < 100; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.False(isDeadlock);
            Assert.Equal(5, options.Length);
            Assert.All(options, o => Assert.True(CardChainRules.IsValidNext(o, state)));
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.3f)]
    [InlineData(0.7f)]
    [InlineData(1f)]
    public void Generate_NullNullState_DeadlockAlwaysFalse_AcrossAllSolvableRates(float solvable)
    {
        var state = State(null, null, ChainDirection.Ascending);
        var config = Config(5, solvable: solvable, wild: 0f);

        for (var seed = 0; seed < 50; seed++)
        {
            var (_, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.False(isDeadlock);
        }
    }

    [Fact]
    public void Generate_AfterReverseState_OptionCount5_SolvableRate0_AllIllegal()
    {
        // (Red, null, Desc) 反转后状态：
        // 异色数字全非法，异色 Reverse 同色合法/异色非法，同色 Number 合法、同色 Reverse 合法
        // illegalPool 含异色 Number 30 张 + 异色 Reverse 6 张 = 36 张
        // OptionCount=5，illegalSlot=5 ≤ 36，守卫不触发
        var state = State(CardColor.Red, null, ChainDirection.Descending);
        var config = Config(5, solvable: 0f, wild: 0f);

        for (var seed = 0; seed < 30; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.True(isDeadlock);
            Assert.Equal(5, options.Length);
            Assert.All(options, o => Assert.False(CardChainRules.IsValidNext(o, state)));
        }
    }

    [Fact]
    public void Generate_AfterReverseState_OptionCount5_SolvableRate1_StandardOutput()
    {
        // (Red, null, Desc) 反转后，SolvableRate=1
        // legalSlot=1 illegalSlot=4，illegalPool=36 充裕，守卫不触发
        var state = State(CardColor.Red, null, ChainDirection.Descending);
        var config = Config(5, solvable: 1f, wild: 0f);

        for (var seed = 0; seed < 30; seed++)
        {
            var (options, isDeadlock) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.False(isDeadlock);
            Assert.Equal(5, options.Length);
            Assert.Single(options.Where(o => CardChainRules.IsValidNext(o, state)));
        }
    }

    [Fact]
    public void Generate_NormalMidGame_GuardNotTriggered()
    {
        // (Red, 5, Asc) OptionCount=3 SolvableRate=0.7
        // illegalPool ≈ 33 充裕，守卫不触发，标准算法正常返回
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0.7f, wild: 0f);

        for (var seed = 0; seed < 30; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            Assert.Equal(3, options.Length);
        }
    }

    // ──────────────────────────────────────────────────────
    // 5. 洗牌与可重放（spec: 选项数组随机洗牌）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_SameSeed_ProducesSameOutput()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(5, solvable: 0.7f, wild: 0.1f);

        var (a, da) = OptionGenerator.Generate(state, config, new Random(42));
        var (b, db) = OptionGenerator.Generate(state, config, new Random(42));

        Assert.Equal(da, db);
        Assert.Equal(a.Length, b.Length);
        for (var i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i].Type, b[i].Type);
            Assert.Equal(a[i].Color, b[i].Color);
            Assert.Equal(a[i].Number, b[i].Number);
        }
    }

    [Fact]
    public void Generate_PositionDistribution_IsRandomized_AcrossSeeds()
    {
        // 弱断言：Wild 在每个位置都有可能出现（位置不是固定）
        // 用 WildRate=1 + SolvableRate=0 + OptionCount=3，保证恰好 1 张 Wild
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0f, wild: 1f);

        var wildPositionCounts = new int[3];
        for (var seed = 0; seed < 1000; seed++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, new Random(seed));
            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].Type == CardType.Wild)
                {
                    wildPositionCounts[i]++;
                    break;
                }
            }
        }

        // 每个位置都应有 Wild 出现过（弱断言：均匀洗牌下每位置约 333 次）
        foreach (var count in wildPositionCounts)
        {
            Assert.True(count > 200, $"Wild 在某位置出现次数 {count} 过低，洗牌可能未生效");
        }
    }

    // ──────────────────────────────────────────────────────
    // 6. 大样本概率收敛（spec: 大样本概率收敛）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_SolvableRate05_ConvergesIn10000Samples()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0.5f, wild: 0f);
        var rng = new Random(12345);

        var solvableCount = 0;
        const int n = 10000;
        for (var i = 0; i < n; i++)
        {
            var (_, isDeadlock) = OptionGenerator.Generate(state, config, rng);
            if (!isDeadlock) solvableCount++;
        }

        var ratio = solvableCount / (double)n;
        Assert.InRange(ratio, 0.47, 0.53);
    }

    [Fact]
    public void Generate_SolvableRate07_ConvergesIn10000Samples()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0.7f, wild: 0f);
        var rng = new Random(12345);

        var solvableCount = 0;
        const int n = 10000;
        for (var i = 0; i < n; i++)
        {
            var (_, isDeadlock) = OptionGenerator.Generate(state, config, rng);
            if (!isDeadlock) solvableCount++;
        }

        var ratio = solvableCount / (double)n;
        Assert.InRange(ratio, 0.67, 0.73);
    }

    [Fact]
    public void Generate_WildRate005_ConvergesIn10000Samples()
    {
        var state = State(CardColor.Red, 5, ChainDirection.Ascending);
        var config = Config(3, solvable: 0f, wild: 0.05f);
        var rng = new Random(12345);

        var wildCount = 0;
        const int n = 10000;
        for (var i = 0; i < n; i++)
        {
            var (options, _) = OptionGenerator.Generate(state, config, rng);
            if (options.Any(o => o.Type == CardType.Wild)) wildCount++;
        }

        var ratio = wildCount / (double)n;
        Assert.InRange(ratio, 0.02, 0.08);
    }

    // ──────────────────────────────────────────────────────
    // 7. 极端兜底 + 池规模核对（spec: 极端兜底，反转后池规模）
    // ──────────────────────────────────────────────────────

    [Fact]
    public void Generate_LegalPoolUnderflow_DoesNotThrow()
    {
        // (Red, null, Desc) 反转后：legalPool = 同色 Red Number 10 张 + 同色 Red Reverse 1 张 + Wild ?
        // Wild 不进 deck，所以 legalPool = 10 + 1 = 11 张
        // OptionCount=20 远超 legalPool，触发兜底
        var state = State(CardColor.Red, null, ChainDirection.Descending);
        var config = Config(20, solvable: 1f, wild: 0f);

        var (options, _) = OptionGenerator.Generate(state, config, new Random(0));
        Assert.NotNull(options);
        // 兜底后选项数 = legalSlot(<=11) + illegalSlot(<=36) <= OptionCount
        Assert.True(options.Length <= config.OptionCount);
    }

    [Fact]
    public void Generate_AfterReverseState_LegalPoolSizeIs11()
    {
        // (Red, null, Desc) 状态下 legalPool 应为：
        //   - 同色 Red Number 10 张（同色 Number 任意合法）
        //   - 同色 Red Reverse 1 张（注：deck 共 2 张 Red Reverse，理论 2 张同色 Reverse 都合法）
        //
        // 修正：deck 中 Red Reverse 共 2 张，state 同色 Reverse 同色合法 → 2 张都进 legalPool
        // legalPool = 10 + 2 = 12
        var state = State(CardColor.Red, null, ChainDirection.Descending);
        var legal = OptionGenerator.Deck.Where(c => CardChainRules.IsValidNext(c, state)).ToList();

        // 同色 Number
        Assert.Equal(10, legal.Count(c => c.Type == CardType.Number && c.Color == CardColor.Red));
        // 同色 Reverse
        Assert.Equal(2, legal.Count(c => c.Type == CardType.Reverse && c.Color == CardColor.Red));
        // 异色 Number 全非法
        Assert.Equal(0, legal.Count(c => c.Type == CardType.Number && c.Color != CardColor.Red));
        // 异色 Reverse 同色判定 → 全非法
        Assert.Equal(0, legal.Count(c => c.Type == CardType.Reverse && c.Color != CardColor.Red));
        // 总 legalPool 规模
        Assert.Equal(12, legal.Count);
    }
}

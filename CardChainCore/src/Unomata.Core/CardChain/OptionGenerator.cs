namespace Unomata.Core;

/// <summary>
/// 单轮选项生成器。严格按 <c>Docs/INTERFACE.md</c> 第五节
/// "发牌算法 Option F：合法位扩展守卫版"伪代码实现。
/// 内部使用，仅由 <c>HackSession</c> 在每轮 <c>OnNewRound</c> 前调用。
/// 纯函数：不持有可变状态，不修改入参 <see cref="SessionState"/>。
/// </summary>
internal static class OptionGenerator
{
    /// <summary>
    /// 固定 48 张逻辑 deck 的缓存：
    /// 4 颜色 × 10 数字 = 40 张 Number；
    /// 4 颜色 × 2 张 = 8 张 Reverse；
    /// 王牌 (Wild) 不进 deck，由 <see cref="HackDifficultyConfig.WildAppearRate"/> 独立判定后塞入选项。
    /// </summary>
    internal static readonly IReadOnlyList<CardData> Deck = BuildDeck();

    /// <summary>
    /// 构造 48 张固定 deck。每个 <c>(Color, Number)</c> 组合恰好 1 张，每色 2 张 Reverse。
    /// 枚举顺序固定（Red→Blue→Green→Yellow，0→9，Reverse 后置），
    /// 与 <c>System.Random</c> 注入一起保证测试可重放。
    /// </summary>
    internal static IReadOnlyList<CardData> BuildDeck()
    {
        var deck = new List<CardData>(48);
        foreach (CardColor color in Enum.GetValues<CardColor>())
        {
            for (var n = 0; n <= 9; n++)
            {
                deck.Add(new CardData
                {
                    Type = CardType.Number,
                    Color = color,
                    Number = n,
                });
            }
            // 每色 2 张 Reverse
            deck.Add(new CardData { Type = CardType.Reverse, Color = color, Number = null });
            deck.Add(new CardData { Type = CardType.Reverse, Color = color, Number = null });
        }
        return deck;
    }

    /// <summary>
    /// 按当前 <paramref name="state"/> 与 <paramref name="config"/> 生成一轮选项。
    /// 流程对应 INTERFACE.md 第五节伪代码：
    /// <list type="number">
    /// <item><description>按 <see cref="CardChainRules.IsValidNext"/> 将 deck 分入 legalPool / illegalPool</description></item>
    /// <item><description>独立判定 <c>isSolvable</c> 与 <c>hasWild</c></description></item>
    /// <item><description>合法位扩展守卫：非法池不足时差额转为合法位</description></item>
    /// <item><description>极端兜底：合法池不足时取 <c>min</c></description></item>
    /// <item><description>无放回抽样填入选项数组</description></item>
    /// <item><description>Fisher-Yates 全洗牌</description></item>
    /// <item><description>计算 <c>isDeadlock</c></description></item>
    /// </list>
    /// 不修改 <paramref name="state"/>；使用 <paramref name="random"/> 完成所有随机决策，固定 seed 可重放。
    /// </summary>
    /// <param name="state">当前会话状态（只读）。</param>
    /// <param name="config">本次骇入难度配置。</param>
    /// <param name="random">注入的随机源。</param>
    /// <returns>选项数组（长度 <c>OptionCount</c>，合法池极端枯竭时可能更短）与本轮死局标志。</returns>
    internal static (CardData[] Options, bool IsDeadlock) Generate(
        SessionState state,
        HackDifficultyConfig config,
        Random random)
    {
        // 1. 分桶
        var legalPool = new List<CardData>(48);
        var illegalPool = new List<CardData>(48);
        foreach (var card in Deck)
        {
            if (CardChainRules.IsValidNext(card, state))
            {
                legalPool.Add(card);
            }
            else
            {
                illegalPool.Add(card);
            }
        }

        // 2. 概率判定
        var isSolvable = random.NextDouble() < config.SolvableRate;
        var hasWild = random.NextDouble() < config.WildAppearRate;

        // 3. 计算选项位
        var hasWildSlot = hasWild ? 1 : 0;
        var legalSlot = isSolvable ? 1 : 0;
        var illegalSlot = config.OptionCount - hasWildSlot - legalSlot;

        // 4. 合法位扩展守卫：非法池不足 → 差额转为合法位
        if (illegalSlot > illegalPool.Count)
        {
            var deficit = illegalSlot - illegalPool.Count;
            illegalSlot -= deficit;
            legalSlot += deficit;
        }

        // 5. 极端兜底：合法池也不够（实践中几乎不触发）
        if (legalSlot > legalPool.Count)
        {
            legalSlot = legalPool.Count;
        }

        // 6. 无放回抽样
        var totalSlots = hasWildSlot + legalSlot + illegalSlot;
        var options = new CardData[totalSlots];
        var idx = 0;
        foreach (var card in SampleWithoutReplacement(legalPool, legalSlot, random))
        {
            options[idx++] = card;
        }
        foreach (var card in SampleWithoutReplacement(illegalPool, illegalSlot, random))
        {
            options[idx++] = card;
        }
        if (hasWildSlot == 1)
        {
            options[idx++] = new CardData
            {
                Type = CardType.Wild,
                Color = null,
                Number = null,
            };
        }

        // 7. 全洗牌（保证位置不可预测，详见 GAME_DESIGN.md 3.5.4）
        FisherYatesShuffle(options, random);

        // 8. 死局判定（与位置无关，仅看内容）
        var isDeadlock = true;
        foreach (var opt in options)
        {
            if (CardChainRules.IsValidNext(opt, state))
            {
                isDeadlock = false;
                break;
            }
        }

        return (options, isDeadlock);
    }

    /// <summary>
    /// 从 <paramref name="pool"/> 无放回抽 <paramref name="k"/> 张。
    /// Fisher-Yates 部分洗牌实现：拷贝 pool 到本地数组，前 k 位与剩余位逐个交换。
    /// 不修改原 pool。<paramref name="k"/> &lt;= <c>pool.Count</c> 由调用方保证。
    /// </summary>
    private static IEnumerable<CardData> SampleWithoutReplacement(
        List<CardData> pool,
        int k,
        Random random)
    {
        if (k <= 0)
        {
            yield break;
        }

        var buf = pool.ToArray();
        var n = buf.Length;
        for (var i = 0; i < k; i++)
        {
            var j = random.Next(i, n);
            (buf[i], buf[j]) = (buf[j], buf[i]);
            yield return buf[i];
        }
    }

    /// <summary>
    /// 标准 Fisher-Yates 洗牌，原地修改 <paramref name="array"/>。
    /// 使用同一注入 <paramref name="random"/> 保证测试可重放。
    /// </summary>
    private static void FisherYatesShuffle(CardData[] array, Random random)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}

using Xunit;

namespace Unomata.Core.Tests.CardChain;

/// <summary>
/// 重放修订后 <c>Docs/GAME_DESIGN.md</c> 第 3.5.3 节的完整 12 步序列。
/// 覆盖：开局 lastColor=null 任意合法 / 同色覆盖 / 严格 ±1 试探非法 /
/// 反转后 lastColor!=null+lastNumber=null 异色路径关闭 / 王牌后 state 重置 /
/// 连续两张同色 Reverse 合法（每色 2 张设计）。
/// </summary>
public class CardChainRulesTests_GameDesignReplay
{
    private static CardData Number(CardColor color, int number) => new()
    {
        Type = CardType.Number,
        Color = color,
        Number = number,
    };

    private static CardData Reverse(CardColor color) => new()
    {
        Type = CardType.Reverse,
        Color = color,
        Number = null,
    };

    private static CardData Wild() => new()
    {
        Type = CardType.Wild,
        Color = null,
        Number = null,
    };

    private static void AssertState(
        SessionState state,
        CardColor? expectedColor,
        int? expectedNumber,
        ChainDirection expectedDirection)
    {
        Assert.Equal(expectedColor, state.LastColor);
        Assert.Equal(expectedNumber, state.LastNumber);
        Assert.Equal(expectedDirection, state.Direction);
    }

    // §12.1 GAME_DESIGN 3.5.3 全 12 步序列重放
    [Fact]
    public void GameDesign_3_5_3_FullSequenceReplay()
    {
        // 开局：state = (null, null, Ascending)
        var state = new SessionState();
        AssertState(state, null, null, ChainDirection.Ascending);

        // Step 1: 接 Red-5 → 合法（lastColor=null 任意），state 变 (Red, 5, Asc)
        var red5 = Number(CardColor.Red, 5);
        Assert.True(CardChainRules.IsValidNext(red5, state));
        CardChainRules.ApplyPrev(red5, state);
        AssertState(state, CardColor.Red, 5, ChainDirection.Ascending);

        // Step 2: 接 Red-2 → 合法（同色覆盖方向），state 变 (Red, 2, Asc)
        var red2 = Number(CardColor.Red, 2);
        Assert.True(CardChainRules.IsValidNext(red2, state));
        CardChainRules.ApplyPrev(red2, state);
        AssertState(state, CardColor.Red, 2, ChainDirection.Ascending);

        // Step 3: 试探 Blue-5 → 非法（异色 + 严格升序需 N'==3，5≠3），state 不变
        var blue5 = Number(CardColor.Blue, 5);
        Assert.False(CardChainRules.IsValidNext(blue5, state));
        AssertState(state, CardColor.Red, 2, ChainDirection.Ascending);

        // Step 4: 接 Blue-3 → 合法（异色严格 +1，N'==2+1），state 变 (Blue, 3, Asc)
        var blue3 = Number(CardColor.Blue, 3);
        Assert.True(CardChainRules.IsValidNext(blue3, state));
        CardChainRules.ApplyPrev(blue3, state);
        AssertState(state, CardColor.Blue, 3, ChainDirection.Ascending);

        // Step 5: 接 Blue-Reverse → 合法（同色反转），state 变 (Blue, null, Desc)
        var blueReverse = Reverse(CardColor.Blue);
        Assert.True(CardChainRules.IsValidNext(blueReverse, state));
        CardChainRules.ApplyPrev(blueReverse, state);
        AssertState(state, CardColor.Blue, null, ChainDirection.Descending);

        // Step 6: 试探 Yellow-2 → 非法（反转后 lastColor!=null + lastNumber=null，
        //          异色路径关闭），state 不变
        var yellow2 = Number(CardColor.Yellow, 2);
        Assert.False(CardChainRules.IsValidNext(yellow2, state));
        AssertState(state, CardColor.Blue, null, ChainDirection.Descending);

        // Step 7: 接 Blue-7 → 合法（同色覆盖），state 变 (Blue, 7, Desc)
        var blue7 = Number(CardColor.Blue, 7);
        Assert.True(CardChainRules.IsValidNext(blue7, state));
        CardChainRules.ApplyPrev(blue7, state);
        AssertState(state, CardColor.Blue, 7, ChainDirection.Descending);

        // Step 8: 接 Wild → 合法，state 变 (null, null, Desc)（方向不变）
        var wild = Wild();
        Assert.True(CardChainRules.IsValidNext(wild, state));
        CardChainRules.ApplyPrev(wild, state);
        AssertState(state, null, null, ChainDirection.Descending);

        // Step 9: 接 Red-9 → 合法（lastColor=null 任意），state 变 (Red, 9, Desc)
        var red9 = Number(CardColor.Red, 9);
        Assert.True(CardChainRules.IsValidNext(red9, state));
        CardChainRules.ApplyPrev(red9, state);
        AssertState(state, CardColor.Red, 9, ChainDirection.Descending);

        // Step 10: 接 Red-Reverse → 合法（同色反转，第一张），state 变 (Red, null, Asc)
        var redReverseFirst = Reverse(CardColor.Red);
        Assert.True(CardChainRules.IsValidNext(redReverseFirst, state));
        CardChainRules.ApplyPrev(redReverseFirst, state);
        AssertState(state, CardColor.Red, null, ChainDirection.Ascending);

        // Step 11: 接 Red-Reverse → 合法（同色反转，第二张连续 — 每色 2 张设计），
        //          state 变 (Red, null, Desc)
        var redReverseSecond = Reverse(CardColor.Red);
        Assert.True(CardChainRules.IsValidNext(redReverseSecond, state));
        CardChainRules.ApplyPrev(redReverseSecond, state);
        AssertState(state, CardColor.Red, null, ChainDirection.Descending);

        // Step 12: 接 Red-4 → 合法（同色任意），state 变 (Red, 4, Desc)
        var red4 = Number(CardColor.Red, 4);
        Assert.True(CardChainRules.IsValidNext(red4, state));
        CardChainRules.ApplyPrev(red4, state);
        AssertState(state, CardColor.Red, 4, ChainDirection.Descending);
    }
}

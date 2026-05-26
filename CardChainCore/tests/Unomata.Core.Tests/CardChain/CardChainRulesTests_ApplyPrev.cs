using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_ApplyPrev
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

    // §10.1 数字牌：更新 LastColor / LastNumber，Direction 不变
    [Fact]
    public void ApplyPrev_Number_UpdatesColorAndNumber()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Blue,
            LastNumber = 7,
            Direction = ChainDirection.Ascending,
        };

        CardChainRules.ApplyPrev(Number(CardColor.Red, 3), state);

        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Equal(3, state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    [Fact]
    public void ApplyPrev_Number_DoesNotFlipDirection_Descending()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Blue,
            LastNumber = 7,
            Direction = ChainDirection.Descending,
        };

        CardChainRules.ApplyPrev(Number(CardColor.Yellow, 5), state);

        Assert.Equal(ChainDirection.Descending, state.Direction);
    }

    // §10.2 反转牌：更新 LastColor、LastNumber=null、Direction 翻转
    [Fact]
    public void ApplyPrev_Reverse_FlipsDirectionAscToDesc()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Blue,
            LastNumber = 7,
            Direction = ChainDirection.Ascending,
        };

        CardChainRules.ApplyPrev(Reverse(CardColor.Red), state);

        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Descending, state.Direction);
    }

    [Fact]
    public void ApplyPrev_Reverse_FlipsDirectionDescToAsc()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Descending,
        };

        CardChainRules.ApplyPrev(Reverse(CardColor.Yellow), state);

        Assert.Equal(CardColor.Yellow, state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    // §10.3 王牌：清 LastColor / LastNumber，Direction 不变
    [Fact]
    public void ApplyPrev_Wild_ResetsColorAndNumber()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Descending,
        };

        CardChainRules.ApplyPrev(Wild(), state);

        Assert.Null(state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Descending, state.Direction);
    }

    // §10.4 连续两次反转方向回到初始
    [Fact]
    public void ApplyPrev_TwoReverses_DirectionRestored()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };

        CardChainRules.ApplyPrev(Reverse(CardColor.Red), state);
        Assert.Equal(ChainDirection.Descending, state.Direction);

        CardChainRules.ApplyPrev(Reverse(CardColor.Red), state);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }
}

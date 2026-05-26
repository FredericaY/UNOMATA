using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_Purity
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

    [Fact]
    public void IsValidNext_DoesNotMutateState_OnTrueResult()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };

        // 同色合法
        var result = CardChainRules.IsValidNext(Number(CardColor.Red, 2), state);

        Assert.True(result);
        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Equal(5, state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    [Fact]
    public void IsValidNext_DoesNotMutateState_OnFalseResult()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };

        // 异色升序 5→3 非法
        var result = CardChainRules.IsValidNext(Number(CardColor.Blue, 3), state);

        Assert.False(result);
        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Equal(5, state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    [Fact]
    public void ApplyPrev_DoesNotMutatePrev_Number()
    {
        var prev = Number(CardColor.Red, 5);
        var state = new SessionState();

        CardChainRules.ApplyPrev(prev, state);

        Assert.Equal(CardType.Number, prev.Type);
        Assert.Equal(CardColor.Red, prev.Color);
        Assert.Equal(5, prev.Number);
    }

    [Fact]
    public void ApplyPrev_DoesNotMutatePrev_Reverse()
    {
        var prev = Reverse(CardColor.Blue);
        var state = new SessionState();

        CardChainRules.ApplyPrev(prev, state);

        Assert.Equal(CardType.Reverse, prev.Type);
        Assert.Equal(CardColor.Blue, prev.Color);
        Assert.Null(prev.Number);
    }

    [Fact]
    public void ApplyPrev_DoesNotMutatePrev_Wild()
    {
        var prev = Wild();
        var state = new SessionState();

        CardChainRules.ApplyPrev(prev, state);

        Assert.Equal(CardType.Wild, prev.Type);
        Assert.Null(prev.Color);
        Assert.Null(prev.Number);
    }
}

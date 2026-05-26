using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_Wild
{
    private static CardData Wild() => new()
    {
        Type = CardType.Wild,
        Color = null,
        Number = null,
    };

    [Fact]
    public void Wild_AlwaysValid_OpeningState()
    {
        var state = new SessionState();
        Assert.True(CardChainRules.IsValidNext(Wild(), state));
    }

    [Fact]
    public void Wild_AlwaysValid_WithNumberContext()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };
        Assert.True(CardChainRules.IsValidNext(Wild(), state));
    }

    [Fact]
    public void Wild_AlwaysValid_AfterReverse()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = null,
            Direction = ChainDirection.Descending,
        };
        Assert.True(CardChainRules.IsValidNext(Wild(), state));
    }

    [Fact]
    public void Wild_AlwaysValid_AfterWild()
    {
        var state = new SessionState
        {
            LastColor = null,
            LastNumber = null,
            Direction = ChainDirection.Ascending,
        };
        Assert.True(CardChainRules.IsValidNext(Wild(), state));
    }
}

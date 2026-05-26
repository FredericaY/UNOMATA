using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_NumberSameColor
{
    private static CardData Number(CardColor color, int number) => new()
    {
        Type = CardType.Number,
        Color = color,
        Number = number,
    };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void SameColor_Ascending_AlwaysValid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };
        Assert.True(CardChainRules.IsValidNext(Number(CardColor.Red, nextNumber), state));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void SameColor_Descending_AlwaysValid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Descending,
        };
        Assert.True(CardChainRules.IsValidNext(Number(CardColor.Red, nextNumber), state));
    }
}

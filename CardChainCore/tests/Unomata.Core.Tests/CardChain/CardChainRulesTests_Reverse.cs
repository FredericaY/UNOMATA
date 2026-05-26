using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_Reverse
{
    private static CardData Reverse(CardColor color) => new()
    {
        Type = CardType.Reverse,
        Color = color,
        Number = null,
    };

    [Theory]
    [InlineData(CardColor.Red)]
    [InlineData(CardColor.Blue)]
    [InlineData(CardColor.Green)]
    [InlineData(CardColor.Yellow)]
    public void Reverse_LastColorNull_AnyReverseValid(CardColor reverseColor)
    {
        var state = new SessionState { LastColor = null };
        Assert.True(CardChainRules.IsValidNext(Reverse(reverseColor), state));
    }

    [Theory]
    [InlineData(CardColor.Red)]
    [InlineData(CardColor.Blue)]
    [InlineData(CardColor.Green)]
    [InlineData(CardColor.Yellow)]
    public void Reverse_SameColor_Valid(CardColor color)
    {
        var state = new SessionState { LastColor = color, LastNumber = 5 };
        Assert.True(CardChainRules.IsValidNext(Reverse(color), state));
    }

    [Theory]
    [InlineData(CardColor.Red, CardColor.Blue)]
    [InlineData(CardColor.Red, CardColor.Green)]
    [InlineData(CardColor.Red, CardColor.Yellow)]
    [InlineData(CardColor.Blue, CardColor.Green)]
    public void Reverse_DifferentColor_Invalid(CardColor lastColor, CardColor reverseColor)
    {
        var state = new SessionState { LastColor = lastColor, LastNumber = 5 };
        Assert.False(CardChainRules.IsValidNext(Reverse(reverseColor), state));
    }

    // §7.3 连续两张同色 Reverse（每色 2 张设计）：第二张同色 Reverse 仍合法，
    // ApplyPrev 两次后 Direction 回到初始值，LastColor 仍保留，LastNumber 始终 null。
    [Fact]
    public void Reverse_TwoSameColorReverses_BothValid()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };

        // 第一张同色 Reverse：合法
        Assert.True(CardChainRules.IsValidNext(Reverse(CardColor.Red), state));
        CardChainRules.ApplyPrev(Reverse(CardColor.Red), state);
        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Descending, state.Direction);

        // 第二张同色 Reverse：仍合法（落在"同色 Reverse"分支，与第一张状态无关）
        Assert.True(CardChainRules.IsValidNext(Reverse(CardColor.Red), state));
        CardChainRules.ApplyPrev(Reverse(CardColor.Red), state);
        Assert.Equal(CardColor.Red, state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }
}

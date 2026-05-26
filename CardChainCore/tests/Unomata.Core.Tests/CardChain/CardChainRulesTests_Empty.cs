using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_Empty
{
    [Fact]
    public void Empty_AsNext_AlwaysInvalid()
    {
        // 多种 state 排列下都应非法
        var states = new[]
        {
            new SessionState(),
            new SessionState
            {
                LastColor = CardColor.Red,
                LastNumber = 5,
                Direction = ChainDirection.Ascending,
            },
            new SessionState
            {
                LastColor = CardColor.Blue,
                LastNumber = null,
                Direction = ChainDirection.Descending,
            },
            new SessionState
            {
                LastColor = null,
                LastNumber = null,
                Direction = ChainDirection.Descending,
            },
        };

        foreach (var state in states)
        {
            Assert.False(CardChainRules.IsValidNext(CardData.Empty, state));
        }
    }

    [Fact]
    public void Empty_AsPrev_ThrowsAndStateUnchanged()
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };

        // 快照
        var lastColorBefore = state.LastColor;
        var lastNumberBefore = state.LastNumber;
        var directionBefore = state.Direction;

        Assert.Throws<InvalidOperationException>(
            () => CardChainRules.ApplyPrev(CardData.Empty, state));

        // 异常后 state 字段值不变
        Assert.Equal(lastColorBefore, state.LastColor);
        Assert.Equal(lastNumberBefore, state.LastNumber);
        Assert.Equal(directionBefore, state.Direction);
    }
}

using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_NumberBoundary
{
    private static CardData Number(CardColor color, int number) => new()
    {
        Type = CardType.Number,
        Color = color,
        Number = number,
    };

    // §8.2 lastColor=null（开局 / Wild 后）→ 任意色任意数字合法
    [Theory]
    [InlineData(CardColor.Red, 0, ChainDirection.Ascending)]
    [InlineData(CardColor.Red, 9, ChainDirection.Ascending)]
    [InlineData(CardColor.Blue, 5, ChainDirection.Ascending)]
    [InlineData(CardColor.Green, 3, ChainDirection.Ascending)]
    [InlineData(CardColor.Yellow, 7, ChainDirection.Descending)]
    [InlineData(CardColor.Blue, 0, ChainDirection.Descending)]
    [InlineData(CardColor.Red, 9, ChainDirection.Descending)]
    public void LastColorNull_AnyNumberValid(CardColor nextColor, int nextNumber, ChainDirection direction)
    {
        var state = new SessionState
        {
            LastColor = null,
            LastNumber = null,
            Direction = direction,
        };
        Assert.True(CardChainRules.IsValidNext(Number(nextColor, nextNumber), state));
    }

    // §8.5 反转后 lastColor!=null + lastNumber=null → 异色数字全部非法
    [Theory]
    [InlineData(CardColor.Blue, 0, ChainDirection.Ascending)]
    [InlineData(CardColor.Blue, 5, ChainDirection.Ascending)]
    [InlineData(CardColor.Blue, 9, ChainDirection.Ascending)]
    [InlineData(CardColor.Green, 3, ChainDirection.Ascending)]
    [InlineData(CardColor.Yellow, 7, ChainDirection.Descending)]
    [InlineData(CardColor.Green, 0, ChainDirection.Descending)]
    [InlineData(CardColor.Yellow, 9, ChainDirection.Descending)]
    public void ReverseAfter_DifferentColor_AllInvalid(CardColor nextColor, int nextNumber, ChainDirection direction)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = null,
            Direction = direction,
        };
        Assert.False(CardChainRules.IsValidNext(Number(nextColor, nextNumber), state));
    }

    // §8.5 反转后同色任意数字合法（同色覆盖路径）
    [Theory]
    [InlineData(0, ChainDirection.Ascending)]
    [InlineData(5, ChainDirection.Ascending)]
    [InlineData(9, ChainDirection.Ascending)]
    [InlineData(0, ChainDirection.Descending)]
    [InlineData(5, ChainDirection.Descending)]
    [InlineData(9, ChainDirection.Descending)]
    public void ReverseAfter_SameColor_AllValid(int nextNumber, ChainDirection direction)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = null,
            Direction = direction,
        };
        Assert.True(CardChainRules.IsValidNext(Number(CardColor.Red, nextNumber), state));
    }

    // §8.6 升序边界 9：同色任意 0-9 都合法
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(9)]
    public void BoundaryNine_Ascending_SameColorValid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 9,
            Direction = ChainDirection.Ascending,
        };
        Assert.True(CardChainRules.IsValidNext(Number(CardColor.Red, nextNumber), state));
    }

    // §8.6 升序边界 9：异色 0-9 全非法（理由：N'==10 不存在，严格 +1 无解）
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void BoundaryNine_Ascending_DifferentColorAllInvalid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 9,
            Direction = ChainDirection.Ascending,
        };
        Assert.False(CardChainRules.IsValidNext(Number(CardColor.Blue, nextNumber), state));
    }

    // §8.7 降序边界 0：同色任意合法
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void BoundaryZero_Descending_SameColorValid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 0,
            Direction = ChainDirection.Descending,
        };
        Assert.True(CardChainRules.IsValidNext(Number(CardColor.Red, nextNumber), state));
    }

    // §8.7 降序边界 0：异色全非法（理由：N'==-1 不存在，严格 -1 无解）
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void BoundaryZero_Descending_DifferentColorAllInvalid(int nextNumber)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 0,
            Direction = ChainDirection.Descending,
        };
        Assert.False(CardChainRules.IsValidNext(Number(CardColor.Blue, nextNumber), state));
    }
}

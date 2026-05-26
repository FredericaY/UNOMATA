using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardChainRulesTests_NumberDirection
{
    private static CardData Number(CardColor color, int number) => new()
    {
        Type = CardType.Number,
        Color = color,
        Number = number,
    };

    // §8.3 异色升序：lastNumber=5, next=Blue-{0..9}, 仅 Number==6 合法（严格 +1）
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]   // 异色同数字非法
    [InlineData(6, true)]    // 唯一合法解（lastNumber + 1）
    [InlineData(7, false)]   // 旧规则下合法，严格 +1 后非法
    [InlineData(9, false)]   // 旧规则下合法，严格 +1 后非法
    public void DifferentColor_Ascending(int nextNumber, bool expected)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Ascending,
        };
        Assert.Equal(expected, CardChainRules.IsValidNext(Number(CardColor.Blue, nextNumber), state));
    }

    // §8.4 异色降序：lastNumber=5, next=Blue-{0..9}, 仅 Number==4 合法（严格 -1）
    [Theory]
    [InlineData(0, false)]   // 旧规则下合法，严格 -1 后非法
    [InlineData(1, false)]   // 同上
    [InlineData(3, false)]   // 同上
    [InlineData(4, true)]    // 唯一合法解（lastNumber - 1）
    [InlineData(5, false)]   // 异色同数字非法
    [InlineData(6, false)]
    [InlineData(9, false)]
    public void DifferentColor_Descending(int nextNumber, bool expected)
    {
        var state = new SessionState
        {
            LastColor = CardColor.Red,
            LastNumber = 5,
            Direction = ChainDirection.Descending,
        };
        Assert.Equal(expected, CardChainRules.IsValidNext(Number(CardColor.Blue, nextNumber), state));
    }
}

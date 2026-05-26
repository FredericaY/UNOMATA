using System.Reflection;
using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardDataTests
{
    [Fact]
    public void Number_Card_ShallStoreAllThreeFields()
    {
        var card = new CardData
        {
            Type = CardType.Number,
            Color = CardColor.Red,
            Number = 5,
        };

        Assert.Equal(CardType.Number, card.Type);
        Assert.Equal(CardColor.Red, card.Color);
        Assert.Equal(5, card.Number);
    }

    [Fact]
    public void Reverse_Card_ShallHaveNullNumber()
    {
        var card = new CardData
        {
            Type = CardType.Reverse,
            Color = CardColor.Blue,
            Number = null,
        };

        Assert.Equal(CardType.Reverse, card.Type);
        Assert.Equal(CardColor.Blue, card.Color);
        Assert.Null(card.Number);
    }

    [Fact]
    public void Wild_Card_ShallHaveNullColorAndNumber()
    {
        var card = new CardData
        {
            Type = CardType.Wild,
            Color = null,
            Number = null,
        };

        Assert.Equal(CardType.Wild, card.Type);
        Assert.Null(card.Color);
        Assert.Null(card.Number);
    }

    [Fact]
    public void CardData_ShallNotExposeCanFollowMethod()
    {
        // 旧版 CanFollow 方法已废弃，合法性判定改由 HackSession 内部完成
        var methods = typeof(CardData)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(methods, m => m.Name == "CanFollow");
    }

    [Fact]
    public void Empty_ShallHaveCorrectFieldValues()
    {
        var empty = CardData.Empty;

        Assert.Equal(CardType.Empty, empty.Type);
        Assert.Null(empty.Color);
        Assert.Null(empty.Number);
    }

    [Fact]
    public void Empty_ShallBeSingleton_ReferenceEqualOnRepeatedAccess()
    {
        var first = CardData.Empty;
        var second = CardData.Empty;

        Assert.Same(first, second);
    }
}

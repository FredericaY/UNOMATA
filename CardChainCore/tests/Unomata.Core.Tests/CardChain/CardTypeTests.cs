using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardTypeTests
{
    [Fact]
    public void Members_ShallBeExactlyFourValues()
    {
        var names = Enum.GetNames<CardType>();
        Assert.Equal(
            new[] { "Number", "Reverse", "Wild", "Empty" }.OrderBy(s => s),
            names.OrderBy(s => s));
    }

    [Fact]
    public void Count_ShallBeFour()
    {
        Assert.Equal(4, Enum.GetValues<CardType>().Length);
    }
}

using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class CardColorTests
{
    [Fact]
    public void Members_ShallBeExactlyFourColors()
    {
        var names = Enum.GetNames<CardColor>();
        Assert.Equal(
            new[] { "Red", "Blue", "Green", "Yellow" }.OrderBy(s => s),
            names.OrderBy(s => s));
    }

    [Fact]
    public void Count_ShallBeFour()
    {
        Assert.Equal(4, Enum.GetValues<CardColor>().Length);
    }
}

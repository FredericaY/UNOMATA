using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class ChainDirectionTests
{
    [Fact]
    public void Members_ShallBeExactlyTwoDirections()
    {
        var names = Enum.GetNames<ChainDirection>();
        Assert.Equal(
            new[] { "Ascending", "Descending" }.OrderBy(s => s),
            names.OrderBy(s => s));
    }

    [Fact]
    public void Count_ShallBeTwo()
    {
        Assert.Equal(2, Enum.GetValues<ChainDirection>().Length);
    }
}

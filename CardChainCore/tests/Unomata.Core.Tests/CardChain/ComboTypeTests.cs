using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class ComboTypeTests
{
    [Fact]
    public void Members_ShallBeExactlyThreeCombos()
    {
        var names = Enum.GetNames<ComboType>();
        Assert.Equal(
            new[] { "None", "SameColorTwice", "SameDirectionTwice" }.OrderBy(s => s),
            names.OrderBy(s => s));
    }

    [Fact]
    public void Count_ShallBeThree()
    {
        Assert.Equal(3, Enum.GetValues<ComboType>().Length);
    }
}

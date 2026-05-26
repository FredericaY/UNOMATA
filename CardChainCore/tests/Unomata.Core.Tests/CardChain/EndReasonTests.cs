using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class EndReasonTests
{
    [Fact]
    public void Members_ShallBeExactlyThreeReasons()
    {
        var names = Enum.GetNames<EndReason>();
        Assert.Equal(
            new[] { "TimeUp", "WrongCard", "Surrender" }.OrderBy(s => s),
            names.OrderBy(s => s));
    }

    [Fact]
    public void Count_ShallBeThree()
    {
        Assert.Equal(3, Enum.GetValues<EndReason>().Length);
    }

    [Fact]
    public void Members_ShallNotContainLegacyManual()
    {
        var names = Enum.GetNames<EndReason>();
        Assert.DoesNotContain("Manual", names);
    }
}

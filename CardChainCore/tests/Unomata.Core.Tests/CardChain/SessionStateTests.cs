using Xunit;

namespace Unomata.Core.Tests.CardChain;

public class SessionStateTests
{
    [Fact]
    public void DefaultConstructor_InitialValues()
    {
        var state = new SessionState();

        Assert.Null(state.LastColor);
        Assert.Null(state.LastNumber);
        Assert.Equal(ChainDirection.Ascending, state.Direction);
    }

    [Fact]
    public void InternalAccessibility()
    {
        var type = typeof(SessionState);

        Assert.False(type.IsPublic, "SessionState 应为 internal，不应暴露给 Core 外部。");
    }
}

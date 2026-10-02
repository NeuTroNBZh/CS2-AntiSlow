using AntiSlow.Core;

namespace AntiSlow.Core.Tests;

public class PlayerTargetTests
{
    [Theory]
    [InlineData("#12", 12)]
    [InlineData("#0", 0)]
    [InlineData(" #7 ", 7)]
    public void UserId_IsRead(string argument, int expected) => Assert.Equal(expected, PlayerTarget.UserIdOf(argument));

    // Review Focus 4: "#abc", "#-1" and plain names are not user ids.
    [Theory]
    [InlineData("#abc")]
    [InlineData("#-1")]
    [InlineData("#")]
    [InlineData("Neutron")]
    [InlineData("")]
    public void NotAUserId(string argument) => Assert.Null(PlayerTarget.UserIdOf(argument));

    [Theory]
    [InlineData("#12", true)]
    [InlineData("#abc", true)]
    [InlineData("Neutron", false)]
    public void LooksLikeUserId(string argument, bool expected) => Assert.Equal(expected, PlayerTarget.IsUserIdSyntax(argument));
}

public class PlayerTargetResolveTests
{
    private sealed record P(int UserId, string Name);

    private static readonly P[] Online = { new(3, "Neo"), new(258, "Wrapped") , new(4, "Kim") };

    // Review: #999 must never reach an engine entity index; only an exact user id among connected players matches.
    [Theory]
    [InlineData("#3", "Neo")]
    [InlineData("#258", "Wrapped")]
    [InlineData("#999", null)]
    [InlineData("#2", null)]
    [InlineData("#abc", null)]
    public void Resolve_IsAnExactUserIdMatch(string argument, string? expected)
    {
        Assert.Equal(expected, PlayerTarget.Resolve(argument, Online, p => p.UserId)?.Name);
    }
}

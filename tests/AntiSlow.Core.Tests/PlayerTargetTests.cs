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

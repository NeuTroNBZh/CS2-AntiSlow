using AntiSlow.Core;

namespace AntiSlow.Core.Tests;

public class WalkButtonsTests
{
    [Fact]
    public void Strip_RemovesWalk_AndKeepsMovement()
    {
        // Observed on the server: forward + move right + walk = 0x10408.
        Assert.Equal(0x408UL, WalkButtons.Strip(0x10408));
    }

    [Fact]
    public void Strip_AlsoRemovesTheAlternateWalkBit()
    {
        Assert.Equal(0x8UL, WalkButtons.Strip(0x8 | 0x20000));
    }

    [Fact]
    public void Strip_LeavesOtherButtonsUntouched()
    {
        Assert.Equal(0x40804UL, WalkButtons.Strip(0x40804));
    }

    // Known IN_* bits only: anything else means the hooked function is not RunCommand any more (game update).
    [Theory]
    [InlineData(0x10408UL, 0UL, true)]
    [InlineData(0UL, 0UL, true)]
    [InlineData(0x8UL, 0x10000UL, true)]
    [InlineData(0x7FFF_0000_0000_0000UL, 0UL, false)]
    [InlineData(0x8UL, 0x1_0000_0000_0000UL, false)]
    public void LooksLikeButtons_RejectsUnknownBits(ulong pressed, ulong changed, bool expected)
    {
        Assert.Equal(expected, WalkButtons.LooksLikeButtons(pressed, changed, knownMask: 0xF_FFFF_FFFFUL));
    }
}

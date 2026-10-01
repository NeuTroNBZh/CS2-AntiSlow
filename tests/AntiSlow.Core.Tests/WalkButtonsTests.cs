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
}

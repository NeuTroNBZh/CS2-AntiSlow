using AntiSlow.Core;

namespace AntiSlow.Core.Tests;

public class BlockListTests
{
    [Fact]
    public void Block_ThenIsBlocked_BySteamId()
    {
        var list = BlockList.Empty.Block(new BlockEntry(1, "Alice", BlockEntry.Permanent, ""));
        Assert.True(list.IsBlocked(1));
        Assert.False(list.IsBlocked(2));
    }

    [Fact]
    public void Block_AgainReplacesTheEntry()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(1, "Alice", 3, "a"))
            .Block(new BlockEntry(1, "Alice", 5, "b"));
        Assert.Equal(5, list.Entries[1].RoundsRemaining);
        Assert.Single(list.Entries);
    }

    [Fact]
    public void RoundEnded_CountsDown_AndExpiresAtZero_PermanentStays()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(1, "Alice", 2, ""))
            .Block(new BlockEntry(2, "Bob", 1, ""))
            .Block(new BlockEntry(3, "Carl", BlockEntry.Permanent, ""));

        var (next, expired) = list.RoundEnded();

        Assert.Equal(new[] { "Bob" }, expired.Select(e => e.PlayerName));
        Assert.Equal(1, next.Entries[1].RoundsRemaining);
        Assert.False(next.IsBlocked(2));
        Assert.True(next.IsBlocked(3));
    }

    [Fact]
    public void Unblock_RemovesOnlyThatPlayer()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(1, "Alice", BlockEntry.Permanent, ""))
            .Block(new BlockEntry(2, "Bob", BlockEntry.Permanent, ""))
            .Unblock(1);
        Assert.False(list.IsBlocked(1));
        Assert.True(list.IsBlocked(2));
    }

    [Fact]
    public void FindByName_IsCaseInsensitive_AndPartial()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(1, "NeuTroNBZh", BlockEntry.Permanent, ""))
            .Block(new BlockEntry(2, "Bob", BlockEntry.Permanent, ""));
        Assert.Equal(new ulong[] { 1 }, list.FindByName("neutron").Select(e => e.SteamId));
    }

    [Theory]
    [InlineData(0, BlockEntry.Permanent)]
    [InlineData(-4, BlockEntry.Permanent)]
    [InlineData(3, 3)]
    public void RoundsFromArgument_ZeroOrNegativeMeansPermanent(int argument, int expected)
    {
        Assert.Equal(expected, BlockEntry.RoundsFromArgument(argument));
    }
}

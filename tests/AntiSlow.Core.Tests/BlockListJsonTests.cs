using AntiSlow.Core;

namespace AntiSlow.Core.Tests;

public class BlockListJsonTests
{
    [Fact]
    public void RoundTrip_KeepsEveryEntry()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(76561199051460419, "NeuTroNBZh", BlockEntry.Permanent, "slow abuse"))
            .Block(new BlockEntry(2, "Bob", 4, ""));

        Assert.True(BlockListJson.TryParse(BlockListJson.Serialize(list), out var back));

        Assert.Equal(list.Entries.Values.OrderBy(e => e.SteamId), back.Entries.Values.OrderBy(e => e.SteamId));
    }

    [Fact]
    public void TryParse_EmptyContent_IsAValidEmptyList()
    {
        Assert.True(BlockListJson.TryParse("  ", out var list));
        Assert.Empty(list.Entries);
    }

    // A truncated or corrupt file must be reported, never silently treated as "no block" and then overwritten.
    [Theory]
    [InlineData("not json")]
    [InlineData("[{\"SteamId\": 1, \"PlayerName\": \"A\"")]
    [InlineData("{\"unexpected\": true}")]
    public void TryParse_CorruptContent_IsReported(string content)
    {
        Assert.False(BlockListJson.TryParse(content, out var list));
        Assert.Empty(list.Entries);
    }

    // Remote tools read one console line with the blocked SteamIDs (as text, sorted).
    [Fact]
    public void StateLine_ListsBlockedSteamIds()
    {
        var list = BlockList.Empty
            .Block(new BlockEntry(76561199086320654, "Ahno", BlockEntry.Permanent, ""))
            .Block(new BlockEntry(76561199051460419, "Neo", 3, ""));
        Assert.Equal("ANTISLOW_STATE {\"blocked\":[\"76561199051460419\",\"76561199086320654\"]}", BlockListJson.StateLine(list));
    }

    [Fact]
    public void StateLine_EmptyList() => Assert.Equal("ANTISLOW_STATE {\"blocked\":[]}", BlockListJson.StateLine(BlockList.Empty));
}

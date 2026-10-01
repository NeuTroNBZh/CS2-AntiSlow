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

        var back = BlockListJson.Parse(BlockListJson.Serialize(list));

        Assert.Equal(list.Entries.Values.OrderBy(e => e.SteamId), back.Entries.Values.OrderBy(e => e.SteamId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"unexpected\": true}")]
    public void Parse_InvalidContent_GivesAnEmptyList(string content)
    {
        Assert.Empty(BlockListJson.Parse(content).Entries);
    }
}

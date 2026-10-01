using System.Collections.Immutable;
using System.Text.Json;

namespace AntiSlow.Core;

// blocks.json: the block list kept across restarts. An unreadable file means no block rather than a plugin that fails to load.
public static class BlockListJson
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(BlockList list) =>
        JsonSerializer.Serialize(list.Entries.Values.OrderBy(e => e.SteamId).ToList(), Options);

    public static BlockList Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return BlockList.Empty;
        }
        try
        {
            var entries = JsonSerializer.Deserialize<List<BlockEntry>>(content, Options) ?? new List<BlockEntry>();
            return new BlockList(entries
                .Where(e => e is { SteamId: > 0 } && e.PlayerName is not null)
                .Select(e => e with { Reason = e.Reason ?? string.Empty })
                .GroupBy(e => e.SteamId)
                .ToImmutableDictionary(g => g.Key, g => g.Last()));
        }
        catch (JsonException)
        {
            return BlockList.Empty;
        }
    }
}

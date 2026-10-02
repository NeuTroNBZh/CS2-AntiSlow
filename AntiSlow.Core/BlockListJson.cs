using System.Collections.Immutable;
using System.Text.Json;

namespace AntiSlow.Core;

// blocks.json: the block list kept across restarts.
public static class BlockListJson
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    // One console line for remote tools (Retake Deck over RCON): blocked SteamIDs as text, sorted.
    public static string StateLine(BlockList list) =>
        "ANTISLOW_STATE " + JsonSerializer.Serialize(new
        {
            blocked = list.Entries.Keys.Order().Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        });

    public static string Serialize(BlockList list) =>
        JsonSerializer.Serialize(list.Entries.Values.OrderBy(e => e.SteamId).ToList(), Options);

    // False for a corrupt file (truncated write, hand edit): the caller keeps a copy instead of overwriting it.
    public static bool TryParse(string content, out BlockList list)
    {
        list = BlockList.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return true;
        }
        try
        {
            var entries = JsonSerializer.Deserialize<List<BlockEntry>>(content, Options) ?? new List<BlockEntry>();
            list = new BlockList(entries
                .Where(e => e is { SteamId: > 0 } && e.PlayerName is not null)
                .Select(e => e with { Reason = e.Reason ?? string.Empty })
                .GroupBy(e => e.SteamId)
                .ToImmutableDictionary(g => g.Key, g => g.Last()));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

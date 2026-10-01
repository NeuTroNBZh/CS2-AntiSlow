using System.Collections.Immutable;

namespace AntiSlow.Core;

public sealed record BlockEntry(ulong SteamId, string PlayerName, int RoundsRemaining, string Reason)
{
    public const int Permanent = -1;

    public bool IsPermanent => RoundsRemaining == Permanent;

    // "!antislow name 0" or a negative count means permanent, like the original command.
    public static int RoundsFromArgument(int rounds) => rounds > 0 ? rounds : Permanent;
}

// Blocks are keyed by SteamID and survive disconnects: reconnecting must not lift a block.
public sealed record BlockList(ImmutableDictionary<ulong, BlockEntry> Entries)
{
    public static BlockList Empty { get; } = new(ImmutableDictionary<ulong, BlockEntry>.Empty);

    public bool IsBlocked(ulong steamId) => Entries.ContainsKey(steamId);

    public BlockList Block(BlockEntry entry) => new(Entries.SetItem(entry.SteamId, entry));

    public BlockList Unblock(ulong steamId) => new(Entries.Remove(steamId));

    public IReadOnlyList<BlockEntry> FindByName(string fragment) =>
        Entries.Values
            .Where(e => e.PlayerName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<(ulong SteamId, string Name)> Unblocked(IEnumerable<(ulong SteamId, string Name)> online) =>
        online
            .Where(p => !IsBlocked(p.SteamId))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public (BlockList Next, IReadOnlyList<BlockEntry> Expired) RoundEnded()
    {
        var expired = new List<BlockEntry>();
        var next = Entries;
        foreach (var entry in Entries.Values.Where(e => !e.IsPermanent))
        {
            var remaining = entry.RoundsRemaining - 1;
            if (remaining <= 0)
            {
                expired.Add(entry);
                next = next.Remove(entry.SteamId);
            }
            else
            {
                next = next.SetItem(entry.SteamId, entry with { RoundsRemaining = remaining });
            }
        }
        return (new BlockList(next), expired);
    }
}

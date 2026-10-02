using System.Globalization;

namespace AntiSlow.Core;

// "#<userid>" targets one player exactly (remote tools use it so a crafted nickname can never be injected).
public static class PlayerTarget
{
    public static bool IsUserIdSyntax(string argument) => argument.Trim().StartsWith('#');

    // Exact match among the given (connected) players: never an engine index lookup, which wraps around and can
    // return any entity.
    public static T? Resolve<T>(string argument, IEnumerable<T> players, Func<T, int> userIdOf) where T : class =>
        UserIdOf(argument) is { } id ? players.FirstOrDefault(p => userIdOf(p) == id) : null;

    public static int? UserIdOf(string argument)
    {
        var text = argument.Trim();
        return text.Length > 1 && text[0] == '#'
            && int.TryParse(text.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : null;
    }
}

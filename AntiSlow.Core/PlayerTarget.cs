using System.Globalization;

namespace AntiSlow.Core;

// "#<userid>" targets one player exactly (remote tools use it so a crafted nickname can never be injected).
public static class PlayerTarget
{
    public static bool IsUserIdSyntax(string argument) => argument.Trim().StartsWith('#');

    public static int? UserIdOf(string argument)
    {
        var text = argument.Trim();
        return text.Length > 1 && text[0] == '#'
            && int.TryParse(text.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : null;
    }
}

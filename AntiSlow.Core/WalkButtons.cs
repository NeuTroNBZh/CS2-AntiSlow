namespace AntiSlow.Core;

// CS2 sends the walk key as IN_SPEED (0x10000); 0x20000 (IN_WALK) is cleared too in case a client or game update uses it.
public static class WalkButtons
{
    public const ulong Mask = 0x10000UL | 0x20000UL;

    public static ulong Strip(ulong buttons) => buttons & ~Mask;

    // Before writing into native memory: values that are not button masks mean the hooked function is no longer
    // RunCommand (a game update moved it), and writing there could crash the server.
    public static bool LooksLikeButtons(ulong pressed, ulong changed, ulong knownMask) =>
        (pressed & ~knownMask) == 0 && (changed & ~knownMask) == 0;
}

namespace AntiSlow.Core;

// CS2 sends the walk key as IN_SPEED (0x10000); 0x20000 (IN_WALK) is cleared too in case a client or game update uses it.
public static class WalkButtons
{
    public const ulong Mask = 0x10000UL | 0x20000UL;

    public static ulong Strip(ulong buttons) => buttons & ~Mask;
}

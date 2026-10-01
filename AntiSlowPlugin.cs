// AntiSlowPlugin v2 — blocks slow-walk (Shift) for targeted players, by NeuTroNBZh.
//
// The walk key is removed from the player's command inside CPlayer_MovementServices::RunCommand, before the engine
// computes the movement: the player runs (and is heard) instead of walking. Clearing the buttons later in the tick, as
// v1 did, comes after the movement has already been computed and has no effect.

using System.Reflection;
using System.Runtime.InteropServices;
using AntiSlow.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;

namespace AntiSlowPlugin;

public sealed class AntiSlowConfig : BasePluginConfig
{
    // vtable index of CPlayer_MovementServices::RunCommand (CS2 build of October 2026). Update it here if a game update moves it.
    public int RunCommandOffsetLinux { get; set; } = 26;

    // Not validated in game on Windows yet: -1 keeps the hook off there until an offset is confirmed.
    public int RunCommandOffsetWindows { get; set; } = -1;

    // Offset of CInButtonStatePB inside CUserCmd; pressed and changed button masks follow at +0x8 and +0x10.
    public int UserCmdButtonStateOffset { get; set; } = 0x58;

    public string Permission { get; set; } = "@css/kick";
}

public sealed class AntiSlowPlugin : BasePlugin, IPluginConfig<AntiSlowConfig>
{
    public override string ModuleName => "AntiSlowPlugin";
    public override string ModuleVersion => "2.1.0";
    public override string ModuleAuthor => "NeuTroNBZh";
    public override string ModuleDescription => "Blocks slow-walk (Shift) for targeted players.";

    private const int MaxVtableOffset = 200;
    private const int MaxUserCmdOffset = 0x400;
    private static readonly ulong KnownButtons = Enum.GetValues<PlayerButtons>().Aggregate(0UL, (mask, b) => mask | (ulong)b);

    private VirtualFunctionVoid<nint, nint>? _runCommand;
    private bool _hookDisabled;
    private BlockList _blocks = BlockList.Empty;
    private string _storePath = string.Empty;
    private SimpleAdminMenu? _adminMenu;

    public AntiSlowConfig Config { get; set; } = new();

    public void OnConfigParsed(AntiSlowConfig config) => Config = config;

    public override void Load(bool hotReload)
    {
        // Next to the plugin config, so redeploying the plugin folder never erases the blocks.
        _storePath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", ModuleName, "blocks.json"));
        _blocks = LoadBlocks();

        AddCommand("css_antislow", "Blocks a player's slow-walk.", OnAntiSlowCommand);
        AddCommand("css_unantislow", "Unblocks a player's slow-walk.", OnUnAntiSlowCommand);
        AddCommand("css_antislowlist", "Lists players whose slow-walk is blocked.", OnAntiSlowListCommand);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        HookRunCommand();
        Logger.LogInformation("[AntiSlow] v{Version} loaded, {Count} block(s) restored", ModuleVersion, _blocks.Entries.Count);
    }

    public override void Unload(bool hotReload)
    {
        _adminMenu?.Unregister();
        _adminMenu = null;
        _runCommand?.Unhook(OnRunCommand, HookMode.Pre);
        _runCommand = null;
    }

    // SimpleAdmin publishes its API while plugins load: the menu entries are added once every plugin is loaded.
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        if (SimpleAdminMenu.TryFind(Logger) is not { } menu)
        {
            return;
        }
        try
        {
            menu.RegisterCategory(Localizer["antislow.menu.category"], Config.Permission);
            menu.RegisterPlayerList("antislow_block", Localizer["antislow.menu.block"], Config.Permission, BlockChoices, Localizer["antislow.menu.none"]);
            menu.RegisterPlayerList("antislow_unblock", Localizer["antislow.menu.unblock"], Config.Permission, UnblockChoices, Localizer["antislow.menu.none"]);
            _adminMenu = menu;
            Logger.LogInformation("[AntiSlow] Entries added to the CS2-SimpleAdmin menu");
        }
        catch (TargetInvocationException ex)
        {
            Logger.LogWarning(ex.InnerException ?? ex, "[AntiSlow] CS2-SimpleAdmin menu registration failed");
        }
    }

    private IReadOnlyList<(string Label, Action<CCSPlayerController> Choose)> BlockChoices() =>
        _blocks.Unblocked(FindPlayersByName(string.Empty).Select(p => (p.SteamID, p.PlayerName)))
            .Select(p => (p.Name, (Action<CCSPlayerController>)(admin =>
            {
                if (Utilities.GetPlayers().FirstOrDefault(c => c.IsValid && c.SteamID == p.SteamId) is { } target)
                {
                    BlockPlayer(admin, target, BlockEntry.Permanent, string.Empty);
                }
            })))
            .ToList();

    private IReadOnlyList<(string Label, Action<CCSPlayerController> Choose)> UnblockChoices() =>
        _blocks.Entries.Values
            .OrderBy(e => e.PlayerName, StringComparer.OrdinalIgnoreCase)
            .Select(e => (e.PlayerName, (Action<CCSPlayerController>)(admin => UnblockPlayer(admin, e))))
            .ToList();

    // =========================================================================
    //  MOVEMENT HOOK
    // =========================================================================

    private void HookRunCommand()
    {
        var offset = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Config.RunCommandOffsetLinux : Config.RunCommandOffsetWindows;
        if (offset is < 1 or > MaxVtableOffset || Config.UserCmdButtonStateOffset is < 0 or > MaxUserCmdOffset)
        {
            Logger.LogError("[AntiSlow] RunCommand offset {Offset} / button state offset {ButtonOffset} out of range: blocks are recorded but not enforced",
                offset, Config.UserCmdButtonStateOffset);
            return;
        }
        try
        {
            _runCommand = new VirtualFunctionVoid<nint, nint>("CCSPlayer_MovementServices", offset);
            _runCommand.Hook(OnRunCommand, HookMode.Pre);
        }
        catch (Exception ex)
        {
            _runCommand = null;
            Logger.LogError(ex, "[AntiSlow] RunCommand could not be hooked (vtable offset {Offset}): blocks are recorded but not enforced", offset);
        }
    }

    private HookResult OnRunCommand(DynamicHook hook)
    {
        if (_hookDisabled || _blocks.Entries.Count == 0)
        {
            return HookResult.Continue;
        }
        try
        {
            StripWalk(hook);
        }
        catch (Exception ex)
        {
            DisableHook(ex, "an exception was thrown in the movement hook");
        }
        return HookResult.Continue;
    }

    private unsafe void StripWalk(DynamicHook hook)
    {
        var services = hook.GetParam<CCSPlayer_MovementServices>(0);
        var command = hook.GetParam<nint>(1);
        if (command == 0 || services?.Pawn.Value?.Controller.Value?.As<CCSPlayerController>() is not { IsValid: true } player
            || !_blocks.IsBlocked(player.SteamID))
        {
            return;
        }
        var buttons = (ulong*)(command + Config.UserCmdButtonStateOffset + 0x8);
        if (!WalkButtons.LooksLikeButtons(buttons[0], buttons[1], KnownButtons))
        {
            DisableHook(null, $"the player command does not hold button masks ({buttons[0]:X}, {buttons[1]:X}), the offsets no longer match this CS2 build");
            return;
        }
        buttons[0] = WalkButtons.Strip(buttons[0]);
        buttons[1] = WalkButtons.Strip(buttons[1]);
        var states = services.Buttons.ButtonStates;
        for (var i = 0; i < states.Length; i++)
        {
            states[i] = WalkButtons.Strip(states[i]);
        }
    }

    // Never write into native memory again once something looks wrong: a blocked player can walk, the server stays up.
    private void DisableHook(Exception? ex, string reason)
    {
        _hookDisabled = true;
        Logger.LogError(ex, "[AntiSlow] Enforcement disabled: {Reason}. Update the offsets in AntiSlowPlugin.json and reload the plugin.", reason);
        Server.NextFrame(() =>
        {
            _runCommand?.Unhook(OnRunCommand, HookMode.Pre);
            _runCommand = null;
        });
    }

    // =========================================================================
    //  ROUNDS AND PERSISTENCE
    // =========================================================================

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        var (next, expired) = _blocks.RoundEnded();
        if (next == _blocks)
        {
            return HookResult.Continue;
        }
        SetBlocks(next);
        foreach (var entry in expired)
        {
            Server.PrintToChatAll(Localizer["antislow.chat.expired", entry.PlayerName]);
            Logger.LogInformation("[AntiSlow] {Player}: round timer expired, slow-walk allowed again", entry.PlayerName);
        }
        return HookResult.Continue;
    }

    private BlockList LoadBlocks()
    {
        try
        {
            if (!File.Exists(_storePath))
            {
                return BlockList.Empty;
            }
            if (BlockListJson.TryParse(File.ReadAllText(_storePath), out var blocks))
            {
                return blocks;
            }
            var copy = $"{_storePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Copy(_storePath, copy, overwrite: true);
            Logger.LogWarning("[AntiSlow] {File} is corrupt: a copy was kept as {Copy}, starting without blocks", _storePath, copy);
            return BlockList.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "[AntiSlow] {File} could not be read: starting without blocks", _storePath);
            return BlockList.Empty;
        }
    }

    // Written to a temporary file then moved: a crash during the write never leaves a truncated blocks.json.
    private void SetBlocks(BlockList blocks)
    {
        _blocks = blocks;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            var temporary = _storePath + ".tmp";
            File.WriteAllText(temporary, BlockListJson.Serialize(blocks));
            File.Move(temporary, _storePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "[AntiSlow] {File} could not be written: blocks will not survive a restart", _storePath);
        }
    }

    // =========================================================================
    //  COMMANDS
    // =========================================================================

    private void OnAntiSlowCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!CheckPermission(caller))
        {
            return;
        }
        if (command.ArgCount < 2)
        {
            Reply(caller, Localizer["antislow.usage.antislow"]);
            return;
        }
        var nameArg = command.GetArg(1);
        var matches = FindPlayersByName(nameArg);
        if (matches.Count == 0)
        {
            Reply(caller, Localizer["antislow.player.notfound", nameArg]);
            return;
        }
        if (matches.Count > 1)
        {
            ListAmbiguous(caller, Localizer["antislow.player.ambiguous", nameArg], matches.Select(m => m.PlayerName));
            return;
        }

        var rounds = BlockEntry.Permanent;
        var reason = string.Empty;
        if (command.ArgCount >= 3)
        {
            if (int.TryParse(command.GetArg(2), out var parsed))
            {
                rounds = BlockEntry.RoundsFromArgument(parsed);
                reason = JoinArgs(command, 3);
            }
            else
            {
                reason = JoinArgs(command, 2);
            }
        }

        BlockPlayer(caller, matches[0], rounds, reason);
    }

    private void BlockPlayer(CCSPlayerController? admin, CCSPlayerController target, int rounds, string reason)
    {
        SetBlocks(_blocks.Block(new BlockEntry(target.SteamID, target.PlayerName, rounds, reason)));
        var adminName = admin?.PlayerName ?? "Console";
        var roundsSuffix = rounds == BlockEntry.Permanent ? string.Empty : Localizer["antislow.suffix.rounds", rounds].ToString();
        var reasonSuffix = string.IsNullOrEmpty(reason) ? string.Empty : Localizer["antislow.suffix.reason", reason].ToString();
        Server.PrintToChatAll(Localizer["antislow.chat.blocked", adminName, target.PlayerName, roundsSuffix, reasonSuffix]);
        Logger.LogInformation("[AntiSlow] {Admin} blocked {Target} (rounds: {Rounds}, reason: {Reason})",
            adminName, target.PlayerName, rounds == BlockEntry.Permanent ? "permanent" : rounds.ToString(), reason);
    }

    private void OnUnAntiSlowCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!CheckPermission(caller))
        {
            return;
        }
        if (command.ArgCount < 2)
        {
            Reply(caller, Localizer["antislow.usage.unantislow"]);
            return;
        }
        var nameArg = command.GetArg(1);
        var matches = _blocks.FindByName(nameArg);
        if (matches.Count == 0)
        {
            Reply(caller, Localizer["antislow.blocked.notfound", nameArg]);
            return;
        }
        if (matches.Count > 1)
        {
            ListAmbiguous(caller, Localizer["antislow.blocked.ambiguous"], matches.Select(m => m.PlayerName));
            return;
        }
        UnblockPlayer(caller, matches[0]);
    }

    private void UnblockPlayer(CCSPlayerController? admin, BlockEntry entry)
    {
        SetBlocks(_blocks.Unblock(entry.SteamId));
        var adminName = admin?.PlayerName ?? "Console";
        Server.PrintToChatAll(Localizer["antislow.chat.unblocked", adminName, entry.PlayerName]);
        Logger.LogInformation("[AntiSlow] {Admin} unblocked {Target}", adminName, entry.PlayerName);
    }

    private void OnAntiSlowListCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!CheckPermission(caller))
        {
            return;
        }
        if (_blocks.Entries.Count == 0)
        {
            Reply(caller, Localizer["antislow.list.empty"]);
            return;
        }
        Reply(caller, Localizer["antislow.list.header", _blocks.Entries.Count]);
        foreach (var entry in _blocks.Entries.Values.OrderBy(e => e.PlayerName, StringComparer.OrdinalIgnoreCase))
        {
            var roundsText = entry.IsPermanent
                ? Localizer["antislow.list.rounds.permanent"].ToString()
                : Localizer["antislow.list.rounds.count", entry.RoundsRemaining].ToString();
            var reasonSuffix = string.IsNullOrEmpty(entry.Reason) ? string.Empty : Localizer["antislow.suffix.reason", entry.Reason].ToString();
            Reply(caller, Localizer["antislow.list.entry", entry.PlayerName, roundsText, reasonSuffix]);
        }
    }

    // =========================================================================
    //  HELPERS
    // =========================================================================

    private bool CheckPermission(CCSPlayerController? caller)
    {
        if (caller is null || AdminManager.PlayerHasPermissions(caller, Config.Permission))
        {
            return true;
        }
        caller.PrintToChat(Localizer["antislow.noperm"]);
        return false;
    }

    private void ListAmbiguous(CCSPlayerController? caller, string header, IEnumerable<string> names)
    {
        Reply(caller, header);
        foreach (var name in names)
        {
            Reply(caller, Localizer["antislow.player.ambiguous.entry", name]);
        }
        Reply(caller, Localizer["antislow.player.bespecific"]);
    }

    private static void Reply(CCSPlayerController? caller, string message)
    {
        if (caller is null)
        {
            Server.PrintToConsole(message);
            return;
        }
        caller.PrintToChat(message);
    }

    private static List<CCSPlayerController> FindPlayersByName(string fragment) =>
        Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && p.PlayerName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static string JoinArgs(CommandInfo command, int start) =>
        start >= command.ArgCount
            ? string.Empty
            : string.Join(" ", Enumerable.Range(start, command.ArgCount - start).Select(command.GetArg));
}

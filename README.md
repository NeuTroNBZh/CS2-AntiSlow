# AntiSlowPlugin

AntiSlowPlugin is a CounterStrikeSharp plugin for CS2 that blocks slow-walk (Shift) for targeted players.

## Highlights

- Really blocks Shift slow-walk: the walk key is removed from the player's command before the engine computes the movement, so a blocked player runs and is heard
- Temporary blocks (in rounds) and permanent blocks
- Blocks are kept by SteamID across reconnects and server restarts (`blocks.json`)
- Full localization support via JSON files
- .NET 10 / CounterStrikeSharp 1.0.370+

## Requirements

- Counter-Strike 2 dedicated server
- Metamod:Source and CounterStrikeSharp 1.0.370 or later
- .NET 10 SDK (for local build only)

## Installation (From Release)

1. Download the latest release archive.
2. Extract it at the root of your CS2 server.
3. Confirm this path exists after extraction:

```text
addons/counterstrikesharp/plugins/AntiSlowPlugin/
```

4. Restart the server (or reload CounterStrikeSharp plugins).

## Commands

- `css_antislow <player> [rounds] [reason...]`
- `css_unantislow <player>`
- `css_antislowlist`

## Permission

Admins need:

- `@css/kick`

## Configuration

`addons/counterstrikesharp/configs/plugins/AntiSlowPlugin/AntiSlowPlugin.json` is created on first start:

| Key | Default | Meaning |
|---|---|---|
| `Permission` | `@css/kick` | Permission needed for the commands |
| `RunCommandOffsetLinux` | `26` | vtable index of `CPlayer_MovementServices::RunCommand` on Linux |
| `RunCommandOffsetWindows` | `25` | Same on Windows |
| `UserCmdButtonStateOffset` | `0x58` (88) | Offset of the button state inside the player command |

The offsets match the CS2 build of October 2026. If a game update moves them, the plugin logs `RunCommand could not be hooked` (or blocked players can walk again): update the values here, no rebuild needed.

## Localization

Language files are in `lang/`:

- `en.json`
- `fr.json`
- `de.json`
- `es.json`
- `pt-BR.json`
- `ru.json`
- `zh-Hans.json`

## Build

```powershell
dotnet build AntiSlow.slnx -c Release
dotnet test tests/AntiSlow.Core.Tests
```

Build output is generated in:

```text
addons/counterstrikesharp/plugins/AntiSlowPlugin/
```

## Release Layout

```text
addons/
  counterstrikesharp/
    plugins/
      AntiSlowPlugin/
        AntiSlowPlugin.dll
        AntiSlowPlugin.deps.json
        AntiSlow.Core.dll
        lang/
          en.json
          fr.json
          de.json
          es.json
          pt-BR.json
          ru.json
          zh-Hans.json
```

## Compatibility

Current branch is standalone and does not require external CS2-SimpleAdmin API DLLs to compile.

## Author

- NeuTroNBZh

## License

MIT. See LICENSE.

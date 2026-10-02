# Changelog

All notable changes to this project are documented in this file.

## 2.3.0 - 2026-10-02

- `css_antislow_state` (server console and RCON only) prints the blocked SteamIDs as one JSON line, so remote tools (Retake Deck) can show who is blocked and offer "unblock".

## 2.2.0 - 2026-10-02

- `css_antislow` and `css_unantislow` accept `#<userid>` to target one player exactly. An argument starting with `#` is never matched as a nickname, so remote tools (Retake Deck over RCON) never have to pass a nickname to the server console.

## 2.1.0 - 2026-10-01

- CS2-SimpleAdmin integration: an **AntiSlow** category in `!admin` with "Block a player's slow-walk" (connected players not blocked yet) and "Unblock a player's slow-walk" (blocked players). Optional and resolved at runtime: without SimpleAdmin, or with an incompatible version, the plugin works as before and logs why.
- Menu labels in every language file (`antislow.menu.*`).

## 2.0.0 - 2026-10-01

- **Slow-walk is really blocked now.** The walk key is removed from the player's command inside `CPlayer_MovementServices::RunCommand`, before the engine computes the movement: a blocked player runs (and is heard) instead of walking. v1 cleared the buttons later in the tick, after the movement was already computed, so it had no effect. Verified in game on a CS2 server (October 2026 build).
- Blocks are kept by SteamID across disconnects and saved in `configs/plugins/AntiSlowPlugin/blocks.json` (written atomically; a corrupt file is copied aside and reported): reconnecting, restarting or redeploying no longer lifts a block.
- Safety: before writing into the player command, the plugin checks it holds button masks; otherwise (game update) it disables enforcement and logs it instead of risking a crash. Windows enforcement is off by default until validated.
- New config `configs/plugins/AntiSlowPlugin/AntiSlowPlugin.json`: `RunCommandOffsetLinux` / `RunCommandOffsetWindows` (vtable index, 26 / -1), `UserCmdButtonStateOffset` (0x58), `Permission` (`@css/kick`). If the hook cannot be installed after a game update, the plugin logs an error and keeps working without enforcement until the offsets are updated.
- .NET 10, CounterStrikeSharp pinned to 1.0.370, pure logic moved to `AntiSlow.Core` with unit tests run by CI.
- Every command answers in the server console when run from it.

## 1.1.1 - 2026-04-29

- Build now works without external CS2-SimpleAdmin API DLL dependency.
- Kept full AntiSlow core functionality and localization support.
- Standardized release-ready output structure for direct server deployment.

## 1.1.0 - 2026-04-29

- Migrated chat messages to CounterStrikeSharp Localizer.
- Added multilingual language files:
  - en
  - fr
  - de
  - es
  - pt-BR
  - ru
  - zh-Hans
- Configured output path to release-ready plugin structure.

## 1.0.0

- Initial AntiSlow plugin implementation.

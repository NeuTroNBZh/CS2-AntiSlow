# Changelog

All notable changes to this project are documented in this file.

## 2.0.0 - 2026-10-01

- **Slow-walk is really blocked now.** The walk key is removed from the player's command inside `CPlayer_MovementServices::RunCommand`, before the engine computes the movement: a blocked player runs (and is heard) instead of walking. v1 cleared the buttons later in the tick, after the movement was already computed, so it had no effect. Verified in game on a CS2 server (October 2026 build).
- Blocks are kept by SteamID across disconnects and saved in `blocks.json` next to the plugin: reconnecting or restarting the server no longer lifts a block.
- New config `configs/plugins/AntiSlowPlugin/AntiSlowPlugin.json`: `RunCommandOffsetLinux` / `RunCommandOffsetWindows` (vtable index, 26 / 25), `UserCmdButtonStateOffset` (0x58), `Permission` (`@css/kick`). If the hook cannot be installed after a game update, the plugin logs an error and keeps working without enforcement until the offsets are updated.
- .NET 10, CounterStrikeSharp pinned to 1.0.370, pure logic moved to `AntiSlow.Core` with unit tests run by CI.
- `!antislowlist` and the other commands answer in the server console when run from it.

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

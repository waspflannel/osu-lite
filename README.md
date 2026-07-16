# osu! lite

A local-first, cross-platform osu!standard desktop player.

> **Current status:** `final-trim` is a working merge checkpoint. Major offline, fixed-skin, fixed-ruleset, mod, editor/export, intro, and stable-import removals are in place. Notification drawer replacement, exact settings/input cleanup, dependency cleanup, and the resource repack are still in progress. See [final-trim.md](final-trim.md) for the binding scope and current checkpoint.

## Supported platforms

- Windows 10+
- macOS 12+
- Linux (x64)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)

## Building from source

```shell
git clone https://github.com/waspflannel/osu-lite
cd osu-lite
dotnet build osu.Desktop -c Debug
```

Run with:

```shell
dotnet run --project osu.Desktop
```

## What is osu! lite?

osu! lite is a trimmed build of the osu! game client that focuses on being a local desktop-only player. It supports:

- Local `.osz` beatmap import, playback, and deletion
- Local `.osr` replay import, playback, export
- Unmodded solo play and Ctrl+Enter autoplay
- Local score history and in-game leaderboard
- Difficulty and performance calculation
- Keyboard, mouse, tablet/pen input, with remaining joystick/mobile residue scheduled for removal
- Six-section settings (General, Input, Gameplay, Audio, Graphics, Maintenance); the final Data allowlist is still in progress
- One bundled Kanna user skin with beatmap-local overrides
- Localisation for retained UI

### What osu! lite does not support

- Online/network features, API endpoints, or web requests
- Selectable gameplay mods (only unmodded play and autoplay)
- Dynamic rulesets (only osu!standard)
- Beatmap editor or external editing
- Beatmap export
- Replay analysis
- Startup intro sequences
- Stable osu! data migration
- Mod-bearing replay import

The notification drawer, joystick/gamepad paths, mobile/touch residue, and resource repack are not yet complete removals; they remain documented work in [final-trim.md](final-trim.md).

## External browser destinations

osu! lite opens an external browser for exactly three fixed destinations:

- [Report an issue](https://github.com/waspflannel/osu-lite/issues/new/choose)
- [OpenTabletDriver tablet list](https://opentabletdriver.net/Tablets)
- [OpenTabletDriver FAQ](https://opentabletdriver.net/Wiki/FAQ/General)

## Licence

osu! lite is based on [osu!](https://github.com/ppy/osu), which is licensed under the MIT licence.
See [LICENCE](LICENCE) for details.

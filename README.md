# osu! lite

A local-first, cross-platform osu!standard desktop player.

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
- Keyboard, mouse, and tablet/pen input
- Six-section settings (General, Input, Gameplay, Audio, Graphics, Data)
- One bundled Kanna user skin with beatmap-local overrides
- Localisation for retained UI

### What osu! lite does not support

- Online/network features, API endpoints, or web requests
- Selectable gameplay mods (only unmodded play and autoplay)
- Dynamic rulesets (only osu!standard)
- Beatmap editor or external editing
- Beatmap export
- Notification drawer (transient toasts only)
- Replay analysis
- Joystick/gamepad input
- Startup intro sequences
- Mobile platforms (Android/iOS)
- Stable osu! data migration
- Mod-bearing replay import

## External browser destinations

osu! lite opens an external browser for exactly three fixed destinations:

- [Report an issue](https://github.com/waspflannel/osu-lite/issues/new/choose)
- [OpenTabletDriver tablet list](https://opentabletdriver.net/Tablets)
- [OpenTabletDriver FAQ](https://opentabletdriver.net/Wiki/FAQ/General)

## Licence

osu! lite is based on [osu!](https://github.com/ppy/osu), which is licensed under the MIT licence.
See [LICENCE](LICENCE) for details.

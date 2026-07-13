# osu! lite — Codebase Map

> Generated from `ppy/osu` at `C:\osu`. Do not modify source code; this is a reference document for trimming.

---

## 1. Solution / Project Overview

| Project | Path | Purpose | Tag |
|---------|------|---------|-----|
| `osu.Game` | `osu.Game/` | Core game library; all screens, gameplay, beatmaps, scoring, skinning, DI wiring | **MODIFY** (trim) |
| `osu.Game.Rulesets.Osu` | `osu.Game.Rulesets.Osu/` | osu! standard ruleset (circles, sliders, spinners) | **KEEP** |
| `osu.Game.Rulesets.Taiko` | `osu.Game.Rulesets.Taiko/` | Taiko drum ruleset | **CUT** |
| `osu.Game.Rulesets.Catch` | `osu.Game.Rulesets.Catch/` | Catch-the-beat ruleset | **CUT** |
| `osu.Game.Rulesets.Mania` | `osu.Game.Rulesets.Mania/` | Mania (keyboard) ruleset | **CUT** |
| `osu.Desktop` | `osu.Desktop/` | Windows/macOS/Linux desktop entry point | **KEEP** |
| `osu.Android` | `osu.Android/` | Android mobile head | **CUT** |
| `osu.iOS` | `osu.iOS/` | iOS mobile head | **CUT** |
| `osu.Game.Tournament` | `osu.Game.Tournament/` | Tournament client (separate executable) | **CUT** |
| `osu.Game.Benchmarks` | `osu.Game.Benchmarks/` | Performance benchmarks | **CUT** |
| `osu.Game.Tests` | `osu.Game.Tests/` | Test project | **CUT** |
| `osu.Game.Rulesets.*.Tests` | `osu.Game.Rulesets.*.Tests/` | Ruleset-specific tests | **CUT** |
| `osu.Game.Rulesets.*.Tests.Android/iOS` | — | Mobile test projects | **CUT** |
| `osu.Game.Tournament.Tests` | — | Tournament tests | **CUT** |
| `Templates/` | `Templates/Rulesets/` | Ruleset scaffolding templates | **CUT** |

---

## 2. High-Level Architecture

### 2.1 Startup Flow

```
Program.Main()                          [osu.Desktop/Program.cs:34]
  ├── setupVelopack()                   [osu.Desktop/Program.cs:39] — auto-updater
  ├── Host.GetSuitableDesktopHost()     [osu.Desktop/Program.cs:111] — creates DesktopGameHost
  ├── host.Run(new OsuGameDesktop())    [osu.Desktop/Program.cs:144]
  │     └── OsuGameDesktop : OsuGame    [osu.Desktop/OsuGameDesktop.cs:29]
  │           └── OsuGame : OsuGameBase [osu.Game/OsuGame.cs:96]
  │                 └── OsuGameBase     [osu.Game/OsuGameBase.cs:78] — bootstraps DI, managers
```

The framework calls `LoadComplete()` on `OsuGame` after construction + DI load. The screen stack is:

1. **Loader** (startup) → `Screens/Loader.cs:24`
   - Pushes **IntroScreen** (circles/welcome/triangles) → `Screens/Menu/IntroScreen.cs`
   - Intro pushes **MainMenu** → `Screens/Menu/MainMenu.cs:50`
2. **MainMenu** has a `ButtonSystem` that navigates to:
   - **SoloSongSelect** (play) → `Screens/Select/SoloSongSelect.cs:28`
   - **Multiplayer / Playlists / DailyChallenge** screens → `Screens/OnlinePlay/`
   - **EditorLoader** (edit) → `Screens/Edit/EditorLoader.cs`
3. **SoloSongSelect** → `PlayerLoader` → `SoloPlayer` → `SoloResultsScreen`

### 2.2 Ruleset Discovery / Loading

`RealmRulesetStore` (`Rulesets/RealmRulesetStore.cs:17`) scans loaded assemblies for `Ruleset` subclasses. At construction it:

1. Loads all assemblies from `LoadedAssemblies` (reflection)
2. Activates each `Ruleset` instance, checks `ILegacyRuleset` interface, creates/stores `RulesetInfo` records in Realm
3. Each ruleset DLL registers its type for `Type.GetType(instantiationInfo)` later
4. Tests compatibility by calling `CreateAllMods()`, `CreateIcon()`, `CreateResourceStore()`, `CreateBeatmapConverter()`

**Key:** `RulesetStore.AvailableRulesets` is populated in `RealmRulesetStore.prepareDetachedRulesets()` at `RealmRulesetStore.cs:32`. This enumerates **every ruleset DLL found**. To trim: remove non-Osu assemblies from the load path.

### 2.3 Dependency Injection Setup

`OsuGameBase.load()` (`OsuGameBase.cs:264`) wires everything:

| Dependency | Type / Bind | Line |
|-----------|-------------|------|
| `RealmAccess` | Database connection | :281 |
| `RulesetStore` / `IRulesetStore` | `RealmRulesetStore` | :283-284 |
| `Storage` | `OsuStorage` | :288 |
| `SkinManager` / `ISkinSource` | `SkinManager` | :303-304 |
| `API` / `IAPIProvider` | `APIAccess` | :319 |
| `ScoreManager` | `ScoreManager` | :326 |
| `BeatmapManager` / `IWorkingBeatmapCache` | `BeatmapManager` | :328-330 |
| `BeatmapDownloader` | `BeatmapModelDownloader` | :331 |
| `ScoreDownloader` | `ScoreModelDownloader` | :332 |
| `SpectatorClient` | `OnlineSpectatorClient` | :339 |
| `MultiplayerClient` | `OnlineMultiplayerClient` | :340 |
| `MetadataClient` | `OnlineMetadataClient` | :341 |
| `RulesetConfigCache` | `RulesetConfigCache` | :353 |
| `LeaderboardManager` | `LeaderboardManager` | :377 |
| `MusicController` | `MusicController` | :394 |
| `KeyBindingStore` | `RealmKeyBindingStore` | :428 |

The global bindables `Ruleset`, `SelectedMods`, `Beatmap`, `AvailableMods` are cached at class level.

**Every cut item touches one or more of these DI cache lines.**

### 2.4 Screen Navigation Flow (simplified)

```
Loader → IntroScreen → MainMenu
                         ├── SoloSongSelect → PlayerLoader → SoloPlayer → SoloResultsScreen → MainMenu
                         ├── Multiplayer (CUT)
                         ├── Playlists (CUT)
                         ├── DailyChallenge (CUT)
                         ├── EditorLoader → Editor (CUT)
                         ├── SettingsOverlay (MODIFY)
                         ├── UserProfileOverlay (CUT)
                         ├── BeatmapListingOverlay (CUT)
                         ├── RankingsOverlay (CUT)
                         ├── ChatOverlay (CUT)
                         ├── BeatmapSetOverlay (CUT)
                         ├── SkinEditorOverlay (CUT)
                         └── ... more overlays
```

---

## 3. Directory Guide — `osu.Game/`

| Directory | Purpose |
|-----------|---------|
| `Audio/` | Audio engine wrappers, effects, samples |
| `Beatmaps/` | Beatmap model, manager, import pipeline, difficulty cache, formats, control points |
| `Collections/` | Beatmap collections (managed via Realm) |
| `Configuration/` | Config manager (`OsuConfigManager`), setting enums, session statics |
| `Database/` | Realm access, model managers, import/export base classes |
| `Extensions/` | C# extension helpers |
| `Graphics/` | Custom drawables, colours, cursors, UI toolkit |
| `Input/` | Bindings, global actions, key combo, tablet/mouse settings |
| `IO/` | File abstractions, archive readers |
| `IPC/` | Inter-process communication channels |
| `Localisation/` | Localisation strings, language configuration |
| `Models/` | Realm data model classes |
| `Online/` | **Entire online layer** — API, chat, multiplayer, spectator, leaderboards, rooms, matchmaking, metadata |
| `Overlays/` | All overlay UIs — settings, mods, skin editor, chat, beatmap listing, dashboard, news, user profiles, wiki, changelog, notifications, toolbar, volume |
| `Performance/` | Performance metrics |
| `Replays/` | Replay handling |
| `Rulesets/` | Ruleset base classes, mod infrastructure, scoring, judgement, UI, configuration |
| `Scoring/` | Score model, manager, import, legacy scoring |
| `Screens/` | All screens — menu, play, select, edit, online play, ranking, utility, backgrounds |
| `Seasonal/` | Seasonal/holiday UI decorations |
| `Skinning/` | Skin engine — managers, legacy skin loading, skin components, default skins |
| `Storyboards/` | Storyboard support |
| `Tests/` | Test base classes |
| `Updater/` | Update manager |
| `Users/` | User model and statistics |
| `Utils/` | Various utilities |

---

## 4. Cut-by-Cut Breakdown

### 4.1 Rulesets (Taiko, Catch, Mania)

**Owning projects:**
- `osu.Game.Rulesets.Taiko/` → `osu.Game.Rulesets.Taiko.csproj`
- `osu.Game.Rulesets.Catch/` → `osu.Game.Rulesets.Catch.csproj`
- `osu.Game.Rulesets.Mania/` → `osu.Game.Rulesets.Mania.csproj`

**How they're discovered:**
- `RulesetStore` scans loaded assemblies. In `OsuGameBase.cs:283`, `RealmRulesetStore` is created which loads **all** `Ruleset` subclass assemblies. The ruleset assemblies are referenced by `osu.Desktop.csproj` (project references).
- Each ruleset registers into the `AvailableRulesets` list in Realm at `RealmRulesetStore.cs:32-124`.

**Reference sites in `osu.Game`:**

| File | Line | What references |
|------|------|-----------------|
| `OsuGameBase.cs` | :283-284 | `new RealmRulesetStore(...)` — scans ALL assemblies |
| `OsuGameBase.cs` | :429 | `RulesetStore.AvailableRulesets.First()` — could pick non-Osu |
| `OsuGame.cs` | :423-434 | Preferred ruleset from config, iterates `AvailableRulesets` |
| `OsuGameBase.cs` | :695-759 | `onRulesetChanged()` — creates ruleset instance, gets mods for type |
| `Rulesets/RealmRulesetStore.cs` | :38-42 | Loads ALL `Ruleset` assemblies from `LoadedAssemblies` |
| `Rulesets/RulesetStore.cs` | (base) | `LoadedAssemblies` collects from app domain |
| `Screens/Select/SongSelect.cs` | :352-363 | `CreateFooterButtons()` passes `Mods` / `Ruleset` bindables |
| `Screens/Select/SoloSongSelect.cs` | :101-106 | Calls `Ruleset.Value.CreateInstance().GetAutoplayMod()` |
| `Screens/Play/PlayerLoader.cs` | :50-870 | Uses `Ruleset.Value` to create drawable ruleset |
| `Beatmaps/BeatmapConverter.cs` | — | Uses `Ruleset.CreateBeatmapConverter()` via conversion |
| `Configuration/OsuConfigManager.cs` | :41 | Default ruleset setting |

**Coupling risk: MEDIUM**
- Removing the three extra ruleset project references from `osu.Desktop.csproj` is easy
- The `RealmRulesetStore` scanning loops over ALL loaded assemblies — if the DLLs aren't loaded / referenced, they won't appear
- The `onRulesetChanged()` handler at `OsuGameBase.cs:695` attempts to create mods for each ruleset — removing the ruleset DLLs means they just won't be in `AvailableRulesets`
- Main coupling risk: the `RulesetInfo` realm records still exist in the database for non-Osu rulesets (from prior runs). Need cleanup or migration.

### 4.2 Online Layer (Everything)

**Owning files (all in `osu.Game/`):**

| Sub-area | Location | Key files |
|----------|----------|-----------|
| API core | `Online/API/` | `APIAccess.cs`, `IAPIProvider.cs`, `APIRequest.cs`, `OAuth.cs` |
| Chat | `Online/Chat/` | `ChannelManager.cs`, `ChatOverlay.cs`, `MessageNotifier.cs` |
| Multiplayer | `Online/Multiplayer/` | `OnlineMultiplayerClient.cs`, `MultiplayerClient.cs` |
| Spectator | `Online/Spectator/` | `OnlineSpectatorClient.cs`, `SpectatorClient.cs` |
| Leaderboards | `Online/Leaderboards/` | `LeaderboardManager.cs`, `LeaderboardScore.cs` |
| Rooms | `Online/Rooms/` | `Room.cs`, all request types |
| Matchmaking | `Online/Matchmaking/` | Matchmaking client/server/requests |
| Metadata | `Online/Metadata/` | `OnlineMetadataClient.cs`, `MetadataClient.cs` |
| Solo | `Online/Solo/` | `CreateSoloScoreRequest.cs`, `SubmitSoloScoreRequest.cs` |
| Notifications | `Online/` | `OnlineStatusNotifier.cs`, `FriendPresenceNotifier.cs` |
| Screens | `Screens/OnlinePlay/` | Multiplayer, Playlists, DailyChallenge, Lounge, Match |

**Key DI references (all in `OsuGameBase.cs`):**

| Line | What |
|------|------|
| :319 | `API ??= new APIAccess(...)` — cached as `IAPIProvider` |
| :326 | `ScoreManager` — takes `API` parameter |
| :328 | `BeatmapManager` — takes `API` parameter |
| :331 | `BeatmapDownloader = new BeatmapModelDownloader(BeatmapManager, API)` |
| :332 | `ScoreDownloader = new ScoreModelDownloader(ScoreManager, API)` |
| :339 | `SpectatorClient = new OnlineSpectatorClient(endpoints)` |
| :340 | `MultiplayerClient = new OnlineMultiplayerClient(endpoints)` |
| :341 | `metadataClient = new OnlineMetadataClient(endpoints)` |
| :343 | `BeatmapOnlineChangeIngest(beatmapUpdater, realm, metadataClient)` |
| :377 | `LeaderboardManager` |
| :381-386 | All clients added to content hierarchy |

**Reference sites in OsuGame.cs (`LoadComplete`):**

| Line | What |
|------|------|
| :1220-1232 | Overlay creation: `beatmapListing`, `dashboard`, `news`, `userProfile`, `beatmapSetOverlay`, `wikiOverlay`, `changelogOverlay`, `rankingsOverlay`, `LoginOverlay`, `AccountCreationOverlay`, `ChatOverlay` |
| :1224 | `ChannelManager(API)` |
| :1256 | `OnlineStatusNotifier` |
| :1257 | `FriendPresenceNotifier` |

**Reference sites in MainMenu (`Screens/Menu/MainMenu.cs`):**

| Line | What |
|------|------|
| :78 | `IAPIProvider api` resolved |
| — | ButtonSystem with `OnMultiplayer`, `OnPlaylists`, `OnDailyChallenge`, `OnBeatmapListing` |
| :220-268 | Multiplayer/Playlists/DailyChallenge buttons check `api.State` |

**Reference sites in SongSelect:**

| Line | What |
|------|------|
| :151 | `IAPIProvider api` resolved |
| :167 | `onlineLookupSource = new RealmPopulatingOnlineLookupSource()` |
| :182 | `onlineLookupSource` added to hierarchy |

**Reference sites in SubmittingPlayer (`Screens/Play/SubmittingPlayer.cs`):**

| Line | What |
|------|------|
| :41 | `IAPIProvider api` resolved |
| :44 | `SpectatorClient spectatorClient` resolved |
| :56-63 | Score token & submission via API |

**Reference sites in Results screen (`Screens/Ranking/SoloResultsScreen.cs`):**

| Line | What |
|------|------|
| :26-29 | `IAPIProvider api`, `LeaderboardManager leaderboardManager` resolved |
| :50-162 | Fetches leaderboard scores via `leaderboardManager` |

**Coupling risk: HIGH**
- The online layer is **deeply interwoven**. `API` is injected into `BeatmapManager`, `ScoreManager`, `BeatmapDownloader`, `ScoreDownloader`, `LeaderboardManager`, `ChannelManager`
- `SpectatorClient`, `MultiplayerClient`, `MetadataClient` are cached in DI and added to the scene graph
- Beatmap import has `performOnlineLookups: true` at `OsuGameBase.cs:328`
- Score submission chain: `SoloPlayer` → `SubmittingPlayer` → creates token → submits via API
- `LeaderboardManager` is referenced by `SoloResultsScreen` for fetching and displaying scores
- Many overlays assume the API exists (login, notifications, online status)
- `OnlineMenuBanner` in `Screens/Menu/` relies on API state
- `MetadataClient` is referenced by `BeatmapOnlineChangeIngest` at `OsuGameBase.cs:343`

### 4.3 Beatmap Editor

**Owning files:** `Screens/Edit/` (46 files) — `Editor.cs`, `EditorLoader.cs`, `EditorBeatmap.cs`, `Compose/`, `Design/`, `Timing/`, `Setup/`, `Verify/`, `GameplayTest/`

**Reference sites:**

| File | Line | What |
|------|------|------|
| `OsuGame.cs` | :660-671 | `HandleTimestamp()` — checks `ScreenStack.CurrentScreen is Editor` |
| `Screens/Menu/ButtonSystem.cs` | :170-174 | "Edit" button with `OnEditBeatmap` action |
| `Screens/Menu/MainMenu.cs` | :50-512 | Links `OnEditBeatmap` to editor screen push |
| `Screens/Select/SoloSongSelect.cs` | :66 | `Edit(beatmap)` method → `new EditorLoader()` |
| `Screens/Select/SongSelect.cs` | :108-1292 | Beatmap right-click → "Edit" action |
| `Rulesets/Edit/` | Each ruleset | Ruleset-specific editor components in each ruleset project |

**Coupling risk: MEDIUM**
- The editor is mostly self-contained under `Screens/Edit/`
- Entry points: main menu "Edit" button + song select context menu → remove those two triggers
- `HandleTimestamp` check in `OsuGame.cs` is a small reference
- Ruleset-specific editor files in each ruleset project (trivially removed with the ruleset)

### 4.4 Skin Editor + Skin Selector

**Owning files:** `Overlays/SkinEditor/` (13 files) — `SkinEditorOverlay.cs`, `SkinEditor.cs`, `SkinBlueprint.cs`, etc.

**Reference sites:**

| File | Line | What |
|------|------|------|
| `OsuGame.cs` | :146 | `SkinEditorOverlay skinEditor` field |
| `OsuGame.cs` | :1232 | `loadComponentSingleFile(skinEditor = new SkinEditorOverlay(...)` |
| `OsuGame.cs` | :1615-1617 | `GlobalAction.ToggleSkinEditor` → `skinEditor.ToggleVisibility()` |
| `OsuGame.cs` | :1636-1658 | `RandomSkin`, `NextSkin`, `PreviousSkin` global actions |
| `OsuGame.cs` | :1817 | `skinEditor.SetTarget(newOsuScreen)` in `ScreenChanged()` |
| `Screens/Menu/ButtonSystem.cs` | :175 | "Skin Editor" button in edit menu |
| `Screens/Menu/MainMenu.cs` | :35 | `SkinEditor` imported |
| `Skinning/SkinManager.cs` | :54-56 | `CurrentSkin` and `CurrentSkinInfo` bindables |
| `OsuGame.cs` | :440-446 | Skin configuration → `SkinManager.SetSkinFromConfiguration()` |
| `OsuGame.cs` | :1069 | `SkinManager.PostNotification = n => Notifications.Post(n)` |
| `OsuGame.cs` | :1070 | `SkinManager.PresentImport = items => PresentSkin(...)` |

**Coupling risk: LOW-MEDIUM**
- Skin editor is an overlay; remove its creation in `OsuGame.LoadComplete()` and the keyboard shortcut
- Skin *selection* (changing skins) is part of `SkinManager` which KEEPS — but we lock to one hardcoded skin. The `SelectRandomSkin`, `SelectNextSkin`, `SelectPreviousSkin` global actions can be removed
- `SkinManager.CurrentSkinInfo` bindable stays but will always point to the default skin
- The skin selector in Settings (`Settings/Sections/SkinSection.cs`) would also need removal

### 4.5 Mod System (Entirely)

**Owning files:**

| Sub-area | Location |
|----------|----------|
| Mod base classes | `Rulesets/Mods/` (71 files) — `Mod.cs`, `ModAutoplay.cs`, `ModDoubleTime.cs`, etc. |
| Mod select overlay | `Overlays/Mods/` (32 files) — `ModSelectOverlay.cs`, `UserModSelectOverlay.cs`, `ModColumn.cs`, etc. |
| Per-ruleset mods | `Rulesets.Osu/Mods/`, `Rulesets.Taiko/Mods/`, etc. |

**Reference sites:**

| File | Line | What |
|------|------|------|
| `OsuGameBase.cs` | :199-201 | `SelectedMods` bindable (Cached) |
| `OsuGameBase.cs` | :206 | `AvailableMods` bindable |
| `OsuGameBase.cs` | :721-756 | `onRulesetChanged()` — iterates mod types, converts mods |
| `OsuGame.cs` | :459 | `SelectedMods.BindValueChanged(modsChanged)` |
| `OsuGame.cs` | :981-992 | `modsChanged()` validation |
| `Screens/Select/SongSelect.cs` | :108 | `ModSelectOverlay modSelectOverlay` field |
| `Screens/Select/SongSelect.cs` | :315 | `LoadComponent(modSelectOverlay = CreateModSelectOverlay())` |
| `Screens/Select/SongSelect.cs` | :331-334 | `CreateModSelectOverlay()` → `new UserModSelectOverlay` |
| `Screens/Select/SongSelect.cs` | :351-363 | `FooterButtonMods` in footer buttons |
| `Screens/Select/SongSelect.cs` | :387 | `overlayManager?.RegisterBlockingOverlay(modSelectOverlay)` |
| `Screens/Select/FooterButtonMods.cs` | :37-436 | Full mod display bar in song select footer |
| `Screens/Select/SoloSongSelect.cs` | :101-145 | `OnStart()` — mods cloning, ctrl+enter autoplay |
| `Screens/Play/PlayerLoader.cs` | — | Applies mods during gameplay creation |
| `Screens/Ranking/ResultsScreen.cs` | — | Mods displayed on rankings |
| `Rulesets/UI/DrawableRuleset.cs` | — | Creates mods and applies them |
| `Overlays/Settings/Sections/GameplaySection.cs` | — | Settings like "mod select hotkey style" |

**Coupling risk: HIGH**
- The mod system is **extremely coupled**. `SelectedMods` is a cached DI bindable used everywhere:
  - Song select footer UI
  - Player loader → gameplay mod application
  - Results screen displays mods
  - Ruleset creation iterates `GetModsFor()` every ruleset type
  - Configuration stores mod hotkey styles
  - `ModSettingChangeTracker` watches mod settings
- Removing mods means:
  - `Ruleset.GetModsFor()` is no longer called
  - `OsuGameBase.cs:721-756` (`onRulesetChanged`) mod iteration removed
  - `FooterButtonMods` and `ModSelectOverlay` entirely removed
  - `SoloSongSelect.OnStart()` mod cloning removed (no mods to clone)
  - `PlayerLoader` mod application removed
  - All per-ruleset `Mods/` directories in each ruleset project removed

### 4.6 Mobile Heads

**Owning projects:**
- `osu.Android/` → `osu.Android.csproj`
- `osu.iOS/` → `osu.iOS.csproj`

**Reference sites:** Minimal in the core library — mobile projects simply reference `osu.Game` and create an `OsuGame` instance. The core has some mobile-conditional code:
| File | Line | What |
|------|------|------|
| `OsuGameBase.cs` | :274 | Special case for Android builds (DLL reading) |
| `OsuGameBase.cs` | :1349 | `RuntimeInfo.IsMobile` check for UI scaling migration |
| `OsuGameBase.cs` | :655 | `RuntimeInfo.IsDesktop` check |
| `OsuConfigManager.cs` | :1348 | Mobile UI scale default |

**Coupling risk: LOW**
- Mobile heads are separate projects; just don't build them
- The `RuntimeInfo.IsMobile` guards in the core are for migration paths — can be left or removed

### 4.7 Settings UI Panels

**Owning files:** `Overlays/Settings/Sections/` (22 entries)

Full directory:
```
Audio/
AudioSection.cs
DebugSection.cs
DebugSettings/
Gameplay/
GameplaySection.cs
General/
GeneralSection.cs
Graphics/
GraphicsSection.cs
Input/
InputSection.cs
Maintenance/
MaintenanceSection.cs
Online/
OnlineSection.cs
RulesetSection.cs
SkinSection.cs
UserInterface/
UserInterfaceSection.cs
```

**Reference sites:**

| File | Line | What |
|------|------|------|
| `OsuGame.cs` | :207 | `SettingsOverlay Settings` field |
| `OsuGame.cs` | :1227 | `loadComponentSingleFile(Settings = new SettingsOverlay(), ...)` |
| `Screens/Menu/MainMenu.cs` | :112 | `OnSettings` callback |
| `Screens/Menu/ButtonSystem.cs` | :112 | Settings button in top-level menu |
| `OsuGameBase.cs` | :648-685 | `CreateSettingsSubsectionFor(InputHandler)` — creates per-platform settings |

**Coupling risk: LOW-MEDIUM** (per-section)
- The settings overlay is a container. Individual sections can be removed from the section list
- `OnlineSection` couples to API; remove it
- `SkinSection` couples to skin selector; remove it (skin locked to default)
- `GameplaySection` has mod-related settings; remove mod-adjacent items
- `MaintenanceSection` may reference online (delete all scores online, etc.)
- Settings **header/footer infrastructure** stays; just trim sections
- `RulesetSection` switches rulesets — since only osu! standard exists, simplify or remove

---

## 5. Keep-List Locations

### Beatmap Import Pipeline

| File | Line | Role |
|------|------|------|
| `Beatmaps/BeatmapManager.cs` | :39 | `ModelManager<BeatmapSetInfo>`, implements `IModelImporter<BeatmapSetInfo>` |
| `Beatmaps/BeatmapImporter.cs` | — | Actual `.osz` import logic (extends `RealmArchiveModelImporter`) |
| `Database/RealmArchiveModelImporter.cs` | — | Base class for archive import into Realm |
| `Database/ModelManager.cs` | — | Generic model management |
| `Database/ImportParameters.cs` | — | Import options |
| `Database/ImportTask.cs` | — | Import task model |
| `OsuGameBase.cs` | :363 | `RegisterImportHandler(BeatmapManager)` |
| `OsuGameBase.Importing.cs` | :28-56 | `Import()` dispatches to registered handlers by extension |
| `Scoring/ScoreManager.cs` | — | `.osr` import (keep for local replays) |
| `Skinning/SkinManager.cs` | — | `.osk` import (keep if needed) |

### Realm Database Storage

| File | Line | Role |
|------|------|------|
| `Database/RealmAccess.cs` | — | Realm DB connection, thread safety, migrations |
| `Database/RealmFileStore.cs` | — | File-level storage (beatmap assets in `files/`) |
| `OsuGameBase.cs` | :281 | `new RealmAccess(Storage, CLIENT_DATABASE_FILENAME, ...)` |

### Song Select

| File | Line | Role |
|------|------|------|
| `Screens/Select/SongSelect.cs` | :61 | Abstract base — carousel, filter, beatmap browsing |
| `Screens/Select/SoloSongSelect.cs` | :28 | Concrete solo play song select |
| `Screens/Select/FilterControl.cs` | :35 | Sorting, filtering, search |
| `Screens/Select/BeatmapCarousel.cs` | — | Beatmap list UI |

### Play Loop

| File | Line | Role |
|------|------|------|
| `Screens/Play/PlayerLoader.cs` | :51 | Loads and pushes Player after pre-checks |
| `Screens/Play/Player.cs` | :46 | Abstract gameplay player — clock, HUD, pause, fail |
| `Screens/Play/SoloPlayer.cs` | :20 | Single-player implementation (extends `SubmittingPlayer`) |
| `Screens/Play/SubmittingPlayer.cs` | :33 | Abstract player with score submission scaffold |

### Results / Ranking

| File | Line | Role |
|------|------|------|
| `Screens/Ranking/ResultsScreen.cs` | :40 | Abstract results — scroll, panels, statistics |
| `Screens/Ranking/SoloResultsScreen.cs` | :19 | Solo results with leaderboard fetch |
| `Screens/Ranking/ScorePanel.cs` | — | Individual score panel |
| `Screens/Ranking/ScorePanelList.cs` | — | Vertical score list |

### Scoring

| File | Line | Role |
|------|------|------|
| `Scoring/ScoreInfo.cs` | — | Score data model (Realm) |
| `Scoring/Score.cs` | — | Real-time score during gameplay |
| `Scoring/ScoreManager.cs` | — | Score CRUD, storage, retrieval |
| `Scoring/ScoreImporter.cs` | — | Import `.osr` files |
| `Scoring/Legacy/` | — | Legacy `.osu` score format support |

### Skin Engine (locked to one default)

| File | Line | Role |
|------|------|------|
| `Skinning/SkinManager.cs` | :39 | Manages skin storage, `CurrentSkin` bindable |
| `Skinning/SkinProvidingContainer.cs` | — | DI container for skin lookups |
| `Skinning/RulesetSkinProvidingContainer.cs` | — | Adds legacy + toggle logic for gameplay |
| `Skinning/BeatmapSkinProvidingContainer.cs` | — | Beatmap-level skin overrides |
| `Skinning/ArgonSkin.cs` | — | Default modern skin |
| `Skinning/ArgonProSkin.cs` | — | Pro variant |
| `Skinning/Triangles/TrianglesSkin.cs` | — | Triangles skin |
| `Skinning/LegacySkin.cs` | — | Stable-format legacy skin loader |
| `Skinning/SkinImporter.cs` | — | `.osk` import |

---

## 6. Open Questions / Risks

### 6.1 Starter-level cuts (clear and safe)

- **Mobile heads** (`osu.Android`, `osu.iOS`): Just don't build them. Zero DI coupling.
- **Taiko/Catch/Mania rulesets**: Remove project references from `osu.Desktop.csproj`. Existing Realm DB records for non-Osu rulesets will be stale but harmless (they'll show as `Available = false`).
- **Beatmap editor**: Remove Editor button from main menu, remove context-menu entry in song select, done.
- **Skin editor / selector**: Remove overlay creation + keyboard shortcuts. Lock `SkinManager.CurrentSkinInfo` to the Argon skin.

### 6.2 Mod system — HIGH risk, pervasive

The `SelectedMods` bindable is a cached DI singleton. It's referenced by:
- Song select footer (mod display bar, mod overlay registration)
- `PlayerLoader` gameplay stat calculations
- Ruleset change handler (which iterates ALL mod types)
- Results screen (displays applied mods)
- Configuration settings (mod hotkey, mod presets)

**Strategy:** The simplest trim is to keep the `SelectedMods` bindable but never populate it (always empty array). Remove `ModSelectOverlay`, `FooterButtonMods`, and all mod UI. Keep `Mod.cs` infrastructure classes only if gameplay engine requires them (unlikely — code may reference `Mod` types for score multiplier calculations).

**Risk item:** `SongSelect.cs:252-253` — score multiplier calculation uses mods. If mods are always empty, multiplier is always 1. That's fine.

### 6.3 Online layer — HIGH risk, deepest coupling

The online layer touches nearly every subsystem:

1. **API** is injected into `BeatmapManager`, `ScoreManager`, `BeatmapDownloader`, `ScoreDownloader`
2. `SoloPlayer` extends `SubmittingPlayer` which calls `CreateTokenRequest()` and `CreateSubmissionRequest()` — these make API calls
3. `SoloResultsScreen` fetches leaderboard via `LeaderboardManager` which queries the API
4. `SpectatorClient` and `MultiplayerClient` are added to scene graph
5. `MetadataClient` feeds `BeatmapOnlineChangeIngest`

**Strategy:**
- Replace `APIAccess` with `DummyAPIAccess` (`Online/API/DummyAPIAccess.cs`) — it returns offline/dummy states
- Make `BeatmapManager` and `ScoreManager` accept a null/noop API
- Override `SoloPlayer` to skip token creation and submission (or make a `LocalSoloPlayer` that doesn't submit)
- Replace `OnlineSpectatorClient` / `OnlineMultiplayerClient` with noop stubs, or null them out
- Replace `LeaderboardManager` with a local-only version
- Remove `BeatmapDownloader` / `ScoreDownloader` / `BeatmapModelDownloader`
- Remove `OnlineMetadataClient` + `BeatmapOnlineChangeIngest`

**Risk item:** `OsuGameBase.cs:319` — `API` is a non-nullable field. The `DummyAPIAccess` handles this. But many UI components check `api.State.Value` and react — these will always show as offline, which is fine.

**Risk item:** `OsuGameBase.cs:328` — `performOnlineLookups: true` parameter passed to `BeatmapManager`. Must be changed to `false` to avoid HTTP calls during beatmap import.

### 6.4 Settings UI — LOW risk, straightforward trim

- `OnlineSection`: remove from section list
- `SkinSection`: remove (skin locked to default)
- `GameplaySection`: remove mod-related controls
- `MaintenanceSection`: remove online deletion options
- `RulesetSection`: simplify (only osu! standard)

**Risk item:** `OsuGameBase.cs:648-685` — `CreateSettingsSubsectionFor(InputHandler)` creates platform-specific input settings. This stays as-is (mouse, tablet, joystick, touch, pen settings are still useful).

### 6.5 Scores and leaderboards

**Open question:** Should `SoloResultsScreen` still show a score list? Currently it fetches via `LeaderboardManager` (API). For "osu! lite", a local-only leaderboard is desirable.

**Suggestion:** Create `LocalSoloResultsScreen` that shows only the current score and local scores from the Realm database, bypassing `LeaderboardManager` entirely. The existing `SoloResultsScreen` can be stripped of its API fetch.

### 6.6 `OsuGame.cs` overlay creation (must trim)

The `LoadComplete()` method at `OsuGame.cs:1218-1248` creates every overlay unconditionally. For osu! lite, keep only:
- `FirstRunSetupOverlay` (optional)
- `ManageCollectionsDialog` (useful for sorting beatmaps)
- `SettingsOverlay` (trimmed sections)
- `NotificationOverlay` (needed for import notifications)
- `DialogOverlay` (needed for confirmations)
- `OnScreenDisplay` (toasts)
- `VolumeOverlay` (volume control)
- `Toolbar` (minimal — remove Online/ multiplayer buttons)
- `NowPlayingOverlay` (optional)

Remove all others: `BeatmapListingOverlay`, `DashboardOverlay`, `NewsOverlay`, `UserProfileOverlay`, `BeatmapSetOverlay`, `WikiOverlay`, `ChangelogOverlay`, `RankingsOverlay`, `ChatOverlay`, `SkinEditorOverlay`, `LoginOverlay`, `AccountCreationOverlay`, `MedalOverlay`, `MessageNotifier`.

### 6.7 Search/reference patterns for de-wiring

When deleting a class `Foo`, search for these patterns to find coupling:
- `new Foo(`
- `Foo foo` (field declaration)
- `[Resolved]` (or `[Resolved(canBeNull: true)]`) for DI
- `dependencies.CacheAs<Foo>` / `dependencies.CacheAs(foo)` in DI setup
- `typeof(Foo)` in `validScreens` arrays
- `using` / namespace import (`using osu.Game.Online.*` etc.)
- `.csproj` `ProjectReference` for ruleset projects

### 6.8 Build dependencies

`osu.Desktop.csproj` references:
- `osu.Game` — keep
- `osu.Game.Rulesets.Osu` — keep
- `osu.Game.Rulesets.Taiko` — remove from csproj
- `osu.Game.Rulesets.Catch` — remove from csproj
- `osu.Game.Rulesets.Mania` — remove from csproj

The ruleset DLL discovery in `RealmRulesetStore` works by scanning loaded assemblies. With the project references removed, those assemblies won't be loaded, and they won't appear in `AvailableRulesets`.

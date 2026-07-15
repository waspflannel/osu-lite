# osu! lite final trim

**Status:** binding implementation guide with post-merge unfinished-business audit
**Audit date:** 2026-07-15
**Audited tree:** repository contents matching `origin/master` at merge `23c522fa68`
**Purpose:** define the final product, record what the merged final trim actually completed, and remove every remaining out-of-scope path

## How to use this document

This is not another options document. Every product decision below is locked. The final trimming agent must implement the whole guide, including small deletions, intertwined deletions, package cleanup, resource cleanup, repository cleanup, and warning cleanup.

The following are not acceptable reasons to leave an item behind:

- the deletion saves only a few lines;
- the code compiles and therefore appears harmless;
- the code is mixed into a retained class;
- removing it causes follow-on compile errors;
- the type is public, serialisable, reflectively constructible, or historically persisted;
- it may be useful to a future upstream-like feature;
- the resource is already supplied by a package;
- a setting, action, localisation key, or model field is not visible during normal play.

When a removed feature is intertwined with retained code, remove its fields, branches, constructor parameters, interfaces, enum members, persistence columns, localisation, resources, and call sites from the retained code. A compile error caused by deletion is a work item, not a reason to restore the old abstraction.

## Current baseline

The merged final-trim pass made another substantial reduction. The following values describe the post-merge source, not the pre-final-trim baseline used when this guide was first written.

| Measurement | Current value |
|---|---:|
| Tracked C# files | 1,395 |
| Physical C# lines | 163,544 |
| Approximate non-blank C# lines | 134,769 |
| Original pre-lite C# files | 4,823 |
| Original pre-lite physical C# lines | 586,147 |
| Files removed from the original tree | 71.1% |
| Physical lines removed from the original tree | 72.1% |
| Final-trim merge reduction | 83 C# files / 10,564 physical C# lines |
| Current Debug build | succeeds with 0 errors / 0 warnings |
| Current Release build | succeeds with 0 errors / 0 warnings |
| Current Release output directory | 583.04 MB |
| `osu.Game.Resources.dll` alone | 125.29 MB |
| Manifest resources in that assembly | 1,651 / 124.64 MB |

The final-trim merge removed about 5.6% of the remaining files and 6.1% of the remaining physical C# lines. Across all trimming passes, the repository has removed 3,428 tracked C# files and 422,603 physical C# lines from the original baseline.

The remaining source reduction is estimated at **20,000–35,000 physical C# lines**, or about **12–21% of the current tree**. The midpoint is roughly **27,000 lines / 16.5%**, leaving approximately **128,500–143,500 physical C# lines**. This is an estimate, not a quota: the product boundary decides completion.

## Unfinished business

This section is the authoritative post-merge remainder. Items elsewhere in this guide that are already absent are historical record; the items below still exist in the audited tree and are required for the product defined here. **CI configuration, CI workflow repair, and CI matrix coverage are explicitly out of scope and do not block completion.** Builds and smoke tests may be run manually.

### 1. Finish the offline/local identity boundary

The ordinary runtime now uses a dummy API provider and no normal ppy contact was demonstrated, but the source still retains the general online capability and API-shaped product model:

- `osu.Game/Online/` still contains about 30 C# files / 2,612 lines;
- `osu.Game/Users/` still contains about 12 C# files / 1,582 lines;
- `IAPIProvider`, `DummyAPIAccess`, `APIRequest`, `OsuWebRequest`, endpoint types, general link routing, remote-avatar fallback, and `ReportPopover` remain;
- the retained user and beatmap models still carry API/online assumptions that are not required by a local player.

Delete the request stack and endpoint model outright, replace API-shaped user state with the locked local identity, make avatars local-only, and reduce URL opening to the three explicitly allowed external-browser destinations. A dummy implementation that fails queued requests is not the final boundary; the capability itself must be absent.

### 2. Make Kanna the only user skin, not merely the default

Kanna is now selected by default, but the broad skin library remains. `SkinManager`, import/export/edit/database/layout paths, and the Argon, Argon Pro, Retro, and Triangles implementations are still registered. The shared skin base is about 83 C# files / 9,062 lines and osu!-specific skinning is about 67 files / 6,543 lines; not all of this is removable because beatmap-local assets and classic fallback are retained.

Remove user skin discovery, selection, persistence, import, export, editing, saved layouts, and every non-Kanna bundled user skin. Replace `SkinManager` with the narrow fixed-skin service described in this guide. Retain only Kanna, beatmap-local overrides, and the classic legacy fallback needed when an element is missing.

### 3. Remove the mod model and close replay admission

The repository still has about 118 mod files / 7,755 lines. `LegacyScoreDecoder` still decodes all stable mod masks, `ScoreInfo` still persists `ModsJson`/`APIMod` state, `ModClassic` synthesis remains, and Ctrl+Enter autoplay is still implemented through `ModAutoplay`.

Delete the selectable mod hierarchy, mod serialization, mod UI, mod displays, and mod persistence. Route autoplay directly into the player without constructing a mod. Reject a replay before storage when either its stable mask or embedded mod settings indicate any mod; retain only unmodded `.osr` import, playback, and export.

### 4. Finish the one-ruleset and narrow beatmap-management boundary

`RulesetStore` still scans assemblies dynamically, `RealmRulesetStore`, `AssemblyRulesetStore`, and the ruleset toolbar selector remain, and converted-beatmap display/conversion is still represented. Beatmap creation, external editing, `LegacyBeatmapExporter`/encoder paths, and general import-as-update APIs also remain.

Replace dynamic discovery with the single bundled osu!standard ruleset. Remove custom-ruleset directories, ruleset persistence/selection, conversion, and scrolling-ruleset UI. Keep ordinary local `.osz` import, duplicate handling, search, playback, and deletion; remove create-new-difficulty, external-editor, beatmap-export, and editor-oriented update paths.

### 5. Collapse notifications, settings, and input to the allowlists

The full notification model/drawer still accounts for about 10 C# files / 1,722 lines. Settings still account for about 82 files / 7,716 lines and construct eight sections: General, Input, User Interface, Gameplay, Audio, Graphics, Maintenance, and Debug. `OsuSetting` still has 68 entries rather than the final allowlist, and joystick settings/translation/events remain.

Replace the notification feature with the narrow operation-status sink required for imports and destructive local operations. Reduce settings to the six sections and exact entries defined in this guide, including `LocalPlayerName`; remove maintenance/debug/product-development controls. Remove joystick support while retaining keyboard, mouse, and tablet/pen input.

### 6. Trim resources and direct dependencies

`ppy.osu.Game.Resources` remains a direct dependency and still contributes an unchanged 125.29 MB output assembly. The resource allowlist/repack has therefore not happened. `AutoMapper`, `Humanizer`, and `Microsoft.Toolkit.HighPerformance` also remain even though this guide locks their removal.

Create the retained resource allowlist only after the UI and feature deletions above, embed that payload locally, remove the upstream resource package, trim the bundled Kanna archive, then remove the three remaining unnecessary direct dependencies and their adapter/extension residue. Verify the published output, not only source references.

### 7. Delete verified dead and disabled code

Two intertwined leftovers require manual deletion rather than another broad file pass:

- `RealmAccess.cs` still wraps the historical migration implementation in `#if false` near line 761;
- the unused `applyFilenameSchemaSuffix` helper remains near line 171.

The current audit also found approximately 1,845 lines of declaration-only code across the following types. Delete the types and any now-orphaned localisation, resources, interfaces, and registration sites:

- `ReportPopover`, `BarLineGenerator`, `IBarLine`, `IHasColumn`, and `IHasHold`;
- `BPMCounter`, `LongestComboCounter`, `MatchScoreDisplay`, `PlayerAvatar`, and `PlayerFlag`;
- `SkinnableModDisplay`, `DefaultRankDisplay`, `LegacyRankDisplay`, and `LegacyPerformancePointsCounter`;
- `ClicksPerSecondCounter`, `ArgonUnstableRateCounter`, `ArgonJudgementCounterDisplay`, and `GameplayOffsetControl`;
- `BigBlackBox` and `TextElement`.

After these known deletions, repeat exact-reference searches at type, member, enum, localisation, resource, constructor-parameter, and package level. Zero-reference code is work even when it is embedded in a retained file.

### 8. Finish repository-facing product cleanup, excluding CI

`README.md`, `CONTRIBUTING.md`, issue templates, and the older trimming documents still describe upstream or retired surfaces. Non-CI editor/repository debris also remains, including `.vscode/`, `.run/`, `.idea/`, `osu.sln.DotSettings`, `osu.Desktop.slnf`, `.config/`, InspectCode scripts, UseLocalResources scripts, and `.git-blame-ignore-revs`.

Rewrite the user/contributor documentation and issue forms for the actual local desktop product, then remove obsolete non-CI editor and helper files once their retained purpose has been checked. Archive or delete the old trim documents after the completion evidence is captured here. Do not spend this pass repairing, replacing, or measuring `.github/workflows/ci.yml`; CI is outside the agreed product boundary.

### Completion verdict

The codebase is dramatically smaller and the merged pass is healthy at compile time, but it is not yet the final light product described by this document. The remaining work is concentrated in a few large generic systems—online/API shape, skin management, mods, dynamic rulesets, settings/notifications, and resources—plus a finite dead-code sweep. Once those clusters are removed, further reductions should be ordinary maintenance rather than another product-wide trim.

## Final product contract

The final product is a cross-platform, local-first osu!standard desktop player. Its complete supported surface is:

- Windows, macOS, and Linux desktop;
- one bundled osu!standard ruleset, kept in the existing three-project solution;
- keyboard, mouse, and tablet/pen input;
- normal local `.osz` import, duplicate handling, deletion, and local metadata/search;
- local beatmap playback with audio, video, storyboard, beatmap colours, beatmap skin elements, and beatmap hitsounds;
- a fixed bundled **kanna 2.0 [OG] ultra lite** user skin with the classic legacy fallback required for missing elements;
- solo play with no selectable gameplay mods;
- Ctrl+Enter autoplay as a local playback mode;
- unmodded `.osr` import, local storage, playback, export, and failed-score replay saving;
- local score history, local in-game leaderboard, detailed local results, difficulty calculation, and performance calculation;
- pause, retry, replay pause/seek/frame-step/fast-forward controls, screenshots, and logs;
- local settings, data reset, data deletion, and relocation of the current lite data directory;
- localisation for the retained UI;
- transient toasts and operation progress, including cancellation and exit protection for active imports;
- exactly three explicit external-browser destinations: the project issue form and the two OpenTabletDriver documentation pages.

Everything outside that list is out of scope and must leave.

## Decisions locked

| ID | Locked decision | Consequence |
|---|---|---|
| F1 | The app is offline in-process. | No HTTP client, web request, API provider, endpoint, remote image, download, update, metadata, preview, or arbitrary URL path remains. |
| F2 | External browsing is allowlisted, not general-purpose. | Keep only `https://github.com/waspflannel/osu-lite/issues/new/choose`, `https://opentabletdriver.net/Tablets`, and `https://opentabletdriver.net/Wiki/FAQ/General`. The launcher accepts an enum destination, never a URL string from game content. |
| F3 | There is no startup intro product. | Loader transitions directly to the main menu. Delete all intro variants, intro selection, intro voice, intro beatmaps, and bundled intro tracks. |
| F4 | Kanna is the only user skin. | Delete skin selection, import, export, editing, database storage, layouts, and every other bundled user skin. Keep map-local skin overrides and the smallest classic fallback. |
| F5 | There is no general mod system in the final runtime. | Ctrl+Enter autoplay becomes a direct playback mode, not a `Mod`. Remove mod lists, mod interfaces, mod settings, mod persistence, mod displays, and mod-adjusted difficulty/performance branches. |
| F6 | Imported replays must be unmodded. | Reject any `.osr` with a non-zero stable mod mask or embedded mod entry before storage, including Autoplay. A raw-zero stable replay is admitted and receives a small replay-compatibility context rather than a synthetic Classic mod. |
| F7 | Replay playback stays; replay analysis goes. | Keep playback controls and replay export. Delete click markers, frame markers, cursor paths, cursor-hiding analysis, display-length settings, and the analysis overlay. |
| F8 | Old user data is intentionally incompatible. | Use a fresh `osu-lite` profile/config/keybinding/Realm namespace. Do not discover, copy, open, import, or migrate old lazer, old lite, or osu!stable data. |
| F9 | Future lite-to-lite schema upgrades remain possible. | Reset to a small schema version and retain only a minimal forward migration hook. Delete the historical 53-version migration body and historical background jobs. |
| F10 | Ruleset discovery is fixed. | Keep the three projects, but register exactly one osu!standard ruleset from Desktop. Delete assembly scanning, custom ruleset loading, dynamic resolution, selectors, conversion, and ruleset persistence. |
| F11 | Results stay useful and local. | Retain score, accuracy, combo, hit counts, rank, hit error, unstable rate, difficulty, and performance displays. Remove online ranks, remote user presentation, mod presentation, medals, and online comparison concepts. |
| F12 | The local player has a name, not an API identity. | Replace `IAPIProvider.LocalUser`/`APIUser`/guest-user plumbing with one local player-name bindable. Remove avatars, flags, covers, profiles, online IDs, supporter state, relations, teams, and activities. |
| F13 | Notifications are transient. | Keep a small toast/operation sink. Delete the drawer, history, sections, unread count, toolbar entry, notification action, and permanent notification models. |
| F14 | Settings have six sections. | Final sections are General, Input, Gameplay, Audio, Graphics, and Data. There is no User Interface, Maintenance, Debug, Mods, First Run, Skin, Online, or Update section. |
| F15 | The main menu is direct and quiet on a fresh profile. | No bundled menu beatmap, intro voice, menu tips, seasonal theme, or selectable background source. Show the selected local beatmap background when available and a code-drawn dark fallback otherwise. |
| F16 | Desktop support is Windows/macOS/Linux only. | Delete mobile, touch-long-press, Android, and iOS residue. Retain actual macOS/Linux paths and build gates. |
| F17 | Windows integration is local-file-only. | Keep `.osz` and `.osr` associations. Delete `osu://`, `.osk`, and general URI registration. Archive IPC remains for local file forwarding. |
| F18 | Localisation remains a product feature. | Retain languages for retained UI, but delete resource sets, wrappers, and keys belonging only to removed features. Do not convert the product to English-only. |
| F19 | Upstream resource bulk is not allowed in the final binary. | Replace `ppy.osu.Game.Resources` with an in-repository allowlisted resource set embedded in `osu.Game`; do not add a fourth project. |
| F20 | The final repository is an application, not an upstream SDK/release pipeline. | Disable NuGet packing and remove template/custom-ruleset publishing, stale IDE/test configs, retired deployment/Sentry/diffcalc/web-mod infrastructure, and upstream project copy. CI itself is excluded. |

## Final screen and service allowlist

The supported screen flow is:

`Loader -> Main menu -> Song select -> Solo play -> Pause/Fail/Results -> Song select or Main menu`

The only additional top-level UI surfaces are:

- six-section settings;
- dialogs required by import, deletion, reset, data relocation, quit, and errors;
- the compact music/volume controls;
- transient operation toasts;
- local replay controls;
- file import progress.

Any screen, overlay, popover, toolbar control, footer action, or global action that cannot be reached from this allowlist must be deleted.

## Final settings allowlist

The settings UI must contain only the following responsibilities.

### General

- language;
- local player name;
- open logs;
- open storage;
- report issue through the fixed issue destination;
- reset settings.

### Input

- keyboard bindings for retained global and osu!standard actions;
- mouse button/wheel disable options and cursor confinement;
- mouse sensitivity when supplied by the framework;
- tablet/pen device, area, rotation, and OpenTabletDriver documentation links;
- reset input settings.

There is no joystick/gamepad subsection or binding presentation.

### Gameplay

- menu and gameplay cursor size;
- cursor rotation;
- background dim and blur;
- lighten during breaks;
- storyboard toggle;
- beatmap skin, colour, and hitsound toggles;
- positional hitsound level;
- first combo-break behaviour;
- HUD visibility and key overlay;
- local in-game leaderboard;
- low-health playfield fade;
- hit lighting;
- combo-colour normalisation;
- song-select star range, grouping, sorting, random algorithm, and background blur.

### Audio

- framework audio device and volume controls;
- inactive volume;
- global audio offset;
- automatic beatmap-offset adjustment.

### Graphics

- renderer, display mode, resolution, frame limiter, and performance display supplied by the framework;
- UI scale;
- screenshot format and menu-cursor inclusion;
- Windows-key blocking during gameplay.

### Data

- current lite storage path and relocation;
- delete all beatmaps;
- delete all scores/replays;
- reset the current lite database/config;
- open storage and logs.

Normal `.osz`/`.osr` import is handled by drag/drop, file association, or file-open flow; the debug batch importer does not survive as a settings feature.

### Final `OsuSetting` allowlist

Compact the enum for the fresh namespace and retain only these game-owned settings, plus the new `LocalPlayerName` replacement:

```text
LocalPlayerName
MenuCursorSize
GameplayCursorSize
DimLevel
BlurLevel
LightenDuringBreaks
ShowStoryboard
KeyOverlay
GameplayLeaderboard
PositionalHitsoundsLevel
AlwaysPlayFirstComboBreak
HUDVisibilityMode
FadePlayfieldWhenHealthLow
MouseDisableButtons
MouseDisableWheel
ConfineMouseMode
AudioOffset
VolumeInactive
CursorRotation
DisplayStarsMinimum
DisplayStarsMaximum
SongSelectGroupMode
SongSelectSortingMode
RandomSelectAlgorithm
ShowFpsDisplay
SongSelectBackgroundBlur
ScreenshotFormat
ScreenshotCaptureMenuCursor
BeatmapSkins
BeatmapColours
BeatmapHitsounds
UIScale
HitLighting
GameplayDisableWinKey
ComboColourNormalisationAmount
AutomaticallyAdjustBeatmapOffset
```

Framework-owned language, audio-device, volume, window, renderer, frame-limiter, and tablet settings remain in their framework configuration stores. Every current `OsuSetting` not in the list above must be deleted. In particular, delete `Ruleset`, `Token`, `AutoCursorSize`, `ShowHealthDisplayWhenCantFail`, `FloatingComments`, `MenuMusic`, `MenuVoice`, `MenuTips`, `MenuParallax`, `Prefer24HourTime`, `BeatmapDetailTab`, `BeatmapDetailModsFilter`, `Username`, `SavePassword`, `SaveUsername`, `ToolbarClockDisplayMode`, `Version`, `ShowFirstRunSetup`, `ShowConvertedBeatmaps`, `Skin`, `IncreaseFirstObjectVisibility`, `ScoreDisplayMode`, `ExternalLinkWarning`, `PreferNoVideo`, every custom scaling/safe-area field except `UIScale`, `IntroSequence`, `UIHoldActivationDelay`, `StarFountains`, `MenuBackgroundSource`, `LastProcessedMetadataId`, both replay-layout settings, `HideCountryFlags`, both configurable hold-for-menu settings, and `ShowMobileDisclaimer`.

`OsuRulesetSetting` retains only slider snaking, cursor trail, cursor ripples, and playfield border style. Delete all five replay-analysis settings.

## Final global-action allowlist

The fresh keybinding namespace removes tombstones and uses explicit stable values for the retained actions. Retain only:

```text
ResetInputSettings
ToggleToolbar
ToggleSettings
IncreaseVolume
DecreaseVolume
ToggleMute
SkipCutscene
QuickRetry
TakeScreenshot
ToggleGameplayMouseButtons
Back
Select
QuickExit
MusicNext
MusicPrev
MusicPlay
ToggleNowPlaying
SelectPrevious
SelectNext
Home
PauseGameplay
HoldForHUD
TogglePauseReplay
ToggleInGameInterface
SelectNextRandom
SelectPreviousRandom
ToggleBeatmapOptions
PreviousVolumeMeter
NextVolumeMeter
SeekReplayForward
SeekReplayBackward
ActivatePreviousSet
ActivateNextSet
ToggleFPSDisplay
SaveReplay
ExportReplay
ToggleReplaySettings
ToggleInGameLeaderboard
IncreaseOffset
DecreaseOffset
StepReplayForward
StepReplayBackward
AbsoluteScrollSongList
ExpandPreviousGroup
ExpandNextGroup
ToggleCurrentGroup
FastForwardReplay
```

Delete every other current `GlobalAction`, its default binding, category entry, handler case, setting row, and localisation key. This explicitly removes all chat/social/beatmap-listing/notification/profile/mod tombstones, both scrolling-ruleset speed actions, `ToggleChatFocus`, all editor actions, and all editor-test-play actions.

## Complete removal ledger

### 1. Online API, remote content, and arbitrary links

Delete `osu.Game/Online/` in full, including:

- `APIRequest`, `APIDownloadRequest`, `ArchiveDownloadRequest`, `OsuJsonWebRequest`, and `OsuWebRequest`;
- `IAPIProvider`, `DummyAPIAccess`, `ILocalUserState`, API state/completion/exception types, and guest user;
- endpoint configuration and production/development endpoints;
- `APIBeatmap`, `APIBeatmapSet`, `APIUser`, `APIRelation`, `APITeam`, `SoloScoreInfo`, `APIMod`, and mod JSON formatting;
- `DrawableLinkCompiler`, `MessageFormatter`, `ILinkHandler`, `LinkDetails`, `LinkWarnMode`, and the current general `ExternalLinkOpener`;
- online download state and archive download concepts.

Also delete:

- `Graphics/UserInterfaceV2/ReportPopover.cs`;
- all `OsuGame.HandleLink()` routing and the no-op `ShowChannel`, `ShowBeatmapSet`, `ShowUser`, `ShowBeatmap`, `Search`, `ShowWiki`, and changelog methods;
- `OpenUrlExternally(string, LinkWarnMode)` and any arbitrary-URL interface;
- remote avatar fallback `https://a.ppy.sh/...`;
- remote preview, remote texture, metadata, update, and download registrations;
- ppy website/API/spectator/submission endpoint constants;
- online URL parsing, wiki link generation, beatmap links, user links, profile links, supporter links, and changelog links.

Replace this with one desktop service whose public input is a closed enum such as `IssueTracker`, `TabletList`, and `TabletFaq`. It maps those values to the three locked URLs internally and calls the OS browser. No game content, beatmap, replay, setting, command line, or IPC message may provide a URL.

Source comments and licence references may contain HTTP URLs. Executable code may contain only the three allowlisted browser constants.

### 2. API-shaped user and online beatmap models

Delete `osu.Game/Users/` in full after replacing retained score/result labels with the local player-name bindable. This removes:

- avatars, updateable avatars, clickable avatars, and avatar textures;
- flags, country codes, flag hiding, and updateable flags;
- clickable usernames and user-card tooltips;
- user covers, statistics, activity, supporter state, teams, relations, ranks, and online IDs;
- `IUser` as a broad API model.

Delete the online beatmap model surface:

- `IBeatmapOnlineInfo` and `IBeatmapSetOnlineInfo`;
- `BeatmapOnlineStatus` and `BeatmapSetOnlineStatusPill`;
- online availability, covers, hype, nominations, nomination metadata, online genre, and online language;
- API fail-time objects and API-only beatmap/set fields;
- online IDs from active local models where they no longer participate in any retained operation;
- ranked-status group pills and online-status presentation in song select.

Retain local file metadata only: hash, title, artist, creator text, difficulty name, source, tags, audio/video/background references, timing, object data, local difficulty attributes, and local score links. Legacy decoder fields such as beatmap IDs may be parsed and discarded when required to read a valid `.osu` file; they are not persisted or presented.

### 3. Fixed Kanna skin

Delete the multi-skin product rather than hiding it:

- `SkinManager` and Realm-backed skin model management;
- `SkinImporter`, `.osk` import, import-as-update, duplicate handling, and skin file storage;
- `LegacySkinExporter`, `LegacySkinEncoder`, and `.osk` export;
- skin selection setting and every selector/dropdown/preview/editor call site;
- skin deletion, editing, reloading by database identity, and user skin lookup;
- `SkinInfo` identities for Argon, Argon Pro, Retro, Triangles, and user skins;
- `ArgonSkin`, `ArgonProSkin`, `RetroSkin`, and `TrianglesSkin`;
- `osu.Game.Rulesets.Osu/Skinning/Argon/` in full;
- `osu.Game/Skinning/Triangles/` in full;
- `SkinLayoutInfo`, `SerialisedDrawableInfo`, `ISerialisableDrawable`, `ISerialisableDrawableContainer`, serialised layout extensions, and saved HUD-layout reflection;
- `UserSkinComponentLookup`, general user-skin sources, import/export transformers, and skin source-change paths that exist only for selection/reload;
- mania skin configuration/decoder branches and non-osu ruleset skin branches;
- Argon/Triangles/Retro shaders, samples, textures, localisation, and layout resources.

Replace `SkinManager` with a small fixed provider that exposes:

1. the embedded Kanna archive;
2. the smallest classic legacy fallback needed for missing textures/samples/components;
3. the active beatmap-local skin layer, controlled by the three retained beatmap-skin settings.

Keep `LegacySkinDecoder` only to the extent needed to read `skin.ini` from Kanna and beatmap-local skins. Remove its user-library, export, mania, taiko, catch, and saved-layout branches. Keep normal, soft, and drum sample-bank support because osu!standard beatmaps can request all three banks.

Audit `DefaultSkin.osk` itself. Remove files that cannot be requested by the retained osu!standard, Kanna, beatmap-skin, result, pause, or fail paths. Do not remove a sample bank merely because its name resembles another ruleset.

After layout serialisation leaves, delete reflectively kept HUD/component orphans, including:

- `BPMCounter`, `LongestComboCounter`, `MatchScoreDisplay`, `PlayerAvatar`, and `PlayerFlag`;
- `SkinnableModDisplay`, `DefaultRankDisplay`, `LegacyRankDisplay`, and `LegacyPerformancePointsCounter`;
- `ClicksPerSecondCounter`, `ArgonUnstableRateCounter`, and Argon judgement displays;
- `Skinning/Components/BigBlackBox` and `Skinning/Components/TextElement`;
- Triangles-only default HUD components with no direct retained construction site;
- `GameplayOffsetControl` and other saved-layout-only controls.

### 4. Mods and autoplay

Delete the general mod system end-to-end:

- all selectable `Mod` implementations in `osu.Game/Rulesets/Mods/` and `osu.Game.Rulesets.Osu/Mods/`;
- mod categories, types, acronyms, icons, incompatibility rules, multipliers, settings, seeds, and adjustment interfaces;
- `Ruleset.GetModsFor()`, `CreateAllMods()`, available/selected mod bindables, and mod selection plumbing;
- mod switches, tooltips, displays, setting controls, setting-change trackers, and localisation;
- `APIMod`, `ModsJson`, mod settings dictionaries, mod serialization, and mod Realm fields;
- mod arrays/lists on score, replay, difficulty, performance, ruleset, player, results, leaderboard, and HUD models;
- `IncreaseFirstObjectVisibility` and all visibility-mod support;
- mod-adjusted rate, difficulty, score multiplier, audio, sample, hit-window, and drawable branches;
- synthetic `ModClassic` creation for stable raw-zero replays.

Autoplay survives as direct replay generation:

- Ctrl+Enter calls an osu!standard autoplay replay factory;
- the generated replay is played immediately and is not persisted as a local score;
- no `ModAutoplay`, mod list, mod icon, mod multiplier, or mod setting is created;
- move or inline the useful part of `ModExtensions.CreateScoreFromReplayData` into the autoplay/replay path and delete `ModExtensions`.

Stable raw-zero replay compatibility becomes an explicit replay-origin/compatibility value consumed only where stable replay semantics differ. Finish the Classic migration in this pass: do not retain `ModClassic` as a compatibility container.

Difficulty and performance calculators become unmodded-only APIs. Remove empty mod parameters and dead mod-aware caches rather than passing empty lists everywhere.

### 5. Replay admission and analysis

Retain `LegacyScoreDecoder`, `LegacyScoreEncoder`, replay decompression, replay frames, replay input, local score storage, and replay controls.

Before a decoded `.osr` enters Realm or file storage:

- reject any non-zero legacy mod mask;
- reject any embedded mod entry or settings payload;
- reject Autoplay as well as every other mod;
- display one local transient error explaining that osu! lite accepts unmodded replays only;
- leave no partial model or file behind.

Delete:

- `ReplayAnalysisOverlay`;
- `ReplayAnalysisSettings`;
- `UI/ReplayAnalysis/` in full;
- replay click/frame markers and cursor path generation;
- replay-analysis cursor hiding;
- the five replay-analysis ruleset settings and related player localisation;
- analysis-only overlays/proxies and cancellation state from `DrawableOsuRuleset`.

Keep the compact playback bar and keyboard actions for pause, seek, frame step, and fast forward. It has a fixed layout; delete `ReplayPlaybackControlsExpanded`. The basic replay settings overlay retains playback speed plus the existing beatmap dim, storyboard, beatmap-skin, beatmap-colour, and beatmap-hitsound controls. It contains no analysis controls.

### 6. One fixed ruleset

Delete dynamic ruleset infrastructure:

- `AssemblyRulesetStore`;
- dynamic assembly scanning and AppDomain assembly resolution in `RulesetStore`;
- custom ruleset directories and crash attribution;
- Realm-backed ruleset installation/state and ruleset setting stores keyed for arbitrary plugins;
- `RulesetSelector`, toolbar ruleset selector/tab button, overlay ruleset selectors/tabs, and associated samples;
- ruleset switching, persisted ruleset selection, and `OsuSetting.Ruleset`;
- `ShowConvertedBeatmaps`, conversion filters, conversion branches, and converted-beatmap presentation;
- `osu.Game/Rulesets/UI/Scrolling/` in full and its F3/F4 actions;
- generic objects/interfaces used only by deleted rulesets or conversion, including `BarLineGenerator`, `IBarLine`, `IHasColumn`, `IHasHold`, and their dead parsers/extensions;
- non-osu variant selection and variant keybinding presentation.

Keep the three-project layout. Desktop explicitly supplies one osu!standard ruleset/factory to Game at startup. The retained Game abstractions are only those needed to maintain the project dependency direction and implement osu!standard gameplay, difficulty, scoring, replay, and skin lookup. When a generic base has exactly one retained implementation and provides no project-boundary value, collapse it.

Delete `OsuBeatmapConverter`; normal `.osu` decoding and osu!standard beatmap processing remain.

### 7. Fresh lite database and configuration

Create a fresh `osu-lite` data identity for:

- Realm database filename/namespace;
- game configuration;
- framework configuration where application naming controls the path;
- keybindings;
- ruleset settings;
- import file storage.

Delete from `RealmAccess`:

- backward database-version scanning and copying;
- the schema-version 53 historical migration switch;
- migrations for deleted rulesets, online IDs, mods, collections, tags, multiplayer, skins, old score models, old keybindings, and mobile;
- special cases that preserve old enum ordinals;
- old-database repair paths which cannot occur in the new namespace.

Start the final schema at a small explicit version and keep a minimal forward migration callback for future `osu-lite` releases. The initial schema contains only retained local beatmap, file, score/replay, settings, and import state.

Delete:

- `BackgroundDataStoreProcessor` and historical score/statistics jobs;
- `StandardisedScoreMigrationTools` and other migration-only helpers after the fresh schema is active;
- dead online-ID Realm queries and model extensions;
- deleted-feature fields, indexes, relationships, and generated Realm model members;
- old config migrations in `OsuGame.applyConfigMigrations()`;
- keybinding tombstones and old ordinal migration logic.

Storage relocation for the current lite directory remains. It moves the current directory only and has no stable/lazer discovery mode.

### 8. osu!stable and first-run migration

Delete the stable-import product in full:

- `LegacyImportManager`;
- `StableStorage` and stable directory detection;
- stable beatmap and stable score importers used by migration;
- first-run setup overlay, import-from-stable screen, and progress button;
- wizard overlay/screen infrastructure in full;
- stable-directory dialogs and settings screens;
- stable directory config and hard-link preference;
- `HardLinkHelper` when its final caller leaves;
- first-run and stable-import localisation/resources;
- `ShowFirstRunSetup` and all launch gating around it.

On a fresh profile the Loader goes directly to the main menu. Normal user-initiated `.osz` and `.osr` import remains separate from this deletion.

### 9. Beatmap manager, editor residue, and export

The final beatmap manager supports only install/reinstall, query, load, select, delete, and file integrity for local `.osz` content.

Delete editor/export operations:

- `CreateNew`, `CreateNewDifficulty`, editor `Save`, editor change tracking, and editor metadata mutation;
- `BeginExternalEditing` and `ExternalEditOperation`;
- `LegacyBeatmapExporter`;
- `LegacyBeatmapEncoder` and `LegacyStoryboardEncoder` after all export callers leave;
- editor report/submit/external-edit actions and localisation;
- archive-importer interface members used only by edit, export, update-as-editor, or skin flows;
- editor-only geometry, snapping, bookmarks, timing, compose/design/setup/verify, and test-play residue;
- import-as-update APIs that exist only for editor workflows.

Keep duplicate `.osz` reconciliation and safe reimport as an installer concern. Rename or narrow methods so they do not preserve an editor-shaped contract.

`IBeatmapUpdater` has one implementation and an unused queue. Remove the interface and queue, and inline the required duplicate/reimport reconciliation into `BeatmapImporter`.

Prune `RealmArchiveModelImporter` and related interfaces to the two supported archive flows: beatmaps and scores/replays. Remove download throttling, online state, skin delegates, editor delegates, and hypothetical generic operations.

### 10. Notification drawer to operation sink

Delete:

- `NotificationOverlay`;
- `NotificationOverlayToastTray` as an overlay-forwarding bridge;
- `NotificationSection` and section grouping;
- drawer history, unread count, read state, notification toolbar button, and `ToggleNotifications`;
- notification expiration/history rules and permanent model storage;
- `TooManyDownloadsNotification`;
- social/online notification types, icons, samples, and localisation.

Retain or replace with a small service that supports only:

- simple information/error toast;
- progress value/text;
- completion/failure state;
- optional cancellation action;
- active-operation enumeration for quit confirmation.

Import, delete, migration-of-current-storage, screenshot, and replay admission call that service directly. There is no route from a toast into a drawer.

### 11. Settings and input cleanup

Delete these settings sections and their now-unused controls:

- `DebugSection` and `DebugSettings/`;
- `UserInterfaceSection` after moving retained controls into General, Gameplay, or Graphics;
- old `MaintenanceSection` after replacing it with the narrow Data section;
- `Gameplay/ModsSettings.cs`;
- `DebugSettings/BatchImportSettings.cs`;
- `DebugSettings/MemorySettings.cs`;
- first-run/stable maintenance screens;
- skin, online, update, and removed-feature setting controls;
- configurable menu intro, voice, tips, parallax, background source, clock mode, safe area, custom scaling, hold delay, and mobile disclaimer controls.

Delete joystick/gamepad support from the application layer:

- `JoystickSettings`;
- gamepad-only keybinding presentation;
- joystick button translation in `RulesetInputManager`;
- menu/gameplay joystick event branches;
- joystick-specific localisation.

Retain framework input support only for keyboard, mouse, and tablet/pen. Remove long-touch/mobile hold properties and iOS mouse special cases.

After the settings surface is reduced, delete generic form controls that have no retained construction site instead of keeping a large form toolkit for hypothetical future settings.

### 12. Menu, toolbar, and decorative residue

Delete the startup intro stack:

- `Configuration/IntroSequence.cs`;
- `Screens/Menu/IntroScreen.cs`;
- `IntroCircles`, `IntroWelcome`, `IntroTriangles`, and their nested sequences;
- `Screens/Menu/IntroSequence.cs` and `OsuLogo` intro-animation dependencies no longer used by the direct menu;
- `RulesetFlow`, glitching triangle intro graphics, welcome/supporter presentation, intro voice, and intro clocks;
- `circles.osz`, `triangles.osz`, `welcome.osz`, `christmas2024.osz`, ranked-play music, intro samples, and intro backgrounds;
- `MenuTipDisplay` and menu-tip localisation;
- `KiaiMenuFountains` and other menu-only decorative effects with no retained direct use.

Simplify Loader to load dependencies and enter MainMenu. MainMenu uses the selected local beatmap background when one exists and a code-drawn dark fallback otherwise. It does not import a protected bundled beatmap on startup.

Simplify the toolbar:

- delete ruleset selector and ruleset tab button;
- delete notification and removed overlay buttons;
- delete analog clock, runtime clock modes, and clock-mode setting;
- delete the analog clock, digital clock, runtime clock, toolbar clock, and clock-mode setting in full;
- keep home, music, settings, and the toolbar container.

Delete Star Fountains as a configurable/decorative product and remove its setting. Retained gameplay hit lighting is separate.

### 13. Desktop and platform cleanup

Delete:

- `osu.Desktop/NVAPI.cs` and all NVIDIA driver-profile startup calls;
- `SDL2BatteryInfo`, `SDL3BatteryInfo`, the application battery abstraction, and battery presentation;
- `ElevatedPrivilegesChecker` and its warning-only path;
- mobile runtime branches, Android/iOS workarounds, mobile disclaimer, mobile safe-area assumptions, and mobile touch gestures;
- `OSU_PROTOCOL`, `OsuSchemeLinkIPCChannel`, command-line `osu://` forwarding, URI handling in `OsuGame`, and Windows URI registration;
- `.osk` association and skin archive forwarding;
- Windows association branches for anything except `.osz` and `.osr`.

Retain:

- `MacOSAppLocationChecker`;
- Windows per-user installation integration required by the desktop package;
- Windows-key blocking during gameplay;
- archive IPC/single-instance forwarding for `.osz` and `.osr`;
- real Linux and macOS storage/window/launch paths.

Guard Windows association registration with an OS check so `CA1416` is eliminated rather than suppressed.

### 14. Resource payload trim

The current external resource assembly is not acceptable for the final product. It is 125.29 MB and contains resources for deleted rulesets, online UI, multiplayer, old skins, intros, web localisation, seasonal content, and upstream tooling.

Remove the `ppy.osu.Game.Resources` package reference and embed an allowlisted resource set in `osu.Game`. Do not add another project.

The allowlist contains only:

- fonts and icons directly requested by retained UI;
- shaders directly requested by retained gameplay/UI;
- textures directly requested by retained menu, song select, gameplay, pause/fail/results, settings, dialogs, and import UI;
- UI and gameplay samples directly requested by retained code;
- the classic legacy fallback elements required when Kanna or a beatmap skin lacks an element;
- localisation resource sets and languages for retained strings;
- the trimmed Kanna archive.

Remove all resource families belonging to:

- intros, bundled tracks, seasonal themes, ranked play, multiplayer, matchmaking, chat, social, online, daily challenge, medals, supporter UI, web UI, changelog, update, first run, stable import, editor, skin editor, mod selection, other rulesets, Argon, Argon Pro, Retro, Triangles, tournament, mobile, notification drawer, country flags, avatars, and remote headers/covers;
- deleted localisation wrapper classes and deleted settings/actions;
- deleted shaders such as Argon-only paths and other removed visual systems;
- unused menu background variants and old skin assets.

Use both static resource-name extraction and a temporary development resource-lookup log during the full smoke corpus. The final commit must remove that instrumentation. A missing lookup is fixed by adding the specific required resource or removing the stale request; it is not fixed by restoring the upstream package.

Acceptance for this phase:

- no `ppy.osu.Game.Resources` package reference;
- no `osu.Game.Resources.dll` in output;
- no bundled `.osz` or intro track;
- no removed feature name in the embedded resource manifest;
- at least a 70% reduction from the current 124.64 MB game-resource stream payload;
- zero missing-resource log entries across the validation corpus.

### 15. Direct dependency removal

Remove these direct packages and their remaining convenience-only consumers:

| Package | Required action |
|---|---|
| `AutoMapper` | Replace the single Realm copy path with explicit mapping, then remove the package. |
| `Humanizer` | Replace retained duration/date/title formatting with small explicit helpers; delete mod/import-only calls and `HumanizerUtils`. |
| `Microsoft.Toolkit.HighPerformance` | Replace the two remaining consumers (`ZipArchiveReader` and `Triangles`) with BCL code or delete the consumer, then remove the package. |
| `System.ComponentModel.Annotations` | Remove the unused database-generation annotation/interface and package. |
| `System.IO.FileSystem.Primitives` 4.3.0 | Remove the legacy compatibility pin. Fix the net8 dependency graph rather than restoring it. |
| `System.Runtime.InteropServices` 4.3.0 | Remove the legacy compatibility pin. |
| `System.Runtime.Handles` 4.3.0 | Remove the legacy compatibility pin. |
| `ppy.osu.Game.Resources` | Replace with the allowlisted embedded resources described above. |

Retain `MessagePack`, `Newtonsoft.Json`, `Realm`, `ppy.osu.Framework`, and `SharpCompress` because the final replay, beatmap, persistence, framework, and archive paths directly use them. Retain the banned-API and localisation analyzers as build-time guardrails.

Run a final transitive-output review. A transitive assembly that remains solely because of a deleted direct package or deleted feature must also disappear from the publish output. Do not attempt to remove a framework transitive dependency that the retained framework genuinely loads.

### 16. NuGet/package metadata

The final repository ships an application, not reusable upstream NuGet packages.

Delete:

- `IsPackable=true`, NuGet titles/package IDs/versions, and package icon metadata from `osu.Game` and `osu.Game.Rulesets.Osu`;
- `assets/lazer-nuget.png`;
- template/custom-ruleset packaging assumptions;
- deployment steps that pack Game or the ruleset.

Set library projects non-packable. Keep the three projects as internal application boundaries.

### 17. Repository and documentation cleanup

Delete outright:

- `.github/FUNDING.yml`;
- `.run/`;
- tracked `.idea/` project files;
- `osu.sln.DotSettings` after confirming `.editorconfig` carries required formatting rules;
- `osu.Desktop.slnf`, which is redundant when the solution contains only three projects;
- stale test/tournament/mobile launch and task entries from `.vscode/`;
- `.config/dotnet-tools.json`, `InspectCode.ps1`, and `InspectCode.sh`;
- `UseLocalResources.ps1` and `UseLocalResources.sh` after the external resources package is removed;
- `.git-blame-ignore-revs`;
- `OSU_LITE_MAP.md`, `OSU_LITE_PLAN.md`, `OSU_LITE_POST_TRIM_REVIEW.md`, and `OSU_LITE_THIRD_TRIM_GUIDE.md` after this guide is completed and its final evidence is recorded here.

CI and dependency-update workflow configuration are outside this trim. Do not repair, replace, or measure `.github/workflows/ci.yml`, and do not treat its state as a completion blocker.

Rewrite the issue template/config for `waspflannel/osu-lite`, desktop-only reproduction, local logs, and the actual supported platforms. Remove ppy discussions, ppy priority labels, mobile instructions, and upstream links.

Rewrite `README.md` to describe the final offline local player, supported formats, supported platforms, build instructions, fixed skin, replay admission policy, and external-browser exceptions. Rewrite `CONTRIBUTING.md` for this repository and remove upstream roadmap, dev server, Discord, mobile, test-project, and ppy issue-tracker instructions.

Keep `LICENCE`, application icon/manifest, `Directory.Build.props`, analyser rules, Fody configuration, licence header configuration, local-framework scripts, and one clean solution. Keep a minimal Desktop-only `.vscode` launch/build configuration.

Do not add a test project merely to replace the deleted upstream tests. The manual build and smoke corpus below are the required validation surface for this trim.

### 18. Verified declaration-only and dead files still present

The following current types have no external source reference beyond their own declaration/implementation and must be deleted. They are not optional polish:

- `Graphics/UserInterface/BarGraph.cs`
- `Graphics/UserInterface/HistoryTextBox.cs`
- `Graphics/UserInterface/LoadingButton.cs`
- `Graphics/UserInterface/OsuNumberBox.cs`
- `Graphics/UserInterface/OsuPasswordTextBox.cs`
- `Graphics/UserInterface/PageTabControl.cs`
- `Graphics/UserInterface/TernaryStateToggleMenuItem.cs`
- `Graphics/UserInterface/ToggleMenuItem.cs`
- `Graphics/UserInterfaceV2/ColourPalette.cs`
- `Graphics/UserInterfaceV2/LabelledDropdown.cs`
- `Rulesets/Difficulty/Skills/StrainDecaySkill.cs`
- `Rulesets/Difficulty/Utils/ReverseQueue.cs`
- `Rulesets/Scoring/AccumulatingHealthProcessor.cs`
- `Screens/Backgrounds/BackgroundScreenCustom.cs`
- `Database/TooManyDownloadsNotification.cs`
- `Overlays/OverlayRulesetTabItem.cs`
- `Overlays/OverlayScrollContainer.cs`
- `Overlays/OverlayTabControl.cs`
- `Rulesets/UI/ModSwitchSmall.cs`
- `osu.Game.Rulesets.Osu/HUD/AimErrorMeter.cs`
- `osu.Game.Rulesets.Osu/Skinning/Default/ManualSliderBody.cs`
- `Database/ImportProgressNotification.cs`
- `Database/INamedFileInfo.cs`
- `Graphics/Containers/OsuRearrangeableListItem.cs`
- `Graphics/UserInterface/ExpandableSlider.cs`
- `IO/Serialization/Converters/SnakeCaseStringEnumConverter.cs`
- `Localisation/PopupDialogStrings.cs`
- `Screens/Select/LocalScoreDeleteDialog.cs`
- `Utils/TaskChain.cs`

Also delete declaration-only extension containers and online-shaped model helpers found by the final reference scan, including `BeatmapInfoExtensions`, `BeatmapSetOnlineStatusExtensions`, `BeatSyncProviderExtensions`, `DrawableExtensions`, `NumberFormattingExtensions`, unused slider-path/type extensions, and other static classes whose members have no retained caller.

Some types are instantiated by dependency injection, Realm generation, skin lookup, or framework reflection. For every apparent declaration-only type, check those four mechanisms. If the type belongs to a removed feature, delete both the registration/metadata and the type. If a retained runtime lookup directly requires it, record the concrete lookup in the implementation commit. Reflection is not a blanket exemption.

### 19. Method-, field-, and localisation-level sweep

After every whole-feature deletion, perform a member-level sweep of retained files. Remove:

- constructor parameters, resolved dependencies, bindables, events, commands, and fields for removed features;
- empty handlers, no-op switch cases, unreachable branches, compatibility comments, and suppression attributes;
- interface members with one implementation and no polymorphic caller;
- model properties and Realm indexes for online IDs, rulesets, mods, skins, API users, old schema versions, and removed settings;
- serializer converters and JSON properties used only by API/mod/skin payloads;
- localisation wrapper properties, resource keys, captions, tooltips, menu tips, and action descriptions for deleted UI;
- resource lookup strings for deleted textures, samples, shaders, and tracks;
- `using` directives and package references exposed by those deletions.

Specific helper pruning still required:

- `TimeDisplayExtensions`: retain only methods with a live local score/result/playback caller;
- `StringDehumanizeExtensions`: remove the class; inline the one required casing conversion beside a retained serializer if that serializer still needs it;
- `JsonSerializableExtensions`: retain only beatmap/score serialization calls;
- `RealmExtensions` and `RealmObjectExtensions`: keep only methods called by the final local schema and explicit mapping;
- `FormatUtils`: remove web-relative-time and deleted-localisation branches while retaining score/time/number formatting;
- `OsuGame`, `OsuGameBase`, `Player`, `ReplayPlayer`, `BeatmapManager`, `ScoreManager`, `Ruleset`, `DrawableRuleset`, and `RealmAccess`: review every member after their removed dependencies are gone. These large retained classes are the most likely place for intertwined residue.

Finish with a declaration/reference scan across all three projects. Every top-level type with no construction, inheritance, generic instantiation, registration, serializer contract, Realm model use, or direct static member call must be deleted. Do not stop at whole-file unused warnings; private members and enum values need the same treatment.

## Required implementation sequence

The order below is binding because it prevents compatibility infrastructure from keeping later features artificially alive.

### Phase 0 — Baseline and guardrails

1. Record C# file/line counts, direct packages, Release output size, and resource manifest size.
2. Record a clean Debug and Release build.
3. Record the current single warning.
4. Add temporary audit scripts or commands only when they are removed before the final commit.

### Phase 1 — Fresh identity and model boundary

1. Establish the `osu-lite` config/keybinding/Realm/storage identity.
2. Define the compact schema, `OsuSetting`, `GlobalAction`, and ruleset-settings allowlists.
3. Remove historical migrations, stable import, first-run, old enum ordinals, and old background jobs.
4. Build and launch a fresh profile before continuing.

### Phase 2 — Offline and local identity

1. Delete `osu.Game/Online/`, remote assets, URL routing, endpoints, and request types.
2. Replace API local-user state with the local player name.
3. Delete user/avatar/flag/profile models and online beatmap metadata.
4. Add the closed three-destination browser launcher.
5. Prove no in-process network capability remains.

### Phase 3 — Fixed skin

1. Replace `SkinManager` with the fixed provider.
2. Remove other skins, import/export/edit/database/layout paths.
3. Reduce legacy skin parsing to Kanna, beatmap-local overrides, and classic fallback.
4. Delete saved-layout/reflection HUD orphans.
5. Validate Kanna and map-local skins before continuing.

### Phase 4 — Remove the mod model

1. Add pre-storage unmodded replay admission.
2. Move autoplay to a direct replay factory.
3. Replace stable raw-zero Classic synthesis with replay compatibility context.
4. Remove all mod implementations, interfaces, persistence, UI, score fields, and calculator parameters.
5. Validate unmodded play, autoplay, replay import, replay export, and rejection.

### Phase 5 — One ruleset and narrow local data services

1. Register osu!standard explicitly from Desktop.
2. Remove dynamic ruleset stores, selectors, conversion, scrolling UI, and generic-only objects.
3. Narrow beatmap/score importers and managers to the retained formats/operations.
4. Remove editor/external-edit/beatmap-export paths.
5. Remove AutoMapper and simplify Realm helpers.

### Phase 6 — Final UI surface

1. Replace notification drawer with the operation sink.
2. Delete replay analysis.
3. Remove startup intros and bundled menu beatmaps.
4. Reduce settings to six sections and the exact allowlists.
5. Remove joystick, mobile/touch residue, dead actions, toolbar selectors, analog clock, menu tips, and decorative orphans.
6. Simplify results/song-select/HUD only where they still expose removed online/mod/user concepts.

### Phase 7 — Desktop and associations

1. Remove NVAPI, SDL2 fallback, elevated warning, mobile code, and `osu://`.
2. Retain only `.osz`/`.osr` associations and archive IPC.
3. Fix platform guards and reach zero warnings.
4. Build and launch on Windows, macOS, and Linux.

### Phase 8 — Resources and dependencies

1. Build the retained resource access list from the now-final code.
2. Replace the 125.29 MB upstream resource assembly with the embedded allowlist.
3. Trim the Kanna archive.
4. Remove the eight direct packages listed above.
5. Review publish output for removed transitive assemblies and resources.

### Phase 9 — Repository finish

1. Delete stale IDE files, scripts, package metadata, old trim documents, and upstream repository copy.
2. Rewrite README, contributing guide, and issue templates for the final product.
3. Run the final declaration/member/resource/reference sweep.
4. Record final counts and validation evidence in this document.

No phase may be skipped or merged into an unreviewable catch-all commit. Use focused commits so a regression can be located without restoring a deleted subsystem wholesale.

## Validation gates

### Build and static gates after every phase

- `dotnet build osu.Desktop -c Debug --no-restore`
- `dotnet build osu.Desktop -c Release --no-restore`
- zero errors and zero warnings;
- no newly unused direct package;
- no tracked generated `bin/`, `obj/`, local database, imported beatmap, or skin output;
- no stale solution or project reference.

### Final platform gates

- manually verified Debug and Release builds on Windows, macOS, and Linux;
- fresh-profile launch reaches MainMenu on all three;
- no first-run wizard or old-data prompt;
- clean exit and second launch work;
- Windows `.osz` and `.osr` associations work;
- macOS/Linux drag/drop or file-open import works;
- a second instance forwards `.osz`/`.osr` to the first instance.

### Final gameplay corpus

Use at least:

1. a simple circle map;
2. a slider-heavy map;
3. a spinner map;
4. a map with normal/soft/drum hitsounds;
5. a map with custom colours;
6. a map with beatmap skin assets;
7. a map with storyboard;
8. a map with video;
9. a map with storyboard and video interaction;
10. a map missing optional assets to exercise classic fallback;
11. pass, fail, retry, pause/resume, skip, screenshot, and results flows;
12. local score history and local in-game leaderboard;
13. Ctrl+Enter autoplay;
14. imported unmodded stable `.osr` playback;
15. exported `.osr` round-trip;
16. rejection of a stable mod-mask replay;
17. rejection of a replay with embedded mod settings;
18. file deletion, score deletion, data reset, and current-lite storage relocation;
19. keyboard, mouse, and tablet/pen input;
20. every retained settings control.

### Final source capability gates

Executable source must contain none of the following product concepts:

- `IAPIProvider`, `APIRequest`, `OsuWebRequest`, endpoint configuration, remote avatar URL, or a general URL router;
- `OSU_PROTOCOL`, `OsuSchemeLinkIPCChannel`, or `osu://` handling;
- `SkinManager`, `SkinImporter`, `LegacySkinExporter`, Argon/Retro/Triangles user skins, skin selection, or saved skin layouts;
- selectable `Mod`, `APIMod`, `ModsJson`, available/selected mods, mod settings, or mod display;
- dynamic ruleset assembly scanning, custom ruleset directories, ruleset selector, conversion, or scrolling ruleset UI;
- first-run setup, stable storage/import, old Realm-copy scan, or historical migration switch;
- notification drawer, unread notification count, notification sections, or notification global action;
- joystick settings, mobile disclaimer, iOS/Android runtime branch, NVAPI, or SDL2 battery fallback;
- replay analysis marker/path/settings types;
- intro sequence types or bundled intro tracks;
- `ppy.osu.Game.Resources` package/output assembly;
- editor/external-edit/beatmap-export actions;
- stale upstream deployment, mobile, tournament, template, Sentry, diffcalc, or web-mod references.

The only executable HTTP URL constants are the three locked browser destinations. Documentation/source comments are excluded from that check.

## Definition of done

The final trim is complete only when all of the following are true:

- the final product contract is the complete reachable product surface;
- every removal ledger section has been implemented;
- no item was deferred for being small, intertwined, risky, or low-value;
- there is no general in-process network capability;
- Kanna is the only user skin, with map-local overrides and classic fallback;
- there is no skin library, mod system, dynamic ruleset system, stable importer, first-run wizard, notification drawer, replay analysis, or startup intro;
- autoplay works without a mod model;
- unmodded `.osr` import/playback/export works and every mod-bearing replay is rejected before storage;
- local `.osz` import and all retained map-fidelity features work;
- settings have exactly six sections and only retained controls;
- the compact settings/action/schema namespaces contain no tombstones or old migration obligations;
- Windows, macOS, and Linux build and launch;
- Debug and Release builds have zero warnings;
- the upstream resource assembly and all removed resource families are absent;
- direct packages match the final dependency list;
- no verified dead type/member/resource remains;
- repository documentation describes this product rather than upstream osu!; CI automation is excluded;
- final source, package, resource, and publish-output measurements are recorded below.

## Final evidence to record after implementation

| Measurement | Pre-final baseline | Post-merge audit | Completed product |
|---|---:|---:|---:|
| Tracked C# files | 1,478 | 1,395 | — |
| Physical C# lines | 174,108 | 163,544 | — |
| Approximate non-blank C# lines | 143,495 | 134,769 | — |
| Direct package references in `osu.Game` | 13 | 10 | — |
| Debug warnings | not separately baselined | 0 | — |
| Release warnings | 1 | 0 | — |
| Release output directory | 583.5 MB | 583.04 MB | — |
| Game resource stream payload | 124.64 MB | 124.64 MB | — |
| Game resource manifest entries | 1,651 | 1,651 | — |
| Windows build/launch | current build only | build verified; launch not re-audited | — |
| macOS build/launch | not baselined | not re-audited | — |
| Linux build/launch | not baselined | not re-audited | — |

When every definition-of-done item outside the explicit CI exclusion passes, replace each dash with measured evidence and mark this document completed. At that point there is no planned additional product-wide trim: further changes are normal maintenance or deliberate product-scope changes.

## Execution log

### 2026-07-15 — Phase 2 complete

- `210e22b6a6` closes arbitrary browser routing. Browser access now accepts only `ExternalBrowserDestination` and maps internally to the three locked destinations. The request-independent link parser, URL handler, warning dialog, endpoint selection, and profile-link actions were deleted. Debug and Release builds passed with zero warnings.
- `ea85d45828` introduces the configuration-backed `LocalPlayerName` identity and removes credential and external-link-warning settings. The retained skinnable player-name component now binds to the local name rather than an API user. Debug and Release builds passed with zero warnings.
- `bc42bc83ef` removes API/Realm score ownership. Imported replays and score presentation are local-only, while replay encoding uses the local player name.
- `06739b68ee` changes beatmap creator metadata to text and removes avatar, flag, profile, results, and HUD user-presentation types.
- `bbc2c40af2` deletes `Online/` and the remaining `Users/` surface, including the API provider/request stack, endpoints, API models, online beatmap metadata, report popover, and arbitrary URL helpers. Debug and Release Desktop builds passed with zero warnings. Exact source scans found no `IAPIProvider`, `APIRequest`, `OsuWebRequest`, `APIUser`, `IUser`, or `RealmUser` references; `ExternalBrowser` is the sole `OpenUrlExternally()` caller.

### 2026-07-15 — Phase 3 complete

- `57597934a0` replaces the Realm-backed multi-skin library with `FixedSkinProvider`, the embedded Kanna archive, beatmap-local overrides, and classic fallback. Skin selection, persistence, import/export/editing, saved layouts, non-Kanna bundled skins, mania skin branches, and their unused HUD components are deleted. Debug and Release Desktop builds passed with zero warnings; exact source scans found no legacy skin-manager, skin-importer, non-Kanna skin, or layout-serialisation type.

### 2026-07-15 — Phase 4 core model removal

- Score, replay-frame, gameplay, HUD, results, and difficulty/performance paths are unmodded-only. The score mod payload, mod-adjusted calculators, legacy score migration helper, and mod result/HUD presentation were removed. Legacy replay admission continues to reject non-zero stable masks and embedded mod entries before storage. Debug and Release Desktop builds passed with zero warnings.
- The remaining Phase 4 work is deletion of the now-unreachable mod implementations, their supporting UI/localisation/resources, and historical Realm migration branches. It is intentionally kept as a following commit so that the core API contraction remains independently reviewable.

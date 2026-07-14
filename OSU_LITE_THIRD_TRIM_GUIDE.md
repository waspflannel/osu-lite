# osu! lite third trimming guide

Reviewed: 2026-07-14  
Branch: `osu-lite-trim2`  
Reviewed HEAD: `5e17d35c2b` (`Set kanna 2.0 ultra lite as the fixed default skin`)

## Purpose

This is the third-pass removal guide. It is intentionally more aggressive than the earlier plans.

The rule for this pass is:

- Do not skip a finding because it is small.
- Do not skip a finding because it is intertwined with retained code.
- For intertwined code, identify the seam that must be introduced and remove the obsolete side of it.
- Every examined area must end in one of three states: **remove**, **retain for a stated product reason**, or **decision required**.
- A green build is necessary but not sufficient. Every phase has a runtime gate.

This document is a guide only. No source changes described below have been implemented as part of this review.

## Current state

The second trimming pass was substantial and successful:

- Relative to the post-first-pass baseline (`8b12c2e`), it changed 297 files, added 1,035 lines, and removed 19,318 lines.
- The solution contains three projects: `osu.Game`, `osu.Game.Rulesets.Osu`, and `osu.Desktop`.
- The tracked source contains 1,551 C# files: 1,316 in `osu.Game`, 223 in the osu! ruleset, and 12 in the desktop head.
- Approximate tracked C# size is 148,762 lines: 127,402 in `osu.Game`, 19,873 in the osu! ruleset, and 1,487 in `osu.Desktop`.
- All test projects are gone.
- A Release build of current HEAD succeeds with zero errors and one warning. The warning is CA1416 at `osu.Desktop/OsuGameDesktop.cs`, where `WindowsAssociationManager.UpdateAssociations()` is not proved to be Windows-only to the analyser.
- The untracked `skins/` directory was left untouched. It belongs to the working tree owner and is not part of this guide's changes.

The largest remaining `osu.Game` areas are:

| Area | Files | Lines |
|---|---:|---:|
| Screens | 244 | 32,022 |
| Graphics | 212 | 21,244 |
| Rulesets | 206 | 16,504 |
| Overlays | 162 | 15,569 |
| Beatmaps | 97 | 9,269 |
| Skinning | 84 | 7,415 |
| Database | 44 | 5,489 |
| Localisation | 62 | 4,419 |
| Online | 32 | 2,151 |
| Storyboards | 33 | 1,583 |

The codebase is now a working local-play client, but it is not yet a minimal local-play client. Several visible entry points were removed while their underlying systems remained almost intact.

## Corrections to the older review

`OSU_LITE_POST_TRIM_REVIEW.md` is now historical rather than current truth in several places:

- It still describes Argon as the fixed skin. Current HEAD embeds Kanna 2.0 ultra lite and selects it by default.
- It reports Phase D as partial even though later commits completed much of the surrounding work.
- It says the app makes no ppy network requests. Current source still contains an active downloader for `https://assets.ppy.sh/client-resources/online.db.bz2`.
- It describes `IAPIProvider` as narrowed. The interface still exposes request queuing, API language/token/version/session/state/endpoints, and a dummy local social state.
- It treats some tombstones as safe to leave. This pass explicitly resolves them.

At the end of the third pass, update the older documents to point to this guide or mark their claims as historical. Do not allow three documents to describe three different products.

## Product decisions required

These are real product boundaries, not excuses to defer cleanup. The recommended answer is shown so work can proceed as soon as the owner confirms it.

| ID | Question | Recommended answer | What changes with the answer |
|---|---|---|---|
| D1 | May the game open a normal web browser for Report Issue and hardware documentation, or must it be completely air-gapped? | Allow explicit browser links, but no in-process network access. | If browser links remain, keep a tiny external-link launcher. If air-gapped, delete it, its warning dialog/config, and all URL affordances. |
| D2 | Are local beatmap storyboards, videos, beatmap skins, beatmap colours, and beatmap hitsounds still part of the product? | Retain them for local map fidelity. | Removing any of these is a large but valid cut. Each should be a separate phase because each changes map presentation. |
| D3 | How much imported replay fidelity is required? | Retain all currently supported replay mods. | If only stable-era mods are required, delete lazer-only fun/experimental mods. If only autoplay is required, most mod implementations and mod-setting plumbing can go. |
| D4 | Must existing Realm databases, keybindings, and the optional osu!stable beatmap/score migration continue to work? | Preserve them and migrate deliberately. | If a clean profile is acceptable, old schema migrations, enum ordinals, stable migration UI, and compatibility models can be removed much more aggressively. |
| D5 | Is this a Windows-only product or a Windows/macOS/Linux desktop product? | Decide explicitly; current source still targets all three. | Windows-only permits removal of macOS/Linux branches, the macOS location checker, hard-link branches, SDL2 fallback work, and related localisation. |
| D6 | Should the osu! ruleset remain a separate project/plugin boundary? | Keep the three-project layout, but hard-code one registered ruleset. | Merging it into the game would allow deeper abstraction removal, but requires moving code to resolve the current project dependency direction. |
| D7 | Are joystick/gamepad input settings supported, or is the input contract keyboard + mouse + tablet/pen only? | Keyboard + mouse + tablet/pen only. | If confirmed, remove `JoystickSettings` and any gamepad-only configuration/binding presentation. |

No execution agent should silently choose a different answer. All other findings below can proceed without waiting.

### Decisions locked (2026-07-14)

The product owner has answered D1-D7. These are now binding for execution and supersede the "Recommended answer" column where they diverge:

| ID | Answer given | Notes for execution |
|---|---|---|
| D1 | Allow explicit browser links; no in-process network otherwise. | Keep a tiny OS-browser launcher with hard-coded destinations for Report Issue and hardware documentation. Delete the general URL router, arbitrary URL handling, and external-link warning dialog/preference. |
| D2 | Retain local beatmap storyboards, videos, beatmap skins, colours, and hitsounds. | Retain only their map-local playback pipelines. This does not justify user skin import/export/selection, general skin management, or other online-era presentation infrastructure. |
| D3 | Autoplay is the only supported gameplay mod; unmodded replay import/playback remains. | Autoplay is a local Ctrl+Enter path, not an imported-replay fidelity promise. Reject every `.osr` containing a mod flag or embedded mod entry—including Autoplay—during import, with a clear local notification; do not store or display unsupported modded scores. Remove the broad mod catalogue, `APIMod`/`ModsJson` persistence, mod-setting controls/trackers, and mod displays. A minimal replay-metadata admission check is still required. Stable replays with a raw zero mod mask currently synthesize `ModClassic`; treat those as unmodded and keep only the smallest internal Classic compatibility path until its behaviour is deliberately inlined. |
| D4 | A one-time clean profile boundary is acceptable for the third pass. | Do not copy, open, or migrate pre-third-pass Realm databases, keybindings, config, or osu!stable data. Establish a fresh lite profile/database namespace, remove the historical migration switch and backward database-copy scan, and delete the stable-import product. Keep only a minimal schema-version foundation for future lite-to-lite upgrades. Obsolete rows, models, settings, and enum members need no transition migration. |
| D5 | Cross-platform desktop (Windows/macOS/Linux). | Retain real Windows, macOS, and Linux paths; cut only mobile and stale test compatibility. Add build and launch gates for all three retained platforms. |
| D6 | Keep the three-project layout; hard-code one registered ruleset. | Matches recommended. No project merge in this pass. |
| D7 | Keyboard + mouse + tablet/pen only. | Matches recommended. Remove `JoystickSettings` and gamepad-only configuration/binding presentation. |

**Status: confirmed and binding for third-pass execution. No removal work has started as part of this guide update.**

## Highest-priority contract failures

### P0: the app can still contact ppy infrastructure

`osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs` downloads `online.db.bz2` when `online.db` is missing or over one month old. It is constructed by both `BeatmapUpdaterMetadataLookup` and `BackgroundDataStoreProcessor`. A machine with a warm cache can appear offline during a smoke test while a clean machine makes a request.

Remove the full metadata-cache path:

- `LocalCachedBeatmapMetadataSource`
- `IOnlineBeatmapMetadataSource`
- `OnlineBeatmapMetadata`
- `BeatmapUpdaterMetadataLookup`
- `MetadataLookupScope`
- `OsuSetting.LastOnlineTagsPopulation`
- submission/rank-date backpopulation and online user-tag backpopulation in `BackgroundDataStoreProcessor`
- the always-inert `processOnlineBeatmapSetsWithNoUpdate()` path
- the `preferOnline`/online-first/local-cache-first arguments threaded through `BeatmapImporter`, `BeatmapManager`, `BeatmapUpdater`, and `IBeatmapUpdater`

Then remove the now-single-consumer packages:

- `Microsoft.Data.Sqlite.Core`
- `SQLitePCLRaw.bundle_e_sqlite3`

Follow-on local data cleanup:

- `BeatmapMetadata.UserTags` currently has no local authoring path; its only producers are the downloaded metadata cache. Remove the field, query parsing, carousel filtering, wedge display, and Realm schema member. Do not add a transition migration under the locked D4 policy.
- `BeatmapSetInfo.DateRanked` and `DateSubmitted` are only populated by the same metadata path in current source. Remove Date Ranked sorting/grouping/search and the submitted/ranked display row; D4 explicitly rejects retaining historical values.
- `APITag`, `Screens/Ranking/UserTag`, `Static.AllBeatmapTags`, and related API response fields should then be removed.

### P0: remote preview and texture infrastructure is still registered

`PreviewTrackManager` is constructed and added to the dependency graph on every startup. Its `Get()` method has no caller, but it creates a `TrustedDomainOnlineStore` capable of fetching `b.ppy.sh/preview/{id}.mp3`.

Remove:

- `Audio/PreviewTrackManager.cs`
- `Audio/PreviewTrack.cs`
- `Audio/IPreviewTrackOwner.cs`
- the owner coupling in `OsuFocusedOverlayContainer`
- the startup registration in `OsuGameBase`

`OsuGameBase` also adds `CreateOnlineStore()` to the global `LargeTextureStore`. Remove that texture source and `TrustedDomainOnlineStore`. Local beatmap backgrounds already use local resource stores.

After this phase, the acceptance check is not merely “no request was observed.” There must be no source path capable of opening HTTP from the normal runtime, apart from the locked, hard-coded OS browser launcher.

### P0: Kanna is selected through the old multi-skin product

The new fixed skin is embedded correctly, but only the entry point is fixed. `SkinManager` still behaves like a general skin library:

- It inherits `ModelManager<SkinInfo>` and implements `IModelImporter<SkinInfo>`.
- It constructs `SkinImporter` and `LegacySkinExporter`.
- It creates and writes Retro, Classic, Triangles, Argon, Argon Pro, and Kanna records into Realm.
- It retains random-skin, query, save, mutable-copy, import/update, external-edit, export, delete, rename, enumeration, and configuration-switching paths.
- `OsuConfigManager` still defines `OsuSetting.Skin` with Argon as the unused default.
- Comments in `OsuGameBase` and `OsuGame` still say the game is locked to Argon.

Reduce `SkinManager` to a fixed skin source and resource provider. The target source chain should be explicit:

1. Kanna legacy skin.
2. Classic legacy fallback for assets Kanna does not contain.
3. Retained beatmap skin transformer/resources for map-local skin fidelity.

Triangles should not remain as a silent third fallback unless a runtime test proves a required asset exists nowhere else.

Delete after the fixed source chain is working:

- `SkinImporter.cs`
- `Database/LegacySkinExporter.cs`
- the skin-specific use of `LegacyArchiveExporter`
- user-skin import/update/external-edit/export/delete/rename/query/enumeration code
- random-skin state and all built-in skin Realm records
- `ArgonSkin.cs`, `ArgonProSkin.cs`, `RetroSkin.cs`, `TrianglesSkin.cs`
- `Skinning/Triangles/`
- `osu.Game.Rulesets.Osu/Skinning/Argon/` (17 files, about 1,589 lines)
- Argon-only HUD and component classes under `Screens/Play`, `Screens/Play/HUD`, `Screens/Play/HUD/ArgonHealthDisplayParts`, and `Skinning/Components`
- `SkinInfo` built-in GUIDs other than the fixed skin and classic fallback
- `OsuSetting.Skin`

Once no user skin is stored, convert `SkinInfo` from a Realm/file-management model into the smallest immutable descriptor required by `Skin` and `LegacyBeatmapSkin`, or remove it entirely in favour of constructor data. The fresh D4 schema must never contain the old `Skin` rows/model; do not write a transition migration for it.

The embedded `.osk` is a legacy skin archive containing `skin.ini` plus image/audio resources. It contains no lazer layout JSON. Therefore the general saved-layout/customisable-skin system is not load-bearing for Kanna.

After fixed-skin conversion, remove the lazer layout/customisation path:

- `SkinLayoutInfo`
- `SerialisedDrawableInfo`
- `SerialisableDrawableExtensions`
- `UserSkinComponentLookup`
- save/update layout methods on `Skin`
- the mutable `SkinnableContainer` editing API
- serialisable HUD components that are never directly constructed by the retained Kanna/classic path

The refactor must first change `SkinnableContainer`/`HUDOverlay` to hold the direct drawable components returned by the fixed source. Do not keep the serialisation layer merely because active legacy HUD components currently implement `ISerialisableDrawable`.

Also prune mania-only skin decoding from `LegacySkin` and delete `LegacyManiaSkinConfiguration`, `LegacyManiaSkinConfigurationLookup`, and `LegacyManiaSkinDecoder`; no mania ruleset remains.

### P0: the offline API is still an online API-shaped compatibility layer

`IAPIProvider` still exposes request methods and network concepts. `DummyAPIAccess` supplies fake tokens, a date-derived API version, localhost endpoints, request attach/fail methods, connection state, a session ID, and empty friends/blocks/favourites lists.

The API request stack has no live concrete request. Its only UI consumer is the orphaned abstract `ReportPopover`.

Delete:

- `APIRequest.cs`
- `APIDownloadRequest.cs`
- `ArchiveDownloadRequest.cs`
- `OsuWebRequest.cs`
- `OsuJsonWebRequest.cs`
- `APIException.cs`
- `APIRequestCompletionState.cs`
- `Graphics/UserInterfaceV2/ReportPopover.cs`
- request methods and request-only properties from `IAPIProvider`/`DummyAPIAccess`
- `APIState` with the removed provider session/state surface
- `EndpointConfiguration`, `ProductionEndpointConfiguration`, and `DevelopmentEndpointConfiguration` after the two retained browser destinations are hard-coded outside the API layer

Replace the remaining “API provider” with a local-player context containing only what active code needs, likely a bindable local user identity. Do not carry online DTOs forward under D3/D4: delete `SoloScoreInfo` and `APIMod`; remove `ScoreInfo.ModsJson`/`APIMods`; and remove `APIBeatmap`/`APIBeatmapSet` after deleting their online-info field, dead conversion helper, and historical Realm references. New local scores are unmodded, local autoplay runs through `ReplayPlayer` and is not imported as a local score, and unmodded imported replay playback can reconstruct internal Classic behaviour from the raw replay rather than persisted mod DTOs. None of these types justifies retaining an `Online` namespace or provider abstraction.

Remove the empty favourites path at the same time:

- `ILocalUserState`
- `DummyLocalUserState.Friends`, `.Blocks`, and `.FavouriteBeatmapSets`
- `APIRelation`
- Favourites and My Maps group modes and grouping logic
- local-user favourite bindings in `FilterControl`/`BeatmapCarousel`

The fixed local user ID makes My Maps semantically invalid, and favourites can never be populated.

### P1: most internal link actions are deliberate no-ops

`OsuGame` parses and dispatches beatmap, beatmap-set, channel, profile, search, wiki, changelog, spectate, and external links. All in-app online destinations are empty methods.

Remove:

- empty `ShowChannel`, `ShowBeatmapSet`, `ShowUser`, `ShowBeatmap`, `SearchBeatmapSet`, `ShowWiki`, `ShowChangelogListing`, and `ShowChangelogBuild`
- their `ILinkHandler` surface and `LinkAction` values
- their parser branches in `MessageFormatter`
- clickable mapper/profile/avatar behaviour that targets those no-ops
- online beatmap links in results
- the unsupported spectate notification branch
- `DrawableLinkCompiler` and comprehensive web/chat/wiki formatting if no real consumer remains

`osu://` currently exists only as a parser/IPC route to these removed actions. Remove `OsuSchemeLinkIPCChannel`, `OSU_PROTOCOL`, protocol registration, and forwarding. Retain `ArchiveImportIPCChannel` for single-instance `.osz`/`.osr` forwarding.

Replace the online link system with a tiny external URL launcher used only by concrete buttons such as Report Issue or tablet documentation. Destinations must be hard-coded rather than accepted from beatmap/chat/IPC data. Remove `ExternalLinkWarning`, its dialog, and its config entry; clicking one of these explicit buttons is the user confirmation.

### P1: the permanent notification centre is still loaded

The toolbar button is gone, but `NotificationOverlay` still creates a 320-pixel drawer, permanent sections, read/unread state, history forwarding, drawer animation, overlay layout participation, and `NotificationSection`. No live code shows or toggles the drawer.

Retain the active local responsibilities:

- transient notifications
- progress state
- cancel actions
- completion actions
- critical/error visibility
- the list of ongoing progress operations used by exit confirmation

Remove:

- `NotificationSection.cs`
- permanent storage and history sections
- `ForwardToOverlay`
- read/unread state and counters
- `MarkAllRead`
- drawer title/description/icon semantics
- drawer `PopIn`/`PopOut`/width/layout behaviour
- `Hide()` if it only exists for the deleted drawer
- the zero-use `ToggleNotifications` action member from the fresh action enum and default bindings

Rename the result from `NotificationOverlay` to a name that reflects its remaining role, such as `NotificationToastSink`. `INotificationOverlay` should expose `Post()` plus ongoing operations, not a hidden notification-centre API.

## Single-ruleset cleanup

Only osu!standard ships, but the runtime still discovers, persists, selects, converts between, and displays multiple rulesets.

Mandatory removals under locked D6:

- `ToolbarRulesetSelector` and its one-item toolbar tab
- `OverlayRulesetSelector`, `RulesetSelector`, and the dead `TabControlOverlayHeader` path where no other consumer remains
- `OsuSetting.Ruleset` and configured ruleset restoration
- `OsuSetting.ShowConvertedBeatmaps`
- converted-beatmap toggles in settings/filter/panels/no-results UI
- branches that preserve or switch to another ruleset in `PresentBeatmap`, song select, and carousel filtering
- `RulesetSettingsSubsection` if it has no single-ruleset consumer
- stale “other ruleset,” conversion, and custom-ruleset comments

Delete the entire `osu.Game/Rulesets/UI/Scrolling/` directory (11 files). It exists for removed taiko/mania-style scrolling rulesets. The only external consumer is a defensive `IDrawableScrollingRuleset` check in `Player`; collapse that check and delete `IncreaseScrollSpeed`/`DecreaseScrollSpeed`, their bindings, historical migration references, and localisation.

Also delete confirmed generic leftovers with no remaining implementation:

- `Rulesets/Objects/BarLineGenerator.cs`
- `Rulesets/Objects/IBarLine.cs`
- `Rulesets/Objects/Types/IHasColumn.cs`
- `Rulesets/Objects/Types/IHasHold.cs`

Then reduce `RealmRulesetStore`:

- register exactly osu!standard
- remove assembly scanning for extra/custom rulesets
- remove duplicate online-ID checks across rulesets
- remove broken custom-ruleset crash attribution/disable logic
- stop iterating all rulesets in settings, background processing, and configuration caches
- define only the osu!standard `RulesetInfo` in the fresh Realm schema; do not import or migrate stale taiko/catch/mania rows

Keep the three-project boundary and the ruleset base abstractions required by `osu.Game.Rulesets.Osu`, but stop pretending the product supports runtime plugins. Do not merge projects during this pass.

## Mod and replay cleanup

The mod-select UI is gone, but `OsuGameBase` still builds `AvailableMods` for every `ModType`, keeps global `SelectedMods`, converts selected mods on ruleset changes, and carries comments describing a mod-select overlay. No runtime selection UI writes to these values.

Remove under locked D3:

- global `AvailableMods`
- startup enumeration of all mod types for UI display
- global `SelectedMods`; Ctrl+Enter should pass autoplay directly into the local play request
- ruleset-change conversion of a selection the user cannot make
- stale mod-select comments in `OsuGameBase` and `FilterControl`
- the empty “Mods” settings subsection wrapper, `IncreaseFirstObjectVisibility`, `ModWithVisibilityAdjustment`, and their localisation/config plumbing after the visual-impairment mods are deleted
- `Localisation/ModSelectOverlayStrings.cs`
- the unused `MenuTipStrings.TryNewMods` member

The locked replay contract is exact:

1. Ctrl+Enter autoplay remains and `OsuModAutoplay` is the only user-facing gameplay mod.
2. Unmodded `.osr` import and playback remain.
3. Inspect the raw legacy mod mask and any embedded lazer mod metadata before constructing gameplay mods. Reject an archive containing any mod—including Autoplay—with a clear transient notification, before storing a score or replay model.
4. A stable replay with a raw zero mod mask is still unmodded even though `LegacyScoreDecoder` currently appends `ModClassic`. Preserve only the smallest internal `ModClassic`/`OsuModClassic` compatibility path required to play such a replay, or inline that behaviour and delete the classes in the same phase. Reconstruct it from the raw replay at playback time; do not persist or expose Classic in settings, selection, or score presentation.
5. Delete all other mod implementations, generic acronym-to-factory catalogues, `MultiMod` presentation construction, `APIMod`, `ScoreInfo.ModsJson`/`APIMods`, mod-setting controls/trackers, mod displays, and score/difficulty branches that become unreachable.

Do not keep thousands of lines of mod code under an unstated “maybe replay” assumption. Every implementation remaining after this phase must be reachable from autoplay or the explicitly documented internal unmodded-stable compatibility path.

## Beatmap, import, and database cleanup

### Remove editor-only BeatmapManager APIs

The editor is gone, but `BeatmapManager` still exposes editor/save APIs with no callers:

- `CreateNew`
- `CreateNewDifficulty`
- `CopyExistingDifficulty`
- `Save`
- `DeleteDifficultyImmediately`
- `ImportAsUpdate`
- `BeginExternalEditing`
- `ExportLegacy`

Keep `Restore(BeatmapInfo)`; song select still calls it.

Deleting the unused save/export path should permit removal of:

- `Database/LegacyBeatmapExporter.cs`
- `LegacyArchiveExporter.cs` once the skin exporter is also gone
- most or all of `LegacyBeatmapEncoder.cs`
- `LegacyStoryboardEncoder.cs`

Move `FIRST_LAZER_VERSION` to a small format-version constant if decoders still need it. Do not retain a full encoder for one constant.

### Remove external-edit infrastructure everywhere

`ExternalEditOperation<TModel>` has no user-facing caller. Remove it end-to-end rather than leaving default throwing methods:

- delete `Database/ExternalEditOperation.cs`
- remove `ImportAsUpdate` and `BeginExternalEditing` from `IModelImporter<T>`
- remove their default implementation from `RealmArchiveModelImporter`
- remove overrides/delegates from beatmap, skin, and score importers/managers

Normal `.osz` import and unmodded `.osr` import remain. Modded `.osr` files must follow the D3 rejection path.

### Collapse the beatmap updater

`IBeatmapUpdater` has one implementation. Its `Queue()` method has no caller. After online metadata removal:

- delete `Queue()` and its scheduler if synchronous/local processing is sufficient
- delete the interface or replace it with a narrow local beatmap processor
- make `Process()` recalculate local statistics/difficulty only
- remove lookup-scope parameters from the entire call chain
- rename online-era methods and comments

### Audit historical background jobs

`BackgroundDataStoreProcessor` runs a series of compatibility scans on every startup. Remove the online/update jobs immediately. Under locked D4, delete every remaining score/statistic/rank processor whose only purpose is upgrading an older lazer database. Retain only processing required for data created by the new lite schema, and document the live producer and consumer for each retained job.

### Remove AutoMapper rather than exempting it as intertwined

AutoMapper has one source consumer: `Database/RealmObjectExtensions.cs`. It implements detach/copy operations for a small known set of Realm models.

Replace those mappings with explicit cloning/copy methods and remove the AutoMapper package. This is not a “low-value” dependency cleanup: it removes runtime reflection/configuration and makes the retained database model graph visible.

## Settings, configuration, and keybindings

### User-facing settings target

The current overlay always creates eight top-level sections. `DebugSection` appears in Release because `MemorySettings` is added outside the debug-build condition.

Target six concise sections:

1. General: language, interface, main menu, song select, installation, and log export.
2. Input: active device settings and active keybindings only.
3. Gameplay: HUD, local beatmap presentation, offsets, and gameplay input behaviour.
4. Audio.
5. Graphics: renderer, display/layout, video, and screenshots.
6. Data: delete/restore/reset and storage-location operations only.

Remove `UserInterfaceSection` by merging its three small subsections into General. Delete `DebugSection`, `BatchImportSettings`, and `MemorySettings` from the shipping product rather than leaving a mostly empty release section. Developers can use framework diagnostics outside the user settings product.

Remove the fake `ModsSettings` section header. Review every remaining subsection wrapper and merge wrappers that contain one control.

### Confirmed dead OsuSetting entries

These have no consumer beyond their declaration/default and should be removed:

- `BeatmapDetailModsFilter`
- `BeatmapDetailTab`
- `FloatingComments`
- `LastProcessedMetadataId`
- `PreferNoVideo`
- `Skin`

Remove these with their dependent feature cuts:

- `Ruleset` and `ShowConvertedBeatmaps` with single-ruleset collapse
- `LastOnlineTagsPopulation` with the metadata cache
- `Username`, `Token`, `SavePassword`, and `SaveUsername` with the dummy auth state
- `ShowMobileDisclaimer` with mobile residue
- `HideCountryFlags` when dead flag/online identity components are removed
- `ExternalLinkWarning` with the locked hard-coded browser buttons
- `ShowFirstRunSetup` with the removed stable-import product

Do not preserve `OsuSetting` ordinals automatically: these settings are string-keyed in config. Verify there is no integer serializer, then delete and compact.

Delete the complete osu!stable import product under D4:

- `FirstRunSetupOverlay`, `Overlays/FirstRunSetup/`, `WizardOverlay`, and `WizardScreen`
- the no-results link that reopens first-run setup and the related localisation
- `LegacyImportManager`, `LegacyModelImporter`, `LegacyBeatmapImporter`, and `LegacyScoreImporter`
- `StableStorage`, `StableDirectorySelectScreen`, `StableDirectoryLocationDialog`, `GetStorageForStableInstall()`, and the desktop override
- `ImportParameters.PreferHardLinks`, the now-unreachable hard-link branch in `RealmFileStore`, `HardLinkHelper`, and its platform P/Invokes/localisation

Do not confuse stable import with the retained user-data storage relocation screens under Settings. Normal `.osz` and permitted `.osr` archive import remains.

### Remove dead session state

Delete:

- `Static.FeaturedArtistDisclaimerShownOnce`
- `Static.DailyChallengeIntroPlayed`
- `Static.AllBeatmapTags`

Rename `Static.UserOnlineActivity` and `UserActivity` to a local screen/window-title concept, or replace the bindable with direct window-title state. It is no longer broadcast online.

### Resolve GlobalAction tombstones and editor bindings

The enum still contains zero-use tombstones:

- `ToggleChat`
- `ToggleSocial`
- `ToggleBeatmapListing`
- `ToggleNotifications`
- `ToggleModSelection`
- `DeselectAllMods`
- `ToggleProfile`
- `IncreaseModSpeed`
- `DecreaseModSpeed`

It also still defines and registers roughly 31 editor actions and four editor-test actions, even though the editor is gone. `ToggleChatFocus` still has an Enter binding with no handler. Scrolling speed actions only serve the dead scrolling-ruleset base.

Resolve this properly:

1. Define a compact fresh set of retained actions and give each an explicit numeric value as the new lite persistence baseline.
2. Delete dead default bindings, localisation, `GlobalActionCategory.Editor`, and `GlobalActionCategory.EditorTestPlay`.
3. Remove obsolete actions from the enum rather than leaving comments or legacy integer constants as permanent tombstones.
4. Remove historical migration cases that mention deleted actions. Do not add a Realm cleanup migration: the locked D4 profile boundary starts with a fresh keybinding store.

Explicit numbering protects future lite profiles; it does not preserve pre-third-pass keybindings.

## Desktop and removed-platform residue

The repository still tracks deleted-project scaffolding:

- `osu.Android.props`
- `osu.iOS.props`
- `osu.TestProject.props`
- Android/iOS `.idea` projects
- benchmark, tournament, and removed-test run configurations
- stale mobile/test Solution Items
- `InternalsVisibleTo` entries for removed iOS/Android/test assemblies

Delete all of it. Also remove test-only branches/comments where no production behaviour needs them, including `DebugUtils.IsNUnitRunning` checks left solely for deleted tests.

Desktop-only mobile residue remains in source:

- `RuntimeInfo.IsMobile` defaults and branches
- `ShowMobileDisclaimer`
- an iOS case in `MouseSettings`
- Android/iOS comments and special locals in import/resource code
- Android null-handling commentary in `RulesetStore`
- `AllowRightClickFromLongTouch` in `OsuUserInputManager`

Remove these under the locked desktop-only D5 scope; mobile heads are already gone.

Retain real Windows, macOS, and Linux desktop paths. Do not delete a branch merely because it is unreachable on the Windows development machine. The stable-import hard-link stack is still deleted under D4 because no retained platform consumes it after that product is removed.

Independently review these desktop extras rather than exempting them as small:

- `NVAPI.cs` is about 632 lines for one Windows startup driver tweak. Remove it unless a measured product requirement justifies it.
- `SDL2BatteryInfo` is a fallback selected only when SDL3 is disabled. If the shipped framework/runtime is SDL3-only, delete the SDL2 implementation and branch.
- `ElevatedPrivilegesChecker` is a warning-only peripheral. Retain only if running elevated is a supported risk the product actively handles.
- Fix or remove the Windows association startup path that produces CA1416. The final Release build should have zero warnings.

## Verified whole dead clusters

The following clusters have no external construction/consumer and should be deleted, not re-audited indefinitely:

### Markdown

Delete `osu.Game/Graphics/Containers/Markdown/` in full (20 files). All references are internal to that directory. No retained screen constructs `OsuMarkdownContainer`.

### Generic scrolling ruleset UI

Delete `osu.Game/Rulesets/UI/Scrolling/` in full (11 files), plus its two GlobalActions/localisation and the one defensive player dependency-cache branch.

### Online request/report stack

Delete the request classes and `ReportPopover` listed in the API section. There is no concrete live request and no `ReportPopover` subclass.

### Dead overlay/ruleset helpers

Delete:

- `Overlays/OverlayStreamItem.cs`
- `Overlays/TabControlOverlayHeader.cs`
- `Overlays/OverlayRulesetSelector.cs`
- `Graphics/UserInterface/BreadcrumbControl.cs`

### Dead beatmap drawables

Delete:

- `Beatmaps/Drawables/DifficultySpectrumDisplay.cs`
- `Beatmaps/Drawables/UpdateableBeatmapBackgroundSprite.cs`
- `Beatmaps/Drawables/OnlineBeatmapSetCover.cs`
- `Screens/Select/UpdateLocalConfirmationDialog.cs`

### Dead generic UI controls

These types have only their declaration/constructor and no consumer:

- `Graphics/Containers/OsuRearrangeableListContainer.cs`
- `Graphics/Containers/DependencyProvidingContainer.cs`
- `Graphics/Sprites/SpriteIconWithTooltip.cs`
- `Graphics/Sprites/SpriteTextWithTooltip.cs`
- `Graphics/UserInterface/SlimEnumDropdown.cs`
- `Graphics/UserInterface/TimeSlider.cs`
- `Graphics/UserInterface/ExternalLinkButton.cs`
- `Graphics/UserInterface/GradientLineTabControl.cs`
- `Graphics/UserInterface/LineGraph.cs`
- `Graphics/UserInterface/OsuTabControlCheckbox.cs`
- `Graphics/UserInterface/SectionHeader.cs`
- `Graphics/UserInterface/ShowMoreButton.cs`
- `Graphics/UserInterface/TernaryStateRadioMenuItem.cs`
- `Graphics/UserInterfaceV2/FormButton.cs`
- `Graphics/UserInterfaceV2/FormColourPalette.cs`
- `Graphics/UserInterfaceV2/FormPasswordTextBox.cs`
- `Graphics/UserInterfaceV2/FormFileSelector.cs`
- `Graphics/UserInterfaceV2/LabelledNumberBox.cs`
- `Graphics/UserInterfaceV2/LabelledColourPalette.cs`
- `Graphics/UserInterfaceV2/LabelledEnumDropdown.cs`
- `Graphics/UserInterfaceV2/LabelledSliderBar.cs`
- `Graphics/UserInterfaceV2/LabelledSwitchButton.cs`
- `Graphics/UserInterfaceV2/LabelledTextBoxWithPopover.cs` after the stable-directory selection flow is removed under D4

### Dead helpers and packages

Delete:

- `Beatmaps/MetadataUtils.cs`: its public methods only call one another; no external consumer.
- `Utils/GeometryUtils.cs`: editor geometry with no external consumer.
- `Utils/TagLibUtils.cs`: no caller. Remove `TagLibSharp` with it.
- `Extensions/WebRequestExtensions.cs`: an empty class with stale imports.
- `Database/IHasFiles.cs`: no implementer/consumer.
- `Utils/OfficialBuildAttribute.cs`: no source/build use.
- `Rulesets.Osu/UI/AnyOrderHitPolicy.cs`: no construction; the active policies are start-time ordered and classic legacy.
- `Skinning/SkinConfigManager.cs`: no construction/consumer.
- `Localisation/ModSelectOverlayStrings.cs`: no consumer.

Also remove the direct `System.ComponentModel.Annotations` package if the post-deletion source still has zero consumer.

### Method-level pruning

Do not retain an entire helper because one method is active:

- In `TimeDisplayExtensions`, retain `ToFormattedDuration`; remove `ToEditorFormattedString` and `ToShortRelativeTime`.
- In `StringDehumanizeExtensions`, retain snake/kebab conversion while required; remove `ToCamelCase` and the otherwise-internal-only Pascal conversion.
- In `JsonSerializableExtensions`, retain methods used by score and JSON beatmap serialization; delete unused overloads/settings only after direct call verification.
- In `RealmExtensions`, retain `FindWithRefresh`, write helpers, collection-change detection, and active online-ID queries until their callers are removed; then prune methods individually.

### Serialisable skin/HUD orphans

After fixed-skin layout serialisation is removed, delete types currently kept alive only by hypothetical saved layout reflection. Verified no direct construction exists for:

- `BPMCounter`
- `LongestComboCounter`
- `MatchScoreDisplay`
- `PlayerAvatar`
- `PlayerFlag`
- `SkinnableModDisplay`
- `DefaultRankDisplay`
- `LegacyRankDisplay`
- `LegacyPerformancePointsCounter`
- `ClicksPerSecondCounter`
- `ArgonUnstableRateCounter`
- `ArgonJudgementCounterDisplay`
- `Skinning/Components/BigBlackBox`
- `Skinning/Components/TextElement`

Triangles-only default HUD components (`DefaultAccuracyCounter`, `DefaultHealthDisplay`, `DefaultScoreCounter`, `DefaultSongProgress`, and related layout helpers) should leave with `TrianglesSkin` unless another retained source directly constructs them.

`GameplayOffsetControl` also has no construction site and should be deleted.

## Online-shaped local types to reduce or delete

Some items in `Online` are active only because local code reused an online-era type. Remove the namespace coupling:

- `DownloadState`/`DownloadButton` are used only by `SaveFailedScoreButton` for local import/export. Rename and reduce the enum to the states actually used; `Downloading` and `Unknown` are not part of that flow.
- Minimal `APIUser` is the local player/score author model. Replace it with a local user type after pruning team/supporter/online fields.
- `SoloScoreInfo` is an online score-submission DTO with no construction site; delete it, its dead `ScoreInfoExtensions` overload, and comments that cite it.
- Delete `APIMod`, `ScoreInfo.ModsJson`/`APIMods`, `FrameHeader.Mods`, and their setting dictionaries, reflective conversion, equality, cloning, and serialization plumbing. Parse embedded lazer mod metadata into a tiny import-only rejection shape that is never stored and never constructs a gameplay mod.
- Delete `APIBeatmap`/`APIBeatmapSet`, `BeatmapInfo.OnlineInfo`, the dead API conversion helper, and historical migration references after the online cache and `SoloScoreInfo` are gone.

`APITeam` is only an unused property on `APIUser`; remove both the property and type. Prune supporter/team/profile/avatar fields once the remaining local UI is checked.

Keep `UpdateableAvatar`, `ClickableUsername`, and `UserCoverBackground` only as long as results genuinely display local score identity. Remove their click/profile behaviour and remote texture paths. `PlayerAvatar` and `PlayerFlag` are separate dead serialisable HUD types and should not be confused with the active results components.

## Settings/control architecture follow-up

`SettingSourceAttribute` currently mixes three removed or reducible jobs:

1. serializing arbitrary mod settings;
2. serializing custom skin/HUD layouts;
3. dynamically constructing settings UI controls.

The locked mod catalogue, saved-layout system, and skin editor are gone. Autoplay has no settings, and the internal Classic compatibility path must use fixed defaults rather than a general settings contract. Unmodded replay-analysis controls are the only direct `CreateSettingsControls()` consumer worth retaining functionally, and they do not require reflection.

Finish the removal:

- build the small retained replay-analysis settings UI explicitly
- remove mod and skin-component setting attributes with the deleted mod-settings and saved-layout systems
- delete `ModSettingChangeTracker` after removing its selected-mod, mod-icon, difficulty-cache, and serialisable-component callers
- remove generic settings enumeration/serialization from `Mod`/`IMod`
- delete `SettingSourceAttribute`, its reflection cache/control factory, and the unused legacy `SettingsItem<T>` control variants

This is exactly the kind of mixed-in cleanup that previous passes deferred. It belongs in the third pass.

## Execution sequence

Each numbered item should be its own commit or a small series of commits with the same gate. Do not combine the fresh profile/schema boundary with unrelated UI deletions.

### Phase 0: lock decisions and baseline

- Treat the confirmed D1-D7 table as binding; do not reopen the choices during execution.
- Choose and document the new lite profile/config/Realm namespace. Prove it does not discover, copy, or open a pre-third-pass database or config.
- Keep one `.osz`, one unmodded stable `.osr`, one unmodded lazer `.osr`, and representative modded `.osr` rejection fixtures outside version control.
- Record a clean Release build and launch.

### Phase 1: repository debris and indisputable orphans

- Delete stale mobile/test props, Solution Items, `.idea` projects/run configurations, and friend assemblies.
- Delete empty/declaration-only helpers and UI controls from the verified inventory.
- Delete Markdown, dead overlay helpers, dead beatmap drawables, `AnyOrderHitPolicy`, empty `WebRequestExtensions`, `GeometryUtils`, `MetadataUtils`, and `TagLibUtils`.
- Remove `TagLibSharp`, unused annotations package, and dead localisation.

Gate: Release build, launch, settings open/search, song select open.

### Phase 2: guarantee no in-process network

- Remove metadata download/cache and online backpopulation.
- Remove preview-track network stack and online texture source.
- Remove SQLite packages.
- Remove user tags/date-ranked UI and data members; do not migrate historical values.
- Prove a clean profile produces no HTTP attempt.

Gate: clean-data launch, `.osz` import, song select filtering, play to results, network log/monitor inspection.

### Phase 3: fixed Kanna skin end-to-end

- Replace multi-skin manager with fixed provider.
- Remove built-in selectable skins, import/export/edit/database paths, Argon implementation, and Triangles fallback after visual proof.
- Remove mania skin parsing.
- Remove the Realm Skin model from the fresh schema without a transition migration.
- Collapse saved-layout/serialisable component machinery and delete its orphan components.
- Correct all Argon comments and validation wording to Kanna.

Gate: clean profile launch, menu, song select, gameplay HUD, cursor, hit objects, slider, spinner, hitsounds, pause/results; missing-resource fallback; retained beatmap skin on/off.

### Phase 4: delete online API and link facades

- Replace `IAPIProvider` with local player context.
- Delete request/report/endpoint/state/social/favourites code.
- Replace the retained local user/score identity with a minimal local model and delete the obsolete online serialization DTOs listed above.
- Remove no-op in-app links and `osu://` IPC/protocol.
- Retain only hard-coded Report Issue and hardware-documentation browser actions; remove the warning/config and generic URL surface.

Gate: local score attribution, score import, replay playback, results identity, both retained browser buttons, confirmation that arbitrary/in-app URLs cannot launch, and second-instance archive import.

### Phase 5: transient notification sink

- Remove drawer/history/read state and `NotificationSection`.
- Narrow and rename the notification interface/component.
- Remove `ToggleNotifications` completely from the fresh action enum and default bindings.

Gate: long `.osz` import progress/cancel, import failure, screenshot notification, log export completion, critical error, exit confirmation during an active operation.

### Phase 6: single-ruleset runtime

- Remove selectors, conversion toggles, generic scrolling UI, and unused object interfaces.
- Hard-code osu!standard registration and simplify rule/config iteration.
- Define only the osu!standard Realm row and remove custom-ruleset loading without migrating old rows.
- Keep the three-project layout after runtime collapse; do not merge projects.

Gate: fresh database, osu! beatmap import, search/sort/group, play, results, replay, and keybinding settings.

### Phase 7: mods and replay policy

- Remove global selectable-mod state and UI catalogues.
- Add a minimal replay-metadata admission check and reject every `.osr` containing a mod flag/entry before model creation.
- Delete unsupported mod implementations and now-unused settings/reflection controls.
- Preserve only the internal Classic behaviour required for a raw-zero-mod stable replay, or inline and delete it during this phase.

Gate: Ctrl+Enter autoplay, unmodded stable and lazer replay import/playback, explicit rejection of every modded replay fixture without storing it, and unmodded score calculation/results display.

### Phase 8: beatmap manager and fresh database baseline

- Delete editor/save/export/external-edit APIs.
- Collapse beatmap updater to local processing.
- Replace AutoMapper with explicit copies.
- Delete historical background compatibility jobs and the old 53-case Realm migration switch/backward-copy scan.
- Establish the fresh lite Realm/config/keybinding namespace and minimal future lite schema-version foundation.
- Delete the complete osu!stable import and now-exclusive hard-link stack listed under D4.
- Remove encoder/exporter code made unreachable.

Gate: `.osz` import, delete/restore beatmap, offsets, permitted `.osr` import/export, clean restart persistence, and proof that a pre-third-pass profile is neither opened nor copied.

### Phase 9: settings, actions, and session state

- Consolidate to the six-section target.
- Delete Debug section and one-control wrappers.
- Remove dead OsuSettings and session statics.
- Explicitly number the fresh retained GlobalActions and delete editor/chat/scroll/tombstone actions, historical migration references, dead bindings, and localisation.
- Remove joystick/gamepad settings, bindings, and presentation while retaining keyboard, mouse, and tablet/pen paths.

Gate: settings search, all remaining controls, keyboard/mouse/tablet input rebinding and conflict resolution, restart persistence, and absence of joystick/gamepad settings.

### Phase 10: desktop/platform cut and final dependency sweep

- Retain and validate Windows/macOS/Linux desktop paths while removing mobile and test-only residue.
- Decide NVAPI, SDL2 battery fallback, elevated warning, and file associations explicitly.
- Remove every package with zero direct retained consumer.
- Remove empty directories, stale `using` directives, comments, localisation, and docs.
- Run a fresh declaration/reference scan and classify every new zero/one-consumer result.

Gate: zero-warning Release builds plus launch smoke tests on Windows, macOS, and Linux, followed by the complete end-to-end checklist below.

## Required validation after every phase

Because all tests were removed, every phase must be checked manually in proportion to its risk:

1. `dotnet build osu.Desktop -c Release` with zero errors on every phase; final validation requires zero-warning builds and launch smoke tests on Windows, macOS, and Linux.
2. Launch to main menu on a clean profile.
3. Place a pre-third-pass profile beside the new build and prove that it is neither discovered, copied, opened, nor modified.
4. Confirm there is no in-process HTTP activity and that only the two hard-coded browser actions can open an external URL.
5. Import an `.osz`; observe progress, completion, cancel, and failure feedback.
6. Search, sort, group, select, and play an osu!standard map.
7. Exercise circle, slider, spinner, cursor, HUD, pause, fail/retry, and results rendering with Kanna.
8. Confirm local score history.
9. Import/play one unmodded stable `.osr` and one unmodded lazer `.osr`.
10. Attempt every representative modded `.osr`; confirm a clear rejection and verify that no score/replay row or file was stored.
11. Restart and confirm new-lite Realm/config/keybinding persistence.
12. Open a second process with `.osz` and permitted `.osr` files and confirm archive forwarding.
13. Exercise retained storyboards, videos, beatmap skins, colours, and hitsounds.
14. Confirm that no osu!stable import/first-run flow or old-profile migration affordance remains.

## Definition of done

The third pass is complete only when:

- normal runtime has no in-process network client or remote resource store;
- Kanna/classic fallback is the only user-skin chain and no user-skin database/import/export/layout editor remains;
- `Online/` is gone or contains no type that is merely local data under an online name;
- notification feedback is transient and has no permanent drawer/history/unread model;
- only osu!standard can be registered, selected, displayed, or converted for gameplay;
- autoplay is the only user-facing mod, unmodded stable/lazer replays play, and every mod-bearing imported replay is rejected before storage;
- the application starts from the new lite profile/schema boundary and contains no pre-third-pass Realm/config/keybinding or osu!stable migration path;
- dead editor, chat, notification, mod-select, scrolling, mobile, and online GlobalActions are removed, not tombstoned;
- settings contain only active product controls;
- every retained mod implementation is required by autoplay or the documented internal raw-zero-mod stable replay path;
- Windows, macOS, and Linux each build and launch through retained, platform-appropriate paths;
- no historical compatibility migration/job remains; any retained data processor has a live new-lite producer and consumer;
- every direct package has a retained source consumer;
- stale mobile/test/Argon/online comments and localisation are gone;
- Release builds with zero warnings and the full runtime checklist passes.

The recommended direction is to stop treating this as a trimmed general osu! client and start treating it as a purpose-built local osu!standard player. Preserve the import/play/results/replay loop, then reshape supporting systems around that loop instead of retaining general upstream frameworks whose entry points have already been removed.

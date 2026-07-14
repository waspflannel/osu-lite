# osu! lite post-trim review

## ▶ Second-pass execution status (resume here)

The second trimming pass is **in progress** on branch **`osu-lite-trim2`** (branched from `osu-lite` at `13116b584e`). Work is in small, individually-building commits; every commit compiles `osu.Desktop` clean, and startup was runtime-verified after Phases A–B (reaches MainMenu, no exceptions, zero outbound network).

| Phase | Status | Notes |
|-------|--------|-------|
| A — Correct documented behavior | ✅ Done | Runtime-verified |
| B — Finish already-started deletions | ✅ Done | Runtime-verified (~5.5k+ lines) |
| C — Collapse online compatibility paths | ✅ Done | Build-verified; interactive song-select playtest still recommended |
| D — Simplify settings and notifications | 🚧 Partial | Dead settings + April Fools + seasonal + toolbar button done; see deferred items below |
| E — Confirmed peripheral cuts | 🚧 Partial | 5/8 sub-items done (collections, updater, Sentry, Discord, first-run wizard); see below |
| F — Final dead-code & dependency sweep | ⬜ Not started | Run after E |

**To resume:** read the per-phase detail in the "Suggested implementation sequence" section (bottom of this doc) — completed phases carry a ✅ and a summary of exactly what changed; the in-progress and not-started phases list the remaining work.

**Remaining work, concretely:**

1. **Finish Phase D:**
   - Notification drawer/history split: keep the transient toast/progress tray (`NotificationOverlayToastTray`) but remove the permanent drawer history (`NotificationSection`), unread/read state, and the `ToggleNotifications` hotkey/binding. Delicate — every import/maintenance/error path posts to `INotificationOverlay`, so verify progress/cancel/error toasts still appear afterward.
   - Six-section settings consolidation (cosmetic; low priority).
2. **Phase E** (each a separate commit): collections (database-aware — prefer schema tombstone over deleting the Realm model without a migration), updater/release streams, Sentry, Discord Rich Presence, first-run→minimal migration prompt, legacy IPC/WebSocket/`.olz`/`osump://`, remaining seasonal UI (`SeasonalUIConfig`/Christmas) + latency certifier, touch (product decision).
3. **Phase F:** re-run cross-file reference analysis; sweep newly-orphaned localisation, config enums, assets; resolve the persisted-enum tombstones left behind (mod-select `GlobalAction`s: `ToggleModSelection`/`DeselectAllMods`/`IncreaseModSpeed`/`DecreaseModSpeed`; `ToggleNotifications`; and the `ModPreset` Realm model) via a deliberate Realm migration or explicit tombstone.

**Known deferred tombstones (intentionally left inert, handlers removed):** mod-select `GlobalAction` enum values, `ModPreset` Realm model + its `RealmAccess` delete-pending hook, and stable *skin* migration (folded into the Phase E first-run rework since it shares the `StableContent` flags).

**Verification gap:** an interactive song-select → play → results playtest was not performed — the dev build isn't a Start-menu app, so computer-use can't drive its window. Recommend a manual playtest of the Phase A–C changes (especially the song-select metadata wedges) before Phase E.

## Purpose and scope

This document reviews the `osu-lite` branch after the major reduction recorded in `OSU_LITE_PLAN.md`. It compares the stated product contract with the current code, identifies additional features that could be removed, and marks dead or unnecessary code that is either self-contained or intertwined with retained gameplay.

No implementation changes are proposed as already-approved work. Each item is a candidate for a later, separately validated phase.

Audit basis:

- Branch: `osu-lite` at `13116b584e`.
- Working tree was clean before this document was added.
- The solution contains only `osu.Game`, `osu.Game.Rulesets.Osu`, and `osu.Desktop`.
- Current source size is 1,736 C# files and about 199,568 physical C# lines.
- The branch differs from `origin/master` by roughly 3,847 files and 441,812 deleted lines, confirming that the first pass removed the overwhelming majority of the original non-lite surface.
- Static analysis included entrypoint tracing, constructor/call-site searches, enum/config usage searches, direct dependency searches, and a cross-file public-type reference sweep.
- A desktop build reached and successfully built `osu.Game` and `osu.Game.Rulesets.Osu`. The final `osu.Desktop` copy step could not complete because Visual Studio had the destination DLLs locked. This is an environmental validation limitation, not a source compilation error found by the audit.

## Executive conclusion

The first trim was structurally successful: online play, online overlays, extra rulesets, editors, mobile heads, tournament code, tests, and most network infrastructure are gone. The remaining codebase still carries four kinds of weight:

1. Features that are still active even though the plan says they were removed, most importantly custom skin importing/selection.
2. Offline compatibility stubs that keep old online execution paths alive instead of removing those paths.
3. UI and settings for unreachable features, especially mod selection, mod presets, seasonal backgrounds, supporter state, and online actions.
4. Optional product features that may not belong in the intended minimal client, including collections, the notification drawer, first-run migration, updater/telemetry/Discord integrations, legacy IPC, seasonal UI, and the latency certifier.

The best next pass is not a broad deletion of notifications or collections. It is a contract-correction pass:

1. Actually enforce the fixed Argon skin.
2. Finish deleting the mod-select path.
3. Remove online lookup/state paths rather than resolving them to no-op components.
4. Remove dead settings, actions, DTOs, helpers, packages, and active UI affordances that now lead to no-ops.
5. Execute the confirmed product decisions for collections, notification history, updater/telemetry, first-run migration, and peripheral integrations.

## Confirmed second-pass removal scope

The following decisions have now been made. These items are no longer optional candidates; they define the intended scope of the second trimming pass.

### Target product after the second pass

osu! lite will be a local desktop osu!standard client that:

- Imports and plays `.osz` beatmaps.
- Imports and plays `.osr` replays.
- Stores and displays local scores.
- Uses Argon as the fixed user-interface/gameplay skin.
- Can optionally migrate beatmaps and scores from an existing stable installation.
- Does not provide online play, accounts, social features, telemetry, Rich Presence, in-app updates, or other external-service integrations.

### 1. Remove collections completely

Collections will not be part of osu! lite. Remove the feature end to end rather than leaving a hidden Realm model or partial song-select support.

Removal scope:

- All files under `osu.Game/Collections/`.
- `ManageCollectionsDialog` construction and dependency resolution in `OsuGame`.
- Song-select collection grouping, filtering, dropdowns, Realm subscriptions, context menus, and `ISongSelect.ManageCollections()`.
- Results-screen `CollectionButton` and `CollectionPopover`.
- Collection maintenance settings and localisation.
- Stable collection import and the first-run collection checkbox.
- Beatmap import/hash-update hooks that maintain collection MD5 membership.
- Collection AutoMapper registration and active Realm queries.
- The `BeatmapCollection` Realm model after adding a deliberate existing-database migration or schema tombstone strategy.

Do not leave an empty Collections heading, disabled dropdown, unused grouping enum value, or compatibility service after the UI is removed.

### 2. Remove the in-app updater

Updates will be handled manually or by the operating-system/package distribution channel.

Removal scope:

- `VelopackUpdateManager` and core updater implementations.
- Velopack setup, restart-to-apply behavior, and the Velopack package reference.
- `NoActionUpdateManager`, `MobileUpdateNotifier`, `GitHubRelease`, and `GitHubAsset` if no remaining updater consumer exists.
- General settings update subsection, check-for-update action, release-stream selection, and update notifications.
- `ReleaseStream` configuration and localisation when no longer consumed.
- Update-specific notification subclasses and assets.

The application should not perform update or GitHub release checks after this pass.

### 3. Remove Sentry telemetry

Remote crash reporting and telemetry will not be part of the offline client.

Removal scope:

- `SentryLogger` construction and lifecycle in `OsuGame`.
- Sentry scope/user updates and exception-routing branches.
- `SentryOnlyDiagnosticsException` if it has no non-Sentry purpose.
- The direct Sentry package reference.

Local file logging and visible local error reporting remain.

### 4. Remove Discord Rich Presence

Discord integration will be removed completely.

Removal scope:

- `osu.Desktop/DiscordRichPresence.cs` and its unconditional construction in `OsuGameDesktop`.
- The `DiscordRichPresence` package reference.
- `DiscordRichPresenceMode`, its config setting, and localisation.
- Discord URI registration, join callbacks, room-secret compatibility code, and presence state.
- `UserOnlineStatus` and `UserStatus` if no non-Discord consumer remains after online identity cleanup.
- `UserActivity` API/status presentation where it exists only to broadcast presence; retain a simpler local screen/window-title concept if required.

### 5. Replace the first-run wizard with a minimal local migration prompt

Stable migration remains useful, but only for beatmaps and scores. The current multi-step wizard and behaviour/settings duplication will be removed.

Keep:

- Optional discovery of an existing stable installation.
- Local import of stable beatmaps.
- Local import of stable scores/replays.
- Clear progress and failure feedback through the retained lightweight toast/progress system.

Remove:

- Stable skin and collection import.
- The skin and collection checkboxes/counting branches.
- The behaviour/defaults page and its duplicated settings sections.
- In-game upgrade/wiki links that lead to removed handlers.
- Resume-wizard notifications and general quick actions for reopening the full wizard.
- Wizard infrastructure that has no remaining consumer after the replacement prompt is implemented.

The resulting flow should be a single optional migration prompt, not a general onboarding/settings framework.

### 6. Retain only single-instance archive IPC

Keep the minimum IPC needed to forward `.osz` and `.osr` files to an already-running osu! lite process.

Remove:

- `LegacyTcpIpcProvider` and its startup/error-handling path.
- The environment-gated WebSocket server, provider, messages, and port configuration.
- `osump://` registration and handling.
- `.olz` import/export and file association.
- `.osk` file association as part of fixed-Argon enforcement.
- `osu://` registration and scheme IPC if no supported local deep-link action remains after link-handler cleanup.

Keep:

- The single-instance host/pipe behavior required by the desktop application.
- Archive forwarding for `.osz` and `.osr` only.

### 7. Remove seasonal and novelty UI

Remove all non-core seasonal presentation:

- Christmas intro, logo, lighting, side flashes, seasonal colours, and `SeasonalUIConfig` branches.
- Seasonal online background loader, response/session state, setting, and localisation.
- The April Fools `AfToggleSection` and its date-based insertion into settings.

There should be one deterministic main-menu presentation path after this change.

### 8. Remove the latency certifier

The built-in latency certifier is a specialist diagnostic tool and will not ship in osu! lite.

Removal scope:

- The Maintenance settings entry.
- `LatencyCertifierScreen`, visual modes, latency area, cursor/sample components, and the circle/scrolling sample gameplay screens under `Screens/Utility`.
- Localisation and icons that become unreferenced.

### 9. Simplify notifications to transient local feedback

The notification system itself will not be deleted because imports, migration, maintenance, and local errors need feedback. Its permanent notification-centre behavior will be removed.

Keep:

- Transient informational and error toasts.
- Progress, completion, and cancellation for local imports/migration/maintenance.
- Ongoing-operation tracking only where shutdown or cancellation logic requires it.
- Local logging of all messages.

Remove:

- The permanent notification drawer and history sections.
- Toolbar notification button and unread badge.
- `ToggleNotifications`, its default binding, and localisation.
- Read/unread state and permanent-store forwarding when no remaining consumer requires them.
- Online-only outage, score-submission, and avatar notification types.
- Update-specific notifications removed with the updater.

### 10. Enforce Argon as the sole user skin

The second pass will finish the previously incomplete fixed-skin migration.

Remove:

- `.osk` registration and importing.
- Stable skin migration.
- Configured skin restoration and persistence.
- Runtime skin selection through `PresentSkin()`.
- User-skin export, external editing, rename, deletion, query, and enumeration paths.
- Maintenance skin controls and user-skin localisation.
- Other selectable built-in skins where they have no load-bearing fallback or compatibility role.

Keep:

- The minimum skin-provider/rendering infrastructure required by gameplay and HUD components.
- Argon as the deterministic user skin.
- Beatmap-provided skin resources only if map fidelity remains an explicit requirement; they must not change the selected user skin.

### Explicitly retained for now

The second pass will not remove these areas unless new evidence shows they are orphaned:

- Core osu!standard gameplay, scoring, difficulty calculation, and replay playback.
- Local Realm score and beatmap persistence.
- `.osz` and `.osr` import.
- Beatmap videos, storyboards, beatmap skin resources, and replay mods.
- Desktop mouse, keyboard, tablet, and touch support.
- Local logs.

After completing the confirmed removals, run a final reference, localisation, configuration, asset, and package sweep. Any abstraction retained solely for one of the removed features should be deleted rather than converted into another no-op compatibility layer.

## What the first pass removed well

The as-built record in `OSU_LITE_PLAN.md` is broadly accurate for these areas:

- Only osu!standard remains as a shipped ruleset.
- Android, iOS, tournament, benchmarks, templates, and all test projects are absent from the solution.
- Beatmap and skin editors, editor entrypoints, and shared editor frameworks are removed.
- Online play, multiplayer, playlists, matchmaking, spectator systems, chat, rankings, profiles, news, wiki, changelog, login/account overlays, and online request classes are largely gone.
- Song select, local beatmap import, local score storage, score/replay import, gameplay, and results remain connected.
- `SoloResultsScreen` now uses local Realm scores rather than an online leaderboard.
- The main menu and toolbar no longer expose the major online destinations.

This was the correct first cut. The next gains come from finishing partial migrations and removing retained upstream generality that no longer serves this product.

## Product contract mismatches and behavioral risks

### P0 — The skin is not actually fixed to Argon

The plan says skin selection, import, export, and deletion were removed and that the game is locked to Argon. The active code does not enforce that contract.

Evidence:

- `osu.Game/OsuGameBase.cs:340-342` still registers `SkinManager` as an archive import handler.
- `osu.Game/Skinning/SkinImporter.cs:39` still accepts `.osk`.
- `osu.Desktop/Windows/WindowsAssociationManager.cs:47-52` still registers `.osk` with Windows.
- `osu.Game/OsuGame.cs:408-414` restores a configured skin on startup and persists runtime skin changes.
- `osu.Game/OsuGame.cs:561-571` still selects an arbitrary imported/database skin.
- `osu.Game/OsuGame.cs:877-880` routes completed skin imports to `PresentSkin()`, which immediately activates the imported skin.
- `osu.Game/Database/LegacyImportManager.cs:129-170` still counts and imports stable skins.
- `osu.Game/Overlays/FirstRunSetup/ScreenImportFromStable.cs:67-70` still offers skin migration.
- `osu.Game/Overlays/Settings/Sections/Maintenance/SkinSettings.cs` still exposes “Delete ALL skins”.
- `SkinManager` still constructs the importer and exporter and retains query, import, external-edit, export, delete, rename, and multi-skin enumeration paths.

Recommended simpler model:

- Keep the skin rendering abstraction required by gameplay.
- Make Argon the sole user-interface skin source.
- Keep beatmap skin support only if map fidelity is a requirement; it is distinct from user-selectable skins.
- Remove `.osk` registration/import, stable skin migration, skin export/edit/delete/rename, configured skin restoration, `PresentSkin()`, and user-skin Realm/file-store management.
- Reduce `SkinManager` from a general model manager/importer/exporter to the smallest active skin provider the runtime needs.

This should be the first correction because the current behavior directly contradicts the documented product.

### P0 — The offline API reports itself as online and logged in

`DummyAPIAccess` is not a minimal offline boundary. It is primarily a test fake retained from upstream behavior.

Evidence:

- `osu.Game/Online/API/DummyAPIAccess.cs:55` initializes state to `APIState.Online`.
- `IsLoggedIn` returns true for that state, and the default local user is a synthetic user with ID 1001 rather than a guest.
- The class still contains simulated login, 2FA, login failure, indefinite connecting, account creation, friends, blocks, favourites, arbitrary request handlers, session verification, and endpoint data.
- `osu.Game/Online/API/LocalUserState.cs` separately implements the documented always-guest behavior but is never constructed.

Consequences:

- `BackgroundDataStoreProcessor.processOnlineBeatmapSetsWithNoUpdate()` enters its online-only branch because `api.IsLoggedIn` is true (`osu.Game/Database/BackgroundDataStoreProcessor.cs:230-290`).
- Discord Rich Presence initializes and publishes because it also sees a logged-in user.
- Score-import and UI branches continue to reason about an authenticated user.
- The reported “benign `DummyAPIAccess cannot process this request`” log is evidence of an old execution path still being exercised, not a desirable steady state.

Recommended simpler model:

- Choose one offline user representation: guest/local player.
- Report `IsLoggedIn == false` and `APIState.Offline` consistently.
- Delete simulated auth/account/test methods and `RegistrationRequest` coupling.
- Narrow `IAPIProvider` to the members still consumed by scoring/models, or replace it with smaller local-user and endpoint/link abstractions.
- Remove call sites whose only behavior is now checking or waiting on online state.

### P1 — Online lookup plumbing still runs through active song-select UI

The current solution resolves online features to null rather than removing their execution paths.

Intertwined sections:

- `osu.Game/Screens/Select/SongSelect.cs:163-176` creates `RealmPopulatingOnlineLookupSource` even though it always returns null.
- `SongSelect.cs:1042-1098` maintains lookup status, cancellation, tasks, continuations, and logging for that no-op result.
- `osu.Game/Screens/Select/BeatmapMetadataWedge.cs:50-65, 277-310, 368-403` consumes the no-op lookup to drive ratings, fail/retry data, genre, language, and user-tag UI.
- `osu.Game/Screens/Select/BeatmapTitleWedge.cs:287-320` maintains online play-count state from the same result.
- `osu.Game/Beatmaps/APIBeatmapMetadataSource.cs` is an always-unavailable implementation used by the retained updater pipeline.
- `osu.Game/Beatmaps/DifficultyRecommender.cs` is constructed and resolved only to return null; all three callers immediately fall back to the first suitable beatmap.
- `osu.Game/Database/UserLookupCache.cs` and `BeatmapLookupCache.cs` are cached components whose methods return null arrays. `BeatmapLookupCache` has no consumer; `UserLookupCache` only supports the optional `PlayerTeamFlag` skin component.

Recommended simpler model:

- Remove the no-op lookup component, task/cancellation state, lookup result type, and online-only wedge branches.
- Render local metadata directly. Hide unavailable ratings, fail/retry, online play-count, genre, and language rows instead of completing a fake lookup with null.
- Replace difficulty recommendation calls with the existing deterministic fallback directly.
- Remove unused lookup caches; decide separately whether `PlayerTeamFlag` is supported in a single-player fixed-skin product.
- Keep local cached metadata only if the bundled `online.db` materially improves imported beatmap metadata. If retained, name it as bundled/local metadata rather than “online” and remove the API fallback abstraction.

### P1 — Active UI still offers actions that lead to no-ops

`OsuGame.HandleLink()` still supports the original online `LinkAction` matrix, while `ShowChannel()`, `ShowBeatmapSet()`, `ShowUser()`, `ShowBeatmap()`, `SearchBeatmapSet()`, `ShowWiki()`, and changelog methods are empty (`osu.Game/OsuGame.cs:442-555`).

Visible or active callers remain:

- “searching online” in `osu.Game/Screens/Select/NoResultsPlaceholder.cs:192-198` invokes a no-op.
- Mapper/creator links in `BeatmapMetadataWedge.cs:350` and `BeatmapTitleWedge.DifficultyDisplay.cs:249` invoke the removed user profile.
- The first-run stable-import screen links to an in-game wiki action that is now empty.
- General settings exposes an upgrade/help wiki quick action through `QuickActionSettings`.
- Windows still registers `osu://` and `osump://`; `osump://` is specifically a removed multiplayer protocol.

Remove the affordances and then narrow the link parser/handler to the actions still supported, principally safe external URLs and any genuinely local deep links.

## High-confidence dead code and unfinished deletion clusters

### 1. Mod-select cluster — highest-value self-contained deletion

Current size:

- `osu.Game/Overlays/Mods/`: 35 files, about 5,037 lines.
- `osu.Game/Screens/Select/FooterButtonMods.cs`: another large unreachable UI component.

The actual overlay and footer button are not constructed by the retained song-select flow. Most references are internal to the cluster.

Delete candidates include:

- `ModSelectOverlay`, `UserModSelectOverlay`, columns, panels, search, footer content, hotkey strategies, customisation panels, incompatibility display, ranking display, deselect button, preset panels/popovers/tooltips, and related dialogs.
- `FooterButtonMods.cs`.
- `ModSpeedHotkeyHandler` and the speed-change song-select action path if manual speed-mod adjustment is no longer supported.
- `ModPreset` Realm model and maintenance UI if no other preset consumer remains.
- Mod-select config values, localisation, session state, and global keybindings.

Shared exceptions that must be separated before deleting the directory:

- `BeatmapAttributeTooltip` is used by `BeatmapTitleWedge.StatisticDifficulty`; retain or relocate it.
- `ShearedOverlayContainer` is used by `WizardOverlay`, `ScreenFooter`, and `OsuGame`; retain or relocate it.

`ModState` appears confined to mod selection and does not need to be preserved for `ModDisplay`/`ModIcon`. Recheck at implementation time, but the earlier plan’s blanket warning to keep it appears over-conservative.

Keep the gameplay mod model and per-ruleset mods. They remain necessary for replay fidelity, scoring, difficulty, autoplay, and imported scores even when users cannot select mods interactively.

### 2. Orphaned notification subclasses

Safe, direct candidates:

- `osu.Game/Overlays/Notifications/OutageNotification.cs`
- `ScoreSubmissionFailureNotification.cs`
- `UserAvatarNotification.cs`

Their types have no consumers outside their own files. They belong to removed outage, online score submission, and social-user flows.

Do not infer from these three files that the whole notification system is dead. `ProgressNotification`, `ProgressCompletionNotification`, `SimpleNotification`, and `SimpleErrorNotification` are used by imports, maintenance, update handling, screenshots, platform warnings, gameplay warnings, and error reporting.

### 3. Orphaned online DTOs and auth types

The 17 response types already listed in `OSU_LITE_PLAN.md` remain high-confidence candidates:

- `APIChangelogIndex`
- `APIKudosuHistory`
- `APIMenuContent`
- `APINewsSidebar`
- `APINotificationsBundle`
- `APIScoresCollection`
- `APISpotlight`
- `APITagCollection`
- `APIUserContainer`
- `APIUserMostPlayedBeatmap`
- `APIUserScoreAggregate`
- `APIWikiPage`
- `ChatAckResponse`
- `CommentBundle`
- `GetMyFavouriteBeatmapSetsResponse`
- `LivenessProbeResponse`
- `PutBeatmapSetResponse`

Also remove `OAuthToken.cs`. If `DummyAPIAccess` is simplified as recommended, remove `RegistrationRequest.cs` and its account-error models at the same time.

Additional response-model trimming should follow the narrowed local score/beatmap model rather than deleting DTOs by directory. `APIUser`, `APIBeatmap`, `APIBeatmapSet`, `APIScore`, `APIMod`, and several nested user types are still embedded in retained scoring and UI models.

### 4. Orphaned overlay and screen helpers

High-confidence candidates with no external constructor/type consumer:

- `Overlays/OverlayView.cs`
- `OverlayHeaderBackground.cs`
- `OverlayPanelDisplayStyleControl.cs`
- `OverlaySidebar.cs`
- `OverlayStreamControl.cs`
- `BreadcrumbControlOverlayHeader.cs`
- `SortDirection.cs`
- legacy `Overlays/Settings/DangerousSettingsButton.cs` (the retained settings use `DangerousSettingsButtonV2`)
- `Overlays/Settings/Sections/SizeSlider.cs`
- `Screens/Menu/ConfirmDiscardChangesDialog.cs`
- `Screens/ScreenWhiteBox.cs`
- `Screens/Play/PlayerSettings/DiscussionSettings.cs`
- `Beatmaps/Drawables/UpdateableOnlineBeatmapSetCover.cs`
- `Database/IModelDownloader.cs` and `ModelDownloader.cs` once the already-removed download implementations are confirmed absent
- `Users/CountryStatistics.cs`
- `Users/Medal.cs`
- `Users/Drawables/StatusIcon.cs`
- `Updater/MobileUpdateNotifier.cs` after removal of the mobile heads

### 5. Secondary orphaned UI sweep

A cross-file type-reference sweep found the following UI types referenced only inside their defining file. They are strong candidates because UI controls are normally explicitly constructed, but they should be removed in small build-verified groups because generic bases and reflection can evade a text search.

- `Graphics/UserInterface/ExternalLinkButton.cs`
- `GradientLineTabControl.cs`
- `HistoryTextBox.cs`
- `LineGraph.cs`
- `OsuTabControlCheckbox.cs`
- `SectionHeader.cs`
- `ShowMoreButton.cs`
- `SlimEnumDropdown.cs`
- `TernaryStateRadioMenuItem.cs`
- `TimeSlider.cs`
- `Graphics/UserInterfaceV2/FormButton.cs`
- `FormColourPalette.cs`
- `FormFileSelector.cs`
- `FormPasswordTextBox.cs`
- `LabelledColourPalette.cs`
- `LabelledEnumDropdown.cs`
- `LabelledNumberBox.cs`
- `LabelledSliderBar.cs`
- `LabelledSwitchButton.cs`
- `ReportPopover.cs`

Do not use the same “type name appears once” rule for extension classes or serialisable skin components. Extension methods are called without the declaring class name, and `ISerialisableDrawable` components may be materialized from saved layouts.

### 6. Dead direct package references

No retained C# source usages were found for these direct `osu.Game.csproj` references:

- `DiffPlex`
- `HtmlAgilityPack`
- `Microsoft.AspNetCore.SignalR.Client`
- `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`
- `Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson`
- `Microsoft.Extensions.Configuration.Abstractions`
- `NUnit`

These should be removed one at a time or as a package-only phase, with restore/build verification. Do not remove `MessagePack`, `Microsoft.Data.Sqlite`, `SQLitePCLRaw`, `SharpCompress`, `TagLibSharp`, `Humanizer`, `AutoMapper`, or `Sentry` solely from this list; each still has direct consumers. Sentry is an optional feature decision discussed below, not currently dead code.

## Notifications: recommended trim, not full removal

### What is active

The notification model is used for:

- `.osz`, `.osr`, and currently `.osk` import progress and completion.
- Database/background migration progress.
- Beatmap cleanup and offset-reset results.
- Update download/check state.
- Screenshot and platform warnings.
- Muted-audio, battery, disabled-HUD, and general error messages.

The current notification UI has two responsibilities:

1. Temporary toast/progress presentation.
2. A permanent 320-pixel drawer with sections, unread counts, history, toolbar button, and global hotkey.

The first responsibility is useful in a local import-driven client. The second is optional product chrome.

### Recommended concise model

Keep a lightweight `INotificationSink`/toast host that can:

- Display transient messages.
- Display progress and cancellation for long imports or maintenance.
- Expose ongoing operations where shutdown/close logic genuinely depends on them.
- Log all messages.

Remove:

- The permanent notification drawer and `NotificationSection` history.
- The notifications toolbar button.
- `ToggleNotifications`, its default binding, and its localisation.
- Unread counts and read/unread state if nothing else consumes them.
- Online-only notification subclasses.
- Window-flash/user-avatar/outage grouping behavior not needed by local tasks.

This preserves important feedback while deleting much of `NotificationOverlay`, `NotificationOverlayToastTray`, toolbar integration, and permanent-storage complexity. Fully removing notifications would force every import/maintenance path to invent another progress/error channel and is therefore a poor first simplification.

## Collections: an active feature selected for removal

Collections occupy only seven files/845 lines in `osu.Game/Collections`, but the feature is woven through retained screens and persistence.

Active seams:

- Realm model mapping and historical migrations.
- Stable collection import.
- Beatmap MD5 update handling in `BeatmapInfo` and `BeatmapImporter`.
- Song-select grouping, filtering, dropdowns, filter criteria, and Realm subscriptions.
- Add/remove context-menu actions in song select.
- Results-screen collection button/popover.
- Global `ManageCollectionsDialog` creation and dependency resolution.
- Maintenance “delete all collections”.
- Collection localisation.

### Confirmed collection-removal seams

Treat this as a product-feature phase, not a dead-code sweep. The phase should remove:

1. Results and song-select entrypoints.
2. Collection grouping/filter modes and `FilterCriteria.Collection`.
3. `CollectionDropdown`, menu items, context actions, and `ISongSelect.ManageCollections()`.
4. The dialog and all `Collections/` drawables.
5. Stable collection import and first-run checkbox.
6. Beatmap hash migration/update hooks.
7. Maintenance controls and localisation.
8. The Realm model only after deciding how existing databases are migrated.

For existing Realm files, deleting the C# model without a schema plan is the risky part. A safe implementation can leave historical migration references/tombstones while removing all runtime feature code, then remove the model in a deliberate schema migration.

Decision: collections will be removed completely. There is little value in a half-state, so the implementation must remove every runtime seam above while handling existing Realm data deliberately.

## Settings simplification

The settings system currently contains 92 C# files/about 8,305 lines and presents eight normal top-level sections: General, Input, User Interface, Gameplay, Audio, Graphics, Maintenance, and Debug. It also inserts a large April Fools section on April 1.

### Controls that are already invalid or meaningless

Remove in the next cleanup regardless of the larger settings design:

- Song-select `ModSelectHotkeyStyle` and `ModSelectTextSearchStartsActive`.
- Maintenance `ModPresetSettings`.
- Maintenance `SkinSettings` once the fixed-skin contract is enforced.
- Seasonal background mode. `SeasonalBackgroundLoader.fetchSeasonalBackgrounds()` is an empty offline stub, so this setting cannot produce a seasonal background.
- Online notification config entries: username mentions, private messages, friend presence.
- Online/content config entries: explicit online content, automatic missing-beatmap downloads, featured-artist filter, dashboard sort/display, profile cover state, supporter cache, and other removed-overlay settings.
- `UserOnlineStatus` if Discord presence is removed; it has no local gameplay meaning.
- First-run/upgrade wiki quick actions that invoke empty in-game handlers.
- April Fools `AfToggleSection`. It is a large hidden seasonal feature in the settings surface and unrelated to the lite product.

Potentially remove after confirming desired replay behavior:

- Gameplay “Increase first object visibility on visual impairment mods”. It only affects visual-impairment mods that users can no longer select, but it can still affect imported replays using those mods.
- `ShowConvertedBeatmaps`. With one ruleset, the control and associated filtering/conversion branches may be unnecessary; validate imported non-osu difficulties before removal.
- Random selection algorithm. Keeping a single random-permutation behavior would remove a setting and dual execution paths without removing the random button.

### Suggested user-facing structure

Use six concise sections:

1. General — language, installation/storage, updates only if retained, and truly global behavior.
2. Controls — mouse, tablet, touch if supported, and keybindings.
3. Gameplay — HUD, background/video behavior, beatmap skins/hitsounds/colors, offsets, and input behavior.
4. Audio — device, volume, and offset.
5. Graphics — renderer, frame limiter, display, layout/UI scale, video, and screenshots.
6. Data — import plus beatmap and score cleanup; collections only if retained.

Remove the release-build Debug section. If memory/GC diagnostics and batch stable import are still needed by developers, gate the whole section behind `DebugUtils.IsDebugBuild` instead of showing `MemorySettings` to every user.

Avoid retaining one-file subsections with a single niche control solely to preserve the upstream taxonomy. Move the control into its owning section and delete the empty subsection layer.

## Confirmed peripheral cuts and remaining boundaries

These areas are not necessarily dead code, but the second-pass decisions above now select them for removal or reduction.

### Updater and release streams

The desktop still creates `VelopackUpdateManager`, package-managed builds create `NoActionUpdateManager`, and General settings can check updates/change release streams. This brings update notifications, GitHub/release endpoints, Velopack setup, restart-to-update behavior, five core updater files, desktop updater code, and the Velopack package.

Decision: remove the in-app updater and release-stream settings. Distribution will be external or manual.

### Sentry telemetry

Decision: remove Sentry initialization, scope updates, diagnostic exception handling, and the package. Local file logging remains.

### Discord Rich Presence

`osu.Desktop/OsuGameDesktop.cs:136` always loads `DiscordRichPresence`. It carries a package, user status/activity models, configuration, link registration, callbacks for removed multiplayer join secrets, and API/login coupling. It also means the client is not isolated from external services.

Decision: remove Discord Rich Presence. This allows deletion or simplification of:

- `DiscordRichPresence.cs` and the desktop package.
- `DiscordRichPresenceMode` and its setting/localisation.
- Much of `UserActivity`/`UserStatus` once window-title needs are handled directly.
- Synthetic login state currently kept partly to make presence work.

### First-run wizard and stable migration

The first-run system is five files/about 997 lines plus `WizardOverlay`, settings duplication, stable locators, legacy importers, notifications, and configuration. It still offers skins and collections, both under consideration for removal.

Decision: replace the wizard with a one-page optional migration prompt for beatmaps and scores only. Remove skin/collection migration, the dead in-game wiki link, behaviour/default selection, and duplicated settings sections.

### Legacy IPC, WebSocket, and protocol/file associations

Decision: retain only the single-instance archive IPC path for opening `.osz`/`.osr` in an existing process. Remove:

- `LegacyTcpIpcProvider`, which exists for legacy-client compatibility.
- The environment-gated `OsuWebSocketProvider` and its message/server types.
- `osump://`, which targets removed multiplayer behavior.
- `.olz` import/export and association if editor/playlist packages are no longer supported.
- `.osk` association as part of fixed-skin enforcement.
- `osu://` registration if all supported deep-link actions have been removed.

### Seasonal and novelty UI

`SeasonalUIConfig.ENABLED` is hard-coded false, yet six seasonal files/about 668 lines remain compiled and referenced behind the flag. Decision: delete the Christmas intro/logo/lighting branches.

`AfToggleSection` is a large April Fools implementation loaded into settings on April 1. It should be removed from a concise client.

### Latency certifier

The Maintenance section exposes a latency certifier backed by nine `Screens/Utility` files/about 1,370 lines. This is a specialist diagnostic tool, not routine maintenance. Decision: remove it rather than retaining a debug-gated production code path.

### Supporter and online identity presentation

The main menu still creates `SupporterDisplay`; intro, menu effects, background selection, score panels, avatars, and tooltips retain supporter/user-profile behavior. With a fixed local user and no account system, simplify these paths:

- Remove supporter prompts and supporter-gated background notes.
- Make mapper/player names non-clickable local metadata.
- Replace interactive online avatars/user cards with plain local score identity where possible.
- Remove the Favourites song-select grouping, which binds to an always-empty online favourite list, unless it is repurposed as a local feature.

### Touch/mobile compatibility

Removing mobile application heads does not automatically make touch code dead. Desktop touch input, touch-screen gameplay, and touch settings remain active and may be valuable on Windows devices.

Safe immediate mobile-only cuts include `MobileUpdateNotifier` and mobile disclaimer state. A broader touch removal is a product choice and would affect gameplay input mapping, automatic Touch Device mod handling, cursor behavior, settings, and score metadata.

### Storyboards, videos, beatmap skins, and replay mods

These are large potential reductions but directly affect beatmap/replay fidelity. They should not be mixed into routine dead-code cleanup.

- Removing storyboards and beatmap videos can save substantial UI/rendering/media code but changes how many maps are presented.
- Removing beatmap skins can simplify the fixed-skin engine but changes gameplay appearance and hitsounds.
- Removing replay mod support can shrink mod/rendering code but breaks faithful playback of imported/local modded scores.

Recommendation: keep all three until the smaller contract-correction phases are complete and measured.

## Persisted-enum and database cautions

Several apparently dead values are intertwined with persisted data.

### `GlobalAction`

Dead handlers/bindings include online actions such as `ToggleChat`, `ToggleSocial`, `ToggleBeatmapListing`, and `ToggleProfile`, plus mod-selection actions such as `ToggleModSelection` and `DeselectAllMods`. `IncreaseModSpeed`/`DecreaseModSpeed` remain handled only by residual mod-speed UI.

`GlobalAction` numeric values are stored in Realm. Deleting or reordering enum values can remap users’ bindings. Use one of these approaches:

- Leave explicit obsolete tombstone values with no default binding/localisation.
- Add a Realm migration that rewrites/removes affected rows before compacting the enum.

The earlier plan’s warning about editor actions remains valid: Realm keybinding migrations reference editor action names, and the settings keybinding UI enumerates them. Do not casually delete them.

### `OsuSetting`

Config keys are name-based, so unused values are easier to stop registering than `GlobalAction` values are to renumber. Remove each setting together with:

- Its `SetDefault()` call.
- Settings controls.
- localisation.
- supporting enum/type if no longer used.
- any migration or compatibility read.

### Realm models

`BeatmapCollection`, `ModPreset`, `SkinInfo`, API-backed user fields, and old ruleset/keybinding records may be present in existing databases. Runtime feature removal and schema/model deletion should be separate decisions. Prefer a deliberate migration or retained schema tombstone over an accidental open failure.

## Suggested implementation sequence

Each phase should be a separate commit with a desktop build and the import → song select → play → results smoke path.

### Phase A — Correct documented behavior — ✅ COMPLETED

- ✅ Enforce Argon and remove user-skin import routing/`PresentSkin`/configured-skin restoration + persistence + the "Delete ALL skins" maintenance panel. (Stable skin *migration* removal is folded into the Phase E first-run rework since it shares the `StableContent` flags; the unused `SkinManager` export/delete/rename members are now dead-but-inert and swept in Phase F.)
- ✅ Remove `.osk` association (Windows file association + `SkinImporter` handled extensions).
- ✅ Set the offline identity/API state consistently to offline: `DummyAPIAccess` now starts `APIState.Offline` so `IsLoggedIn` is always false; `ScoreImporter` LastPlayed no longer gates on login. (Local user kept as "Local user" — a valid local-player representation — to avoid `SYSTEM_USER_ID` sentinel edge cases in local score attribution.)
- ✅ Remove visible no-op online affordances: main-menu `SupporterDisplay`, song-select "searching online" link, settings wiki quick action. (Mapper/creator name de-linking is folded into the Phase C wedge rework since those files are rewritten there.)

Verified at runtime: builds green, reaches MainMenu with no exceptions, no outbound network requests, and no "DummyAPIAccess cannot process this request" log messages.

### Phase B — Finish already-started deletions — ✅ COMPLETED

- ✅ Deleted the mod-select UI cluster (30 files in `Overlays/Mods/` + `FooterButtonMods`, ~5.5k lines), the `Mods/Input/` hotkey handlers, `ModSpeedHotkeyHandler` and its song-select actions, the `ModSelectHotkeyStyle`/`ModSelectTextSearchStartsActive` settings + config keys + localisation, and the `ModPreset` maintenance panel. Relocated the two shared helpers first: `BeatmapAttributeTooltip` → `Screens/Select`, `ShearedOverlayContainer` → `Overlays`.
- ✅ Deleted orphaned notification subclasses (`OutageNotification`, `ScoreSubmissionFailureNotification`, `UserAvatarNotification`), overlay/screen helpers (`OverlayView`, `OverlayHeaderBackground`, `OverlayPanelDisplayStyleControl`, `OverlaySidebar`, `OverlayStreamControl`, `BreadcrumbControlOverlayHeader`, `SortDirection`, legacy `DangerousSettingsButton`, `SizeSlider`, `ConfirmDiscardChangesDialog`, `ScreenWhiteBox`, `DiscussionSettings`, `UpdateableOnlineBeatmapSetCover`), `MobileUpdateNotifier`, dead user DTOs (`CountryStatistics`, `Medal`, `StatusIcon`), the 17 orphaned online response DTOs + `OAuthToken`, and the `IModelDownloader`/`ModelDownloader` abstractions.
- ✅ Removed dead package references: `DiffPlex`, `HtmlAgilityPack`, three `Microsoft.AspNetCore.SignalR.*`, `Microsoft.Extensions.Configuration.Abstractions`, `NUnit` (its only "uses" are `DebugUtils.IsNUnitRunning`, an osu.Framework helper needing no package).

Deferred to database-aware / Phase F: the `ModPreset` Realm model and the mod-select `GlobalAction` enum values (`ToggleModSelection`, `DeselectAllMods`, `IncreaseModSpeed`, `DecreaseModSpeed`) are left as inert tombstones — handlers gone — to avoid a risky Realm keybinding/schema migration mid-phase.

Verified at runtime: builds green, reaches MainMenu with no exceptions and no outbound network requests.

### Phase C — Collapse online compatibility paths — ✅ COMPLETED

- ✅ Removed the song-select online lookup entirely: deleted `RealmPopulatingOnlineLookupSource`, `fetchOnlineInfo`, the cached `BeatmapSetLookupResult`/`BeatmapSetLookupStatus` result state, and the online-only wedge rows. `BeatmapMetadataWedge` now renders only local metadata (creator as plain text, source, mapper tags, submitted/ranked, and local user tags) — the genre/language/ratings/fail-retry/success-rate rows and their four sub-display classes are gone. `BeatmapTitleWedge` drops the online play-count (length becomes the leading statistic) and its now-redundant post-lookup status refresh (local status is already applied in `updateDisplay`).
- ✅ Deleted `DifficultyRecommender` and inlined the deterministic first-suitable-beatmap fallback at all three call sites.
- ✅ Removed the unused `BeatmapLookupCache` and `UserLookupCache`, plus the offline-meaningless `PlayerTeamFlag` HUD component (its sole consumer).
- ✅ Narrowed `IAPIProvider` to the offline-consumed members and rewrote `DummyAPIAccess` as a minimal offline provider (removed simulated login/2FA/logout/account-creation/friends/blocks/outage state); deleted `RegistrationRequest`.
- ✅ Simplified the metadata pipeline to the bundled local cache only: deleted `APIBeatmapMetadataSource`, dropped the `IAPIProvider` dependency from `BeatmapUpdater`/`BeatmapUpdaterMetadataLookup`.

Verified: builds green; three startup smoke tests reach the game with zero exceptions and zero outbound network requests. Note: an interactive song-select/play-through check via computer-use was not possible (the dev build isn't a Start-menu app, so it can't be granted). The removed lookup-result consumers and the cached provider were removed together (grep-verified no other consumers), so the DI graph is self-consistent; a manual playtest of song select → play → results is still recommended before release.

### Phase D — Simplify settings and notifications — 🚧 IN PROGRESS

- ✅ Removed dead online config keys and defaults: `NotifyOnUsernameMentioned`/`NotifyOnPrivateMessage`/`NotifyOnFriendPresenceChange`, `ShowOnlineExplicitContent`, `AutomaticallyDownloadMissingBeatmaps`, `BeatmapListingFeaturedArtistFilter`, `DashboardSortMode`, `ProfileCoverExpanded`, `WasSupporter` (plus the unused `LocalUserState` that read it).
- ✅ Removed the April Fools `AfToggleSection` and its date-based settings insertion.
- ✅ Removed the dead seasonal-background feature end to end (setting, `SeasonalBackgroundLoader`, `SeasonalBackgroundMode`, `APISeasonalBackgrounds`, session static, settings dropdown, localisation). Its loader was an empty offline stub, so `BackgroundScreenDefault` already fell back to normal backgrounds — behavior-preserving.
- ✅ Removed the toolbar notification button and its unread badge.
- ⏳ **Deferred (larger/riskier, not yet done):**
  - The internal `NotificationOverlay` drawer/history split — keep the transient toast/progress tray while removing the permanent history sections, unread/read state, and the `ToggleNotifications` hotkey. This is delicate because every import/maintenance/error path depends on the notification model for feedback, so it needs careful refactoring plus a runtime check that progress/cancel/error toasts still work.
  - The six-section settings consolidation (a cosmetic reorg; lower value, deferred to keep focus on removals).

### Phase E — Confirmed peripheral cuts

Execute as separate reviewable commits:

- ✅ Collections — removed end to end: `osu.Game/Collections/` (7 files), `LegacyCollectionImporter`, song-select grouping/filter/dropdown/context-menu integration (`FilterCriteria`, `FilterControl`, `CollectionDropdown`, `BeatmapCarousel`/`BeatmapCarouselFilterGrouping`/`BeatmapCarouselFilterMatching`, `PanelBeatmapSet`, `SongSelect`/`SoloSongSelect`, `FooterButtonOptions.Popover`), results-screen `CollectionButton`/`CollectionPopover`, Maintenance `CollectionsSettings`, first-run checkbox, debug batch-import button, beatmap-hash MD5 transfer hooks (`BeatmapImporter`/`BeatmapInfo`/`BeatmapManager`), AutoMapper registration, and localisation. Existing Realm databases are handled via a deliberate schema migration (`schema_version` 51→52, historical `case 21` stable-import block turned into a documented no-op) rather than an silent model deletion. Touch support removal (item 8) was confirmed via user decision to proceed with removal. Build-verified (`osu.Desktop` clean).
- ✅ Updater/release streams — deleted `osu.Game/Updater/UpdateManager.cs` (base class + all its notification subclasses), `NoActionUpdateManager`, `GitHubRelease`, `GitHubAsset`, `VelopackUpdateManager`, `ReleaseStream` enum/config setting, and the General-settings `UpdateSettings` subsection. Removed the Velopack package reference and all Velopack setup/hook code from `osu.Desktop/Program.cs` and `OsuGameDesktop.cs`. `RestartAppWhenExited()` (used by migration/renderer-restart flows, not just updates) now relaunches via `Process.Start(Environment.ProcessPath)` instead of `Velopack.UpdateExe`. Windows file/URI association installation, previously wired through Velopack's first-run/update/uninstall hooks, is now performed idempotently on every Windows startup via `WindowsAssociationManager.UpdateAssociations()`. `IsPackageManaged` was kept (still used by the macOS app-location checker, unrelated to the updater). Removed update-specific notification/localisation strings (`GameVersionAfterUpdate`/`UpdateCompleteNotification`, `UpdateReadyToInstall`, `NotOfficialBuild`, `DownloadingUpdate`, `UpdateAvailable*`) and the version-bump "what's new" notification in `OsuGame.cs` (version bookkeeping itself is kept). Build-verified (`osu.Desktop` clean).
- ✅ Sentry — deleted `SentryLogger` and `SentryOnlyDiagnosticsException`, removed construction/`AttachUser`/`Dispose` lifecycle calls and the `ScreenChanged` scope-tagging block in `OsuGame.cs`, dropped the `Sentry` package reference, and removed the now-misleading "automatically reported to the dev team" wording from the general log-forwarding notification (it now just shows the truncated message). Local file logging and in-app error notifications are unaffected. Build-verified (`osu.Desktop` clean).
- ✅ Discord Rich Presence — deleted `osu.Desktop/DiscordRichPresence.cs` and its construction site in `OsuGameDesktop.LoadComplete`, the `DiscordRichPresenceMode` enum/setting, `UserStatus`/`OsuSetting.UserOnlineStatus` (had no non-Discord consumer), the `DiscordRichPresence` package reference, and the now-fully-orphaned `OnlineSettingsStrings.cs` (every member in it was either Discord-only or already-dead from earlier phases with no other consumer). `UserActivity` itself is untouched — it has many non-Discord consumers (window title, screen activity tracking). Build-verified (`osu.Desktop` clean).
- ✅ First-run/stable migration — deleted `ScreenWelcome`, `ScreenUIScale`, `ScreenBehaviour` (language picker, UI-scale preview, and the duplicated-settings behaviour/defaults page) and `LegacySkinImporter`. `FirstRunSetupOverlay` now registers only `ScreenImportFromStable` as its single step, so the existing step-machinery in `WizardOverlay` naturally collapses to a one-screen "Get started -> Finish" flow rather than a multi-page wizard. Removed the mid-wizard "resume" notification (meaningless for a single screen), the Skins checkbox and stable skin migration path (`StableContent.Skins`, `LegacyImportManager`/`BatchImportSettings` skin branches), and the dead in-screen wiki link for "hard links" (routed to the no-op `ShowWiki`). Also removed the "Run setup wizard" quick-action settings button per the doc's explicit removal of general re-entry points to the wizard — the prompt still appears on first run and via `MigrationSelectScreen`'s folder-change flow. Build-verified (`osu.Desktop` clean).
- Legacy IPC/WebSocket/protocols and `.olz`.
- Seasonal UI and latency certifier.
- Touch support.

### Phase F — Final dead-code and dependency sweep

- Re-run cross-file type/reference analysis after the larger owners are gone.
- Remove newly orphaned localisation, config enums, user DTO fields, graphics controls, packages, assets, and comments.
- Search for “online”, “login”, “multiplayer”, “supporter”, “skin import”, “mod select”, “collection”, and “mobile” in active source and justify every remaining occurrence.

## Validation gates for future phases

Because all test projects were removed, runtime validation must be explicit.

Minimum gate after every phase:

1. `dotnet build osu.Desktop` with no errors.
2. Launch to the main menu.
3. Confirm no unexpected network requests or synthetic API failures in logs.
4. Import an `.osz` and confirm progress/error feedback remains understandable.
5. Search, select, and play an osu!standard map.
6. Reach results and confirm local score history.
7. Import and play an `.osr`, including a modded replay if replay fidelity is retained.
8. Restart and confirm Realm/config migrations open existing user data.

Feature-specific gates:

- Fixed skin: `.osk` is rejected/ignored, Argon persists after restart, and beatmap skin policy behaves as intended.
- Notifications: long imports can still report progress/cancel and fatal errors remain visible.
- Collections removal: databases containing old collections still open.
- Settings: search and keybinding subpanels still resolve after consolidation.
- API collapse: no `DummyAPIAccess cannot process this request` messages remain.
- Integration removal: single-instance `.osz`/`.osr` opening still works if retained.

## Final recommendation

Execute the confirmed second-pass scope in small, independently buildable commits. Start with the still-active custom-skin path, the test-like `DummyAPIAccess` reporting online/logged-in state, and the no-op online lookup/affordance plumbing embedded in song select. Then remove mod selection and the high-confidence orphan clusters before undertaking database-aware collection removal and the peripheral integration cuts.

Notifications will become a minimal transient toast/progress sink. Collections will be removed as a dedicated database-aware phase. Updates, Sentry, Discord, seasonal/novelty UI, latency certification, legacy TCP/WebSocket integrations, unsupported protocols/archive types, and the full first-run wizard are all confirmed removals. Stable beatmap/score migration and single-instance `.osz`/`.osr` forwarding are the intentionally retained compatibility paths.

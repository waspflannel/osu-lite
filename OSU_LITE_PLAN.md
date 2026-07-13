# osu! lite — Execution Checklist

> Companion to `OSU_LITE_MAP.md`. Work top to bottom. **Do not skip the checkpoint at the end of each phase** — the whole point of this ordering is that you always have a running build to fall back to.

# ══════════════════════════════════════════════════════════════
# AS-BUILT RECORD (this is the source of truth; the phase checklist below is historical)
# ══════════════════════════════════════════════════════════════

**osu! lite is complete** on branch `osu-lite` (pushed to `origin/waspflannel/osu-lite`).

**What it is:** a fully offline, single-mode osu! that imports and plays local beatmaps with one fixed skin. Nothing else.

**Verified:** `osu.Desktop` builds with 0 errors; the game launches to the main menu, plays, and makes **zero network requests to any ppy server**. Confirmed by log inspection (no `Request to *.ppy.sh`, no exceptions beyond the benign `DummyAPIAccess cannot process this request`).

**Solution now contains only 3 projects:** `osu.Game`, `osu.Game.Rulesets.Osu`, `osu.Desktop`.

---

## What was REMOVED

**Rulesets & platforms**
- Taiko, Catch, Mania rulesets (projects + all references)
- Android/iOS application heads and every mobile test project
- Tournament client, benchmark project, ruleset-scaffolding templates
- **All test projects** and the in-`osu.Game` visual-test framework (`osu.Game/Tests/`)

**Editing**
- The beatmap editor (`Screens/Edit`), the skin editor (`Overlays/SkinEditor`), the shared editor blueprint/composer framework (`Rulesets/Edit`), and the Osu ruleset's editor components (`osu.Game.Rulesets.Osu/Edit`)
- Editor hooks on the `Ruleset` base class and all editor-only config settings
- Online beatmap submission (was editor-coupled)

**Skinning**
- Skin selection UI, skin import/export/delete, and the random/next/previous-skin hotkeys. The skin **engine** stays, locked to the default skin.

**Mods**
- The mod-**select** overlay and mods footer button are removed from song select; there is no way to choose mods. Gameplay-side mod handling (autoplay via Ctrl+Enter, scoring, difficulty) is intact.

**The entire online subsystem** (this was the big cut — ~925 files deleted)
- All online **overlays**: beatmap listing, beatmap set, changelog, chat, comments, dashboard, news, profile, rankings, wiki, login, account creation, medals
- All online-play **screens**: multiplayer, playlists, daily challenge, matchmaking, spectator (`Screens/OnlinePlay`, `Screens/Spectate`, spectator/submitting players, gameplay-leaderboard/spectator HUD)
- The online **backend**: `Online/{Chat, Multiplayer, Spectator, Rooms, Metadata, Matchmaking, RankedPlay, Solo, Leaderboards, Notifications}` plus hub/SignalR/persistent/polling infrastructure and download trackers
- All online **API request classes** (`Online/API/Requests/*.cs`) — only the `Responses/` model types were kept
- The real `APIAccess`/`OAuth` (replaced by `DummyAPIAccess`)
- Online entry points: multiplayer/playlists/daily/browse main-menu buttons, the online toolbar buttons (news/chat/rankings/listing/changelog/wiki/social/user), the online menu banner, the first-run bundled-beatmap-download screen
- 17 orphaned localisation string files (chat, multiplayer, online-play, editor, skin-editor, account creation, etc.)

**Settings**: the Online and Ruleset settings sections (offline, single-ruleset).

## What is STILL in the codebase (kept, working)

- **osu! standard gameplay** — full hit/scoring/timing/replay engine
- **Local beatmap import** (`.osz`) + Realm database + file store
- **Song select → play → results** loop; results shows the just-played score **and local scores from the Realm DB** (`SoloResultsScreen` was reworked to query realm instead of an online leaderboard)
- **Score import** (`.osr`) and local replays
- **The skin engine**, locked to the default (Argon) skin
- **Trimmed settings**: General, Input, User Interface, Gameplay, Audio, Graphics, Maintenance, Debug
- **Minimal toolbar**: music, clock, notifications (import toasts)
- **A minimal offline API surface** kept so DI still resolves: `DummyAPIAccess` (never contacts a server), `IAPIProvider`, `APIRequest`, `APIState`, `GuestUser`, `APIMod`, and the `Responses/` model types (used by scoring/beatmap models). `LocalUserState` is always a guest; user/beatmap lookup caches, `DifficultyRecommender`, and the online metadata sources are offline no-op stubs.
- **Relocated shared types** pulled out of deleted online namespaces (do not recreate): `FrameHeader`→`Scoring`, `DrawableRank`/`UpdateableRank`→`Scoring/Drawables`, `MessageFormatter`/`LinkDetails`/`Link`/`ExternalLinkOpener`/`LinkWarnMode`/`DrawableLinkCompiler`→`Online`, `FireAndForget`→`Extensions/TaskExtensions`, `Beatmaps/BeatDivisor`, `Graphics/Containers/DependencyProvidingContainer`, plus the editor-phase relocations `LabelledTextBoxWithPopover`/`RepeatingButtonBehaviour`→`Graphics`.

## Known residual (inert dead code — compiles, does nothing, safe to leave)

- **`Overlays/Mods/`** — the mod-select overlay classes (`ModSelectOverlay`, `UserModSelectOverlay`, `ModColumn`, `ModPanel`, presets, etc.) still exist but are **unreachable** (no entry point creates them after the Phase-7 UI removal). Mod *display* classes are still used on results/song-select. This cluster could be deleted in a future pass; it was left because it's self-contained and harmless.
- **Editor `GlobalAction` enum values** (F1–F5 editor hotkeys etc.) and `EditorStrings` — **cannot be safely removed** because a Realm keybinding **migration** and the settings keybinding UI reference them by name. They are inert (no handler, no default binding after cleanup attempts). Left intentionally to avoid touching a data migration.
- A handful of unused `OsuSetting` enum entries for removed online settings, and some stale online `GlobalAction` values (ToggleChat/ToggleSocial/etc. still have default keybindings but no handlers). Cosmetic.

## Commit history (this branch, newest first)

`8b12c2e` remove orphaned localisation strings · `ab9e6eb` trim online refs from game/skins/desktop · `d9dd1ed` delete online overlays/screens/backend · `91c503c` stub API/components for offline · `f99ac46` relocate shared types · `ee17f1f` plan status · `4052901` remove dead login prompt · `efddfc0` remove orphaned toolbar buttons · `507c891` trim online/ruleset settings · `417f79a` remove mod-select UI · `b3785e8` fully offline dummy API · `eb43c3b` remove online menu/toolbar entry points · `3ce4f65` stop instantiating online overlays · `66cb6a1` lock skin selection · `cd4e199` remove beatmap + skin editors · `2c06be9` remove Taiko/Catch/Mania · `9b9321c` remove test projects · `b53e074` remove mobile/benchmarks/templates · `6e5c950` remove tournament client · `59f7398` add map + plan

---

## Original deviations from plan (for context)
- **Skin-editor removal merged into Phase 3** — it shares the editor's blueprint/composer framework.
- **All test projects removed up front (Phase 2)** — they reference everything later phases gut.
- **Online layer: fully deleted** (relocate-then-delete + offline stubs), after an initial "neuter and defer" pass. The interconnected web (shared enums like `SearchBeatmapSetsRequest` filters, and shared drawables like `DrawableRank`, `UpdateableFlag`, `FrameHeader`) was untangled by relocating the shared pieces before deleting.

## Guiding principles

1. **One phase per commit (or per branch).** Never mix a "safe" cut with a "deep" cut in the same commit. If a phase breaks something, you can revert exactly that phase.
2. **Neuter, don't delete, for deeply-wired systems.** For the online layer and mods, replacing behavior with a no-op is far safer than tearing out plumbing that dozens of components resolve via DI. (See map §6.2, §6.3.)
3. **Cheapest, most-reversible cuts first.** Build confidence and shrink the surface before touching the hard stuff.
4. **Verify by running, not just building.** A green build proves it compiles; it does not prove you can still import a map and play it.

## The checkpoint routine (run at the end of EVERY phase)

```
1. Build:        dotnet build osu.Desktop
2. Run:          dotnet run --project osu.Desktop
3. Import:       drag a test .osz onto the window (keep one handy)
4. Play:         song select -> pick map -> play to the results screen
5. If green:     git add -A && git commit -m "osu-lite phase N: <what>"
6. If red:       fix, or `git checkout .` to revert the phase and rethink
```

Keep a known-good `.osz` file in the repo root (gitignored) for step 3 so the test is identical every time.

---

## Phase 0 — Baseline (no cuts yet)

Prove the stock game builds and runs **before** changing anything, so any later breakage is provably yours.

- [ ] Create a working branch: `git checkout -b osu-lite`
- [ ] Run the full checkpoint routine on the **unmodified** repo
- [ ] Confirm you can import a `.osz` and play a map to results
- [ ] Commit nothing (baseline is just the fork HEAD)

**Gate:** stock game runs and plays. If this fails, stop — it's an environment problem, not a scope problem.

---

## Phase 1 — Drop non-shipping projects (lowest risk, no `osu.Game` edits)

Pure `.sln` / `.csproj` surgery. Zero DI coupling (map §6.1, §4.6).

- [ ] Remove ruleset project refs is **Phase 2** — skip here
- [ ] From the solution, remove: `osu.Android`, `osu.iOS`, `osu.Game.Tournament`, `osu.Game.Tournament.Tests`, `osu.Game.Benchmarks`, and the mobile/tournament test projects
- [ ] Optionally remove `Templates/`
- [ ] Decide on test projects: keeping `osu.Game.Tests` is useful early; you can drop them at the end
- [ ] **Checkpoint routine**

**Gate:** builds and runs identically. These projects aren't referenced by `osu.Desktop`, so nothing in gameplay changes.

---

## Phase 2 — Remove Taiko / Catch / Mania rulesets (map §4.1)

- [ ] In `osu.Desktop/osu.Desktop.csproj`, remove the three `<ProjectReference>` lines for `Rulesets.Taiko`, `Rulesets.Catch`, `Rulesets.Mania` (leave `Rulesets.Osu`)
- [ ] Remove the three ruleset projects from the `.sln`
- [ ] Build. `RealmRulesetStore` (map §2.2) discovers rulesets by scanning **loaded assemblies** — with the refs gone, they simply won't load or appear in `AvailableRulesets`
- [ ] Note: stale `RulesetInfo` rows may linger in an existing Realm DB from earlier runs; harmless (they show as unavailable). A fresh DB won't have them
- [ ] **Checkpoint routine** — confirm song select defaults to osu! standard and there's no ruleset switching in play

**Gate:** game runs, only osu! standard is selectable, a map plays to results. Do **not** yet touch the ruleset selector UI in song select — that's cosmetic and comes later.

---

## Phase 3 — Cut the beatmap editor (map §4.3)

Mostly self-contained under `Screens/Edit/`. Remove the *entry points* first, then the screens.

- [ ] Remove the "Edit" trigger from `Screens/Menu/ButtonSystem.cs` / `MainMenu.cs`
- [ ] Remove the "Edit" context-menu action in `Screens/Select/SongSelect.cs` / `SoloSongSelect.cs` (`Edit(beatmap)` -> `EditorLoader`)
- [ ] Remove the `Editor` check in `OsuGame.cs` `HandleTimestamp()`
- [ ] Delete `Screens/Edit/` once no references remain (let the compiler find stragglers)
- [ ] **Checkpoint routine**

**Gate:** no Edit button anywhere; game plays normally.

---

## Phase 4 — Cut skin editor + lock to one skin (map §4.4)

Keep the skin **engine** (`SkinManager`); remove the editor and the ability to switch skins.

- [ ] Remove `skinEditor` field + creation in `OsuGame.cs` (around the overlay-creation block)
- [ ] Remove `ToggleSkinEditor` and `RandomSkin` / `NextSkin` / `PreviousSkin` global actions in `OsuGame.cs`
- [ ] Remove the "Skin Editor" button in `ButtonSystem.cs`
- [ ] Delete `Overlays/SkinEditor/`
- [ ] Lock the active skin: force `SkinManager.CurrentSkinInfo` to the chosen default (Argon) and stop reading skin choice from config
- [ ] (Skin *selector* in Settings is removed in Phase 6 with the rest of the sections)
- [ ] **Checkpoint routine**

**Gate:** one skin, always; no way to open a skin editor; gameplay renders correctly.

---

## Phase 5 — Trim overlays (map §6.6)

`OsuGame.cs` `LoadComplete()` creates every overlay unconditionally. Remove the online/social ones.

- [ ] **Keep:** SettingsOverlay, NotificationOverlay (import toasts), DialogOverlay (confirms), OnScreenDisplay, VolumeOverlay, Toolbar (trimmed), NowPlayingOverlay, ManageCollectionsDialog, FirstRunSetupOverlay (optional)
- [ ] **Remove:** BeatmapListingOverlay, DashboardOverlay, NewsOverlay, UserProfileOverlay, BeatmapSetOverlay, WikiOverlay, ChangelogOverlay, RankingsOverlay, ChatOverlay, LoginOverlay, AccountCreationOverlay, MedalOverlay, MessageNotifier
- [ ] Remove the corresponding toolbar buttons that open them
- [ ] Let the compiler flag every `[Resolved]` reference to a removed overlay; stub or delete each
- [ ] **Checkpoint routine**

**Gate:** main menu + toolbar show no online/social entry points; everything that remains opens.

> Note: some removed overlays are resolved by online code you haven't cut yet (Phase 8). Expect a few "unresolved dependency" errors here that trace back to online components — remove those references now or temporarily comment the overlay-consumer out and finish in Phase 8. If it gets tangled, do Phase 8 first, then return to finish Phase 5.

---

## Phase 6 — Trim settings sections (map §4.7)

The settings overlay is a container; remove sections, keep the frame.

- [ ] Remove `OnlineSection`
- [ ] Remove `SkinSection` (skin is locked)
- [ ] Remove/simplify `RulesetSection` (only osu! standard exists)
- [ ] Trim mod-related items from `GameplaySection` (finish after Phase 7)
- [ ] Remove online-deletion options from `MaintenanceSection`
- [ ] **Keep** the per-platform input subsections created by `OsuGameBase.CreateSettingsSubsectionFor(InputHandler)` — mouse/tablet/etc. are still useful
- [ ] **Checkpoint routine**

**Gate:** settings opens, shows only the trimmed sections, no dead controls.

---

## Phase 7 — Neuter the mod system (map §6.2) — UI only, KEEP base classes

**Do not delete `Rulesets/Mods/`.** The gameplay/scoring/difficulty path very likely references `Mod` types even with zero mods applied. Remove the **UI and selection**, keep the infrastructure.

- [ ] **Verify first:** search the gameplay path (`DrawableRuleset`, scoring, difficulty calc) for `Mod` usage to confirm base classes must stay. Assume they do until proven otherwise.
- [ ] Remove `ModSelectOverlay` creation + registration in `SongSelect.cs`
- [ ] Remove `FooterButtonMods` from the song-select footer
- [ ] Remove mod-select global action / hotkey
- [ ] Leave the cached `SelectedMods` bindable in place but never populate it (stays empty -> score multiplier is always 1, which is correct)
- [ ] Simplify `onRulesetChanged()` in `OsuGameBase.cs` mod iteration if it errors (only osu! standard now)
- [ ] Delete per-ruleset `Mods/` dirs only for rulesets already removed (they went with Phase 2)
- [ ] Finish trimming mod controls from `GameplaySection` (from Phase 6)
- [ ] **Checkpoint routine** — confirm you can start a map with no mod bar and it scores/results correctly

**Gate:** no mod UI anywhere; gameplay, scoring, and results all still work with an empty mod set.

---

## Phase 8 — Go fully offline (map §6.3) — the deep cut, done last

Swap real online clients for no-op/dummy implementations instead of deleting the plumbing.

- [ ] `OsuGameBase.cs:319` — replace `new APIAccess(...)` with `DummyAPIAccess` (already exists in `Online/API/`)
- [ ] `OsuGameBase.cs:328` — change `performOnlineLookups: true` -> `false` on `BeatmapManager` (stops HTTP during import — critical for offline)
- [ ] Null/stub `SpectatorClient`, `MultiplayerClient`, `MetadataClient` (map §2.3 lines :339-341) and remove them from the scene graph
- [ ] Remove `BeatmapDownloader` / `ScoreDownloader` (`BeatmapModelDownloader` / `ScoreModelDownloader`)
- [ ] Remove `OnlineMetadataClient` + `BeatmapOnlineChangeIngest` (:343)
- [ ] Score submission: make `SoloPlayer` skip token creation + submission (it extends `SubmittingPlayer`) — either short-circuit the submission calls or introduce a local-only player
- [ ] Results: strip the `LeaderboardManager` fetch from `SoloResultsScreen` so it shows only the just-played score + local scores from Realm (map §6.5)
- [ ] Remove the `Screens/OnlinePlay/` screens (Multiplayer/Playlists/DailyChallenge) and their main-menu buttons if any survived Phase 5
- [ ] Finish any Phase 5 overlay references that traced back to online components
- [ ] **Checkpoint routine** — with **no network / airplane mode on**: launch, import a `.osz`, play to results. Nothing should hang waiting on a server.

**Gate:** the game fully functions offline. Import + play + local results work with networking disabled. This is the definition of "osu! lite" done.

---

## Phase 9 — Cleanup & polish

- [ ] Delete now-orphaned `Online/` subtrees that nothing references (chat, rooms, matchmaking, leaderboards) — let the compiler confirm they're dead
- [ ] Remove test projects if you're keeping the tree lean
- [ ] Simplify the ruleset selector UI in song select (cosmetic — only osu! standard exists)
- [ ] Rename product strings / window title to "osu! lite" where appropriate
- [ ] Update `OSU_LITE_MAP.md` line numbers if you'll keep relying on it (they've drifted)
- [ ] Full regression pass of the checkpoint routine, offline
- [ ] Tag the result: `git tag osu-lite-v0.1`

---

## Risk-ordered summary

| Phase | What | Coupling | Reversible? |
|-------|------|----------|-------------|
| 0 | Baseline | — | — |
| 1 | Drop non-shipping projects | none | trivially |
| 2 | Remove 3 rulesets | low-med | yes (csproj) |
| 3 | Cut editor | medium | yes |
| 4 | Cut skin editor, lock skin | low-med | yes |
| 5 | Trim overlays | medium | yes |
| 6 | Trim settings | low | yes |
| 7 | Neuter mods (UI only) | HIGH | keep base classes = safer |
| 8 | Go offline (dummy API) | HIGHEST | hardest to unwind |
| 9 | Cleanup | low | — |

**If a phase spirals:** revert it (`git checkout .`), drop back to the last green commit, and reconsider — don't push forward on a broken build into the next phase.

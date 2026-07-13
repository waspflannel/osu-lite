# osu! lite — Execution Checklist

> Companion to `OSU_LITE_MAP.md`. Work top to bottom. **Do not skip the checkpoint at the end of each phase** — the whole point of this ordering is that you always have a running build to fall back to.

## Execution status (as built)

Phases 0–8 are **complete**. The game builds, launches to the main menu, and runs **fully offline** (verified: zero outbound requests to ppy servers). Result: single-ruleset (osu! standard), no editor, no skin editor, one fixed skin, no mod-select UI, trimmed menu/toolbar/settings, dummy offline API.

Notable deviations from the original plan, and why:
- **Skin-editor removal was merged into Phase 3** (the beatmap editor, skin editor, and Osu ruleset editor components share one blueprint/composer framework and could not be separated). Three genuinely reusable helpers were relocated out of the editor namespace rather than lost: `LabelledTextBoxWithPopover`, `RepeatingButtonBehaviour`, and a new `Beatmaps/BeatDivisor` helper.
- **All test projects were removed up front (Phase 2)** rather than at the end, because they reference rulesets/online/mods that later phases gut — keeping them compiling through every cut would have been a large ongoing tax.
- **Online layer: entry points removed + API neutered, classes retained.** Online overlays, online-play screens (multiplayer/playlists/daily/spectate), and chat backend are no longer reachable or instantiated, and `DummyAPIAccess` guarantees no network activity. The *classes* remain as dead code: the online subsystem (~30% of the codebase) is a tightly interconnected web where shared enums/components (e.g. `SearchBeatmapSetsRequest` filters, `KudosuTable`, `UpdateableFlag`) live inside online-overlay namespaces, so wholesale deletion cascades into core infrastructure. Deleting it safely is a dedicated follow-up effort, deliberately not attempted here to protect the working build.
- **Phase 9** did the safe, isolated cleanups (orphaned toolbar buttons, dead login prompt). The deeper dead-online-code deletion is left as future work.

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

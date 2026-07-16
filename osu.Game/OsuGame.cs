// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using JetBrains.Annotations;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Pen;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Music;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.OSD;
using osu.Game.Overlays.Toolbar;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens;
using osu.Game.Screens.Footer;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select;
using osu.Game.Skinning;
using osu.Game.Rulesets;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game
{
    /// <summary>
    /// The full osu! experience. Builds on top of <see cref="OsuGameBase"/> to add menus and binding logic
    /// for initial components that are generally retrieved via DI.
    /// </summary>
    [Cached(typeof(OsuGame))]
    public partial class OsuGame : OsuGameBase, IKeyBindingHandler<GlobalAction>, ILocalUserPlayInfo, IPerformFromScreenRunner, IOverlayManager
    {
#if DEBUG
        // Different port allows running release and debug builds alongside each other.
        public const string IPC_PIPE_NAME = "osu-lite-debug";
#else
        public const string IPC_PIPE_NAME = "osu-lite";
#endif

        /// <summary>
        /// The amount of global offset to apply when a left/right anchored overlay is displayed (ie. settings or notifications).
        /// </summary>
        protected const float SIDE_OVERLAY_OFFSET_RATIO = 0.05f;

        /// <summary>
        /// A common shear factor applied to most components of the game.
        /// </summary>
        public static readonly Vector2 SHEAR = new Vector2(0.2f, 0);

        /// <summary>
        /// For elements placed close to the screen edge, this is the margin to leave to the edge.
        /// </summary>
        public const float SCREEN_EDGE_MARGIN = 12f;

        private const double general_log_debounce = 60000;
        private const string tablet_log_prefix = @"[Tablet] ";

        public Toolbar Toolbar { get; private set; }

        [NotNull]
        protected readonly NotificationSink Notifications = new NotificationSink();

        private Container overlayContent;

        private Container rightFloatingOverlayContent;

        private Container leftFloatingOverlayContent;

        private Container topMostOverlayContent;

        private Container footerBasedOverlayContent;

        protected ScalingContainer ScreenContainer { get; private set; }

        protected Container ScreenOffsetContainer { get; private set; }

        private Container overlayOffsetContainer;

        private OnScreenDisplay onScreenDisplay;

        private DialogOverlay dialogOverlay;

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Cached]
        private readonly ScreenshotManager screenshotManager = new ScreenshotManager();

        private float toolbarOffset => (Toolbar?.Position.Y ?? 0) + (Toolbar?.DrawHeight ?? 0);

        private IdleTracker idleTracker;

        /// <summary>
        /// Whether the user is currently in an idle state.
        /// </summary>
        public IBindable<bool> IsIdle => idleTracker.IsIdle;

        /// <summary>
        /// Whether overlays should be able to be opened game-wide. Value is sourced from the current active screen.
        /// </summary>
        public readonly IBindable<OverlayActivation> OverlayActivationMode = new Bindable<OverlayActivation>();

        IBindable<LocalUserPlayingState> ILocalUserPlayInfo.PlayingState => UserPlayingState;

        protected readonly Bindable<LocalUserPlayingState> UserPlayingState = new Bindable<LocalUserPlayingState>();

        public OsuScreenStack ScreenStack { get; private set; }

        protected BackButton BackButton => screenStackFooter.BackButton;
        protected ScreenFooter ScreenFooter => screenStackFooter.Footer;

        protected SettingsOverlay Settings;

        private FPSCounter fpsCounter;

        private VolumeOverlay volume;

        private OsuLogo osuLogo;

        private MainMenu menuScreen;

        [CanBeNull]
        private DevBuildBanner devBuildBanner;

        private Bindable<bool> applySafeAreaConsiderations;

        private Bindable<float> uiScale;

        private RealmDetachedBeatmapStore detachedBeatmapStore;

        private ScreenStackFooter screenStackFooter;

        private readonly string[] args;

        private readonly List<OsuFocusedOverlayContainer> focusedOverlays = new List<OsuFocusedOverlayContainer>();
        private readonly List<OverlayContainer> externalOverlays = new List<OverlayContainer>();

        private readonly List<OverlayContainer> visibleBlockingOverlays = new List<OverlayContainer>();

        /// <summary>
        /// Whether the game should be limited to only display officially licensed content.
        /// </summary>
        public virtual bool HideUnlicensedContent => false;

        private bool tabletLogNotifyOnWarning = true;
        private bool tabletLogNotifyOnError = true;
        private int generalLogRecentCount;

        protected OsuGame(Func<Ruleset> createRuleset, string[] args = null)
            : base(createRuleset)
        {
            this.args = args;

            Logger.NewEntry += forwardGeneralLogToNotifications;
            Logger.NewEntry += forwardTabletLogToNotifications;

            Schedule(() =>
            {
                ITabletHandler tablet = Host.AvailableInputHandlers.OfType<ITabletHandler>().SingleOrDefault();
                tablet?.Tablet.BindValueChanged(_ =>
                {
                    tabletLogNotifyOnWarning = true;
                    tabletLogNotifyOnError = true;
                }, true);
            });
        }

        #region IOverlayManager

        IBindable<OverlayActivation> IOverlayManager.OverlayActivationMode => OverlayActivationMode;

        private void updateBlockingOverlayFade() =>
            ScreenContainer.FadeColour(visibleBlockingOverlays.Any() ? OsuColour.Gray(0.5f) : Color4.White, 500, Easing.OutQuint);

        IDisposable IOverlayManager.RegisterBlockingOverlay(OverlayContainer overlayContainer)
        {
            if (overlayContainer.Parent != null)
                throw new ArgumentException($@"Overlays registered via {nameof(IOverlayManager.RegisterBlockingOverlay)} should not be added to the scene graph.");

            if (externalOverlays.Contains(overlayContainer))
                throw new ArgumentException($@"{overlayContainer} has already been registered via {nameof(IOverlayManager.RegisterBlockingOverlay)} once.");

            externalOverlays.Add(overlayContainer);

            if (overlayContainer is ShearedOverlayContainer)
                footerBasedOverlayContent.Add(overlayContainer);
            else
                overlayContent.Add(overlayContainer);

            if (overlayContainer is OsuFocusedOverlayContainer focusedOverlayContainer)
                focusedOverlays.Add(focusedOverlayContainer);

            return new InvokeOnDisposal(() => unregisterBlockingOverlay(overlayContainer));
        }

        void IOverlayManager.ShowBlockingOverlay(OverlayContainer overlay)
        {
            if (!visibleBlockingOverlays.Contains(overlay))
                visibleBlockingOverlays.Add(overlay);
            updateBlockingOverlayFade();
        }

        void IOverlayManager.HideBlockingOverlay(OverlayContainer overlay) => Schedule(() =>
        {
            visibleBlockingOverlays.Remove(overlay);
            updateBlockingOverlayFade();
        });

        /// <summary>
        /// Unregisters a blocking <see cref="OverlayContainer"/> that was not created by <see cref="OsuGame"/> itself.
        /// </summary>
        private void unregisterBlockingOverlay(OverlayContainer overlayContainer) => Schedule(() =>
        {
            externalOverlays.Remove(overlayContainer);

            if (overlayContainer is OsuFocusedOverlayContainer focusedOverlayContainer)
                focusedOverlays.Remove(focusedOverlayContainer);

            overlayContainer.Expire();
        });

        #endregion

        /// <summary>
        /// Close all game-wide overlays.
        /// </summary>
        /// <param name="hideToolbar">Whether the toolbar should also be hidden.</param>
        public void CloseAllOverlays(bool hideToolbar = true)
        {
            foreach (var overlay in focusedOverlays)
                overlay.Hide();

            ScreenFooter.ActiveOverlay?.Hide();

            if (hideToolbar) Toolbar.Hide();
        }

        protected override UserInputManager CreateUserInputManager()
        {
            var userInputManager = base.CreateUserInputManager();
            (userInputManager as OsuUserInputManager)?.PlayingState.BindTo(UserPlayingState);
            return userInputManager;
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        private readonly List<string> dragDropFiles = new List<string>();
        private ScheduledDelegate dragDropImportSchedule;

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            if (host.Window != null)
            {
                host.Window.CursorState |= CursorState.Hidden;
                host.Window.DragDrop += onWindowDragDrop;
            }
        }

        private void onWindowDragDrop(string path)
        {
            lock (dragDropFiles)
            {
                dragDropFiles.Add(path);

                Logger.Log($@"Adding ""{Path.GetFileName(path)}"" for import");

                // File drag drop operations can potentially trigger hundreds or thousands of these calls on some platforms.
                // In order to avoid spawning multiple import tasks for a single drop operation, debounce a touch.
                dragDropImportSchedule?.Cancel();
                dragDropImportSchedule = Scheduler.AddDelayed(handlePendingDragDropImports, 100);
            }

            void handlePendingDragDropImports()
            {
                lock (dragDropFiles)
                {
                    Logger.Log($"Handling batch import of {dragDropFiles.Count} files");

                    string[] paths = dragDropFiles.ToArray();
                    dragDropFiles.Clear();

                    Task.Factory.StartNew(() => Import(paths), TaskCreationOptions.LongRunning);
                }
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            dependencies.CacheAs(osuLogo = new OsuLogo { Alpha = 0 });

            uiScale = LocalConfig.GetBindable<float>(OsuSetting.UIScale);
            Ruleset.Value = RulesetStore.RulesetInfo;

            UserPlayingState.BindValueChanged(p =>
            {
                BeatmapManager.PauseImports = p.NewValue != LocalUserPlayingState.NotPlaying;
                ScoreManager.PauseImports = p.NewValue != LocalUserPlayingState.NotPlaying;
            }, true);

            IsActive.BindValueChanged(active => updateActiveState(active.NewValue), true);

            Audio.AddAdjustment(AdjustableProperty.Volume, inactiveVolumeFade);

            Beatmap.BindValueChanged(beatmapChanged, true);

            applySafeAreaConsiderations = LocalConfig.GetBindable<bool>(OsuSetting.SafeAreaConsiderations);
            applySafeAreaConsiderations.BindValueChanged(apply => SafeAreaContainer.SafeAreaOverrideEdges = apply.NewValue ? SafeAreaOverrideEdges : Edges.All, true);
        }

        public void CopyToClipboard(string value) => waitForReady(() => onScreenDisplay, _ =>
        {
            dependencies.Get<Clipboard>().SetText(value);
            onScreenDisplay.Display(new CopiedToClipboardToast());
        });

        /// <summary>
        /// Present a beatmap at song select immediately.
        /// The user should have already requested this interactively.
        /// </summary>
        /// <param name="beatmap">The beatmap to select.</param>
        /// <param name="difficultyCriteria">Optional predicate used to narrow the set of difficulties to select from when presenting.</param>
        /// <remarks>
        /// Among items satisfying the predicate, the order of preference is:
        /// <list type="bullet">
        /// <item>first beatmap from the current ruleset,</item>
        /// <item>first beatmap from any ruleset.</item>
        /// </list>
        /// </remarks>
        public void PresentBeatmap(IBeatmapSetInfo beatmap, Predicate<BeatmapInfo> difficultyCriteria = null)
        {
            Logger.Log($"Beginning {nameof(PresentBeatmap)} with beatmap {beatmap}");
            Live<BeatmapSetInfo> databasedSet = null;

            if (beatmap is BeatmapSetInfo localBeatmap)
                databasedSet = BeatmapManager.QueryBeatmapSet(s => s.Hash == localBeatmap.Hash && !s.DeletePending);

            if (databasedSet == null)
            {
                Logger.Log("The requested beatmap could not be loaded.", LoggingTarget.Information);
                return;
            }

            var detachedSet = databasedSet.PerformRead(s => s.Detach());

            if (detachedSet.DeletePending)
            {
                Logger.Log("The requested beatmap has since been deleted.", LoggingTarget.Information);
                return;
            }

            PerformFromScreen(screen =>
            {
                // Find beatmaps that match our predicate.
                var beatmaps = detachedSet.Beatmaps.Where(b => difficultyCriteria?.Invoke(b) ?? true).ToList();

                // Use all beatmaps if predicate matched nothing
                if (beatmaps.Count == 0)
                    beatmaps = detachedSet.Beatmaps.ToList();

                // Prefer a beatmap matching the current ruleset, else fall back to a sane selection.
                var selection = beatmaps.FirstOrDefault(b => b.Ruleset.Equals(Ruleset.Value))
                                ?? beatmaps.First();

                if (screen is IHandlePresentBeatmap presentableScreen)
                {
                    presentableScreen.PresentBeatmap(BeatmapManager.GetWorkingBeatmap(selection), selection.Ruleset);
                }
                else
                {
                    bool requiresRulesetSwitch = !selection.Ruleset.Equals(Ruleset.Value);

                    if (requiresRulesetSwitch)
                    {
                        Ruleset.Value = selection.Ruleset;
                        Beatmap.Value = BeatmapManager.GetWorkingBeatmap(selection);

                        Logger.Log($"Completing {nameof(PresentBeatmap)} with beatmap {beatmap} ruleset {selection.Ruleset}");
                    }
                    else
                    {
                        Beatmap.Value = BeatmapManager.GetWorkingBeatmap(selection);

                        Logger.Log($"Completing {nameof(PresentBeatmap)} with beatmap {beatmap} (maintaining ruleset)");
                    }
                }
            }, validScreens: new[]
            {
                typeof(SongSelect), typeof(IHandlePresentBeatmap)
            });
        }

        /// <summary>
        /// Present a score's replay immediately.
        /// The user should have already requested this interactively.
        /// </summary>
        public void PresentScore(ScoreInfo score, ScorePresentType presentType = ScorePresentType.Results)
        {
            Logger.Log($"Beginning {nameof(PresentScore)} with score {score}");

            Score databasedScore;

            try
            {
                databasedScore = ScoreManager.GetScore(score);
            }
            catch (LegacyScoreDecoder.BeatmapNotFoundException)
            {
                Logger.Log("The replay cannot be played because the beatmap is missing.", LoggingTarget.Information);
                return;
            }

            if (databasedScore == null) return;

            if (databasedScore.Replay == null)
            {
                Logger.Log("The loaded score has no replay data.", LoggingTarget.Information, LogLevel.Important);
                return;
            }

            var databasedBeatmap = databasedScore.ScoreInfo.BeatmapInfo;
            Debug.Assert(databasedBeatmap != null);

            // This should be able to be performed from song select always, but that is disabled for now
            // due to the weird decoupled ruleset logic (which can cause a crash in certain filter scenarios).
            //
            // As a special case, if the beatmap and ruleset already match, allow immediately displaying the score from song select.
            // This is guaranteed to not crash, and feels better from a user's perspective (ie. if they are clicking a score in the
            // song select leaderboard).
            // Similar exemptions are made here for daily challenge where it is guaranteed that beatmap and ruleset match.
            // `OnlinePlayScreen` is excluded because when resuming back to it,
            // `RoomSubScreen` changes the global beatmap to the next playlist item on resume,
            // which may not match the score, and thus crash.
            IEnumerable<Type> validScreens =
                Beatmap.Value.BeatmapInfo.Equals(databasedBeatmap) && Ruleset.Value.Equals(databasedScore.ScoreInfo.Ruleset)
                    ? new[] { typeof(SongSelect) }
                    : Array.Empty<Type>();

            PerformFromScreen(screen =>
            {
                Logger.Log($"{nameof(PresentScore)} updating beatmap ({databasedBeatmap}) and ruleset ({databasedScore.ScoreInfo.Ruleset}) to match score");

                // some screens (mostly online) disable the ruleset/beatmap bindable.
                // attempting to set the ruleset/beatmap in that state will crash.
                // however, the `validScreens` pre-check above should ensure that we actually never come from one of those screens
                // while simultaneously having mismatched ruleset/beatmap.
                // therefore this is just a safety against touching the possibly-disabled bindables if we don't actually have to touch them.
                // if it ever fails, then this probably *should* crash anyhow (so that we can fix it).
                if (!Ruleset.Value.Equals(databasedScore.ScoreInfo.Ruleset))
                    Ruleset.Value = databasedScore.ScoreInfo.Ruleset;

                if (!Beatmap.Value.BeatmapInfo.Equals(databasedBeatmap))
                    Beatmap.Value = BeatmapManager.GetWorkingBeatmap(databasedBeatmap);

                switch (presentType)
                {
                    case ScorePresentType.Gameplay:
                        screen.Push(new ReplayPlayerLoader(databasedScore));
                        break;

                    case ScorePresentType.Results:
                        screen.Push(new SoloResultsScreen(databasedScore.ScoreInfo));
                        break;
                }
            }, validScreens: validScreens);
        }

        public override Task Import(ImportTask[] imports, ImportParameters parameters = default)
        {
            // encapsulate task as we don't want to begin the import process until in a ready state.

            // ReSharper disable once AsyncVoidLambda
            // TODO: This is bad because `new Task` doesn't have a Func<Task?> override.
            // Only used for android imports and a bit of a mess. Probably needs rethinking overall.
            var importTask = new Task(async () => await base.Import(imports, parameters).ConfigureAwait(false));

            waitForReady(() => this, _ => importTask.Start());

            return importTask;
        }

        protected virtual Loader CreateLoader() => new Loader();

        /// <summary>
        /// Adjust the globally applied <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/> in every <see cref="ScalingContainer"/>.
        /// Useful for changing how the game handles different aspect ratios.
        /// </summary>
        public virtual Vector2 ScalingContainerTargetDrawSize { get; } = new Vector2(1024, 768);

        protected override Container CreateScalingContainer() => new ScalingContainer(ScalingMode.Everything);

        #region Beatmap progression

        private void beatmapChanged(ValueChangedEvent<WorkingBeatmap> beatmap)
        {
            beatmap.OldValue?.CancelAsyncLoad();
            beatmap.NewValue?.BeginAsyncLoad();
            updateWindowTitle();
        }

        private void updateWindowTitle()
        {
            if (Host.Window == null)
                return;

            string newTitle;

            newTitle = ScreenStack?.CurrentScreen is Player
                ? $"{Name} - {Beatmap.Value.BeatmapInfo.GetDisplayTitleRomanisable(true, false)}"
                : Name;

            if (newTitle != Host.Window.Title)
                Host.Window.Title = newTitle;
        }

        #endregion

        private PerformFromMenuRunner performFromMainMenuTask;

        public void PerformFromScreen(Action<IScreen> action, IEnumerable<Type> validScreens = null)
        {
            performFromMainMenuTask?.Cancel();
            Add(performFromMainMenuTask = new PerformFromMenuRunner(action, validScreens, () => ScreenStack.CurrentScreen));
        }

        public override void AttemptExit()
        {
            // The main menu exit implementation gives the user a chance to interrupt the exit process if needed.
            PerformFromScreen(menu => menu.Exit(), new[] { typeof(MainMenu) });
        }

        /// <summary>
        /// Wait for the game (and target component) to become loaded and then run an action.
        /// </summary>
        /// <param name="retrieveInstance">A function to retrieve a (potentially not-yet-constructed) target instance.</param>
        /// <param name="action">The action to perform on the instance when load is confirmed.</param>
        /// <typeparam name="T">The type of the target instance.</typeparam>
        private void waitForReady<T>(Func<T> retrieveInstance, Action<T> action)
            where T : Drawable
        {
            var instance = retrieveInstance();

            if (ScreenStack == null || ScreenStack.CurrentScreen is StartupScreen || instance?.IsLoaded != true)
                Schedule(() => waitForReady(retrieveInstance, action));
            else
                action(instance);
        }

        protected override void Dispose(bool isDisposing)
        {
            // Without this, tests may deadlock due to cancellation token not becoming cancelled before disposal.
            // To reproduce, run `TestSceneButtonSystemNavigation` ensuring `TestConstructor` runs before `TestFastShortcutKeys`.
            detachedBeatmapStore?.Dispose();

            base.Dispose(isDisposing);

            if (Host?.Window != null)
                Host.Window.DragDrop -= onWindowDragDrop;

            Logger.NewEntry -= forwardGeneralLogToNotifications;
            Logger.NewEntry -= forwardTabletLogToNotifications;
        }

        protected override IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults()
        {
            return new Dictionary<FrameworkSetting, object>
            {
                // General expectation that osu! starts in fullscreen by default (also gives the most predictable performance).
                // However, macOS is bound to have issues when using exclusive fullscreen as it takes full control away from OS, therefore borderless is default there.
                { FrameworkSetting.WindowMode, RuntimeInfo.OS == RuntimeInfo.Platform.macOS ? WindowMode.Borderless : WindowMode.Fullscreen },
                { FrameworkSetting.VolumeUniversal, 0.6 },
                { FrameworkSetting.VolumeMusic, 0.6 },
                { FrameworkSetting.VolumeEffect, 0.6 },
                { FrameworkSetting.AudioUseExperimentalWasapi, true },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The next time this is updated is in UpdateAfterChildren, which occurs too late and results
            // in the cursor being shown for a few frames during the intro.
            // This prevents the cursor from showing until we have a screen with CursorVisible = true
            GlobalCursorDisplay.ShowCursor = menuScreen?.CursorVisible ?? false;

            // todo: all archive managers should be able to be looped here.
            BeatmapManager.PostNotification = n => Notifications.Post(n);
            BeatmapManager.PresentImport = items => PresentBeatmap(items.First().Value);

            ScoreManager.PostNotification = n => Notifications.Post(n);
            ScoreManager.PresentImport = items => PresentScore(items.First().Value);

            ScreenFooter.BackReceptor backReceptor;

            dependencies.CacheAs(idleTracker = new GameIdleTracker(6000));

            var sessionIdleTracker = new GameIdleTracker(300000);
            sessionIdleTracker.IsIdle.BindValueChanged(idle =>
            {
                if (idle.NewValue)
                    SessionStatics.ResetAfterInactivity();
            });

            Add(sessionIdleTracker);

            Container logoContainer;

            AddRange(new Drawable[]
            {
                ScreenOffsetContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        ScreenContainer = new ScalingContainer(ScalingMode.ExcludeOverlays)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                backReceptor = new ScreenFooter.BackReceptor(),
                                ScreenStack = new OsuScreenStack { RelativeSizeAxes = Axes.Both },
                                logoContainer = new Container { RelativeSizeAxes = Axes.Both },
                                // TODO: what is this? why is this?
                                // TODO: this is being screen scaled even though it's probably AN OVERLAY.
                                footerBasedOverlayContent = new Container
                                {
                                    Depth = -1,
                                    RelativeSizeAxes = Axes.Both,
                                },
                                new PopoverContainer
                                {
                                    // Ensure the footer is displayed above any content and/or overlays.
                                    Depth = -1,
                                    RelativeSizeAxes = Axes.Both,
                                    Child = screenStackFooter = new ScreenStackFooter(ScreenStack, backReceptor)
                                    {
                                        // TODO: this is really really weird and should not exist.
                                        RequestLogoInFront = inFront => ScreenContainer.ChangeChildDepth(logoContainer, inFront ? float.MinValue : 0),
                                        BackButtonPressed = handleBackButton
                                    },
                                },
                            }
                        },
                    }
                },
                overlayOffsetContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        overlayContent = new Container { RelativeSizeAxes = Axes.Both },
                        leftFloatingOverlayContent = new Container { RelativeSizeAxes = Axes.Both },
                        rightFloatingOverlayContent = new Container { RelativeSizeAxes = Axes.Both },
                    }
                },
                topMostOverlayContent = new Container { RelativeSizeAxes = Axes.Both },
                idleTracker,
                new ConfineMouseTracker()
            });

            dependencies.Cache(ScreenFooter);

            ScreenStack.ScreenPushed += screenPushed;
            ScreenStack.ScreenExited += screenExited;

            loadComponentSingleFile(fpsCounter = new FPSCounter
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding(5),
            }, topMostOverlayContent.Add);

            if (!IsDeployedBuild)
                loadComponentSingleFile(devBuildBanner = new DevBuildBanner(), ScreenContainer.Add);

            loadComponentSingleFile(osuLogo, _ =>
            {
                osuLogo.SetupDefaultContainer(logoContainer);

                // Loader has to be created after the logo has finished loading as Loader performs logo transformations on entering.
                ScreenStack.Push(CreateLoader().With(l => l.RelativeSizeAxes = Axes.Both));
            });

            loadComponentSingleFile(Toolbar = new Toolbar
            {
                OnHome = delegate
                {
                    CloseAllOverlays(false);

                    if (menuScreen?.GetChildScreen() != null)
                        menuScreen.MakeCurrent();
                },
            }, topMostOverlayContent.Add);

            loadComponentSingleFile(volume = new VolumeOverlay(), leftFloatingOverlayContent.Add, true);

            onScreenDisplay = new OnScreenDisplay();

            onScreenDisplay.BeginTracking(this, frameworkConfig);
            onScreenDisplay.BeginTracking(this, LocalConfig);

            loadComponentSingleFile(onScreenDisplay, Add, true);

            loadComponentSingleFile(Notifications, rightFloatingOverlayContent.Add, true);

            loadComponentSingleFile(screenshotManager, Add);

            // overlay elements
            loadComponentSingleFile(Settings = new SettingsOverlay(), leftFloatingOverlayContent.Add, true);

            loadComponentSingleFile(new NowPlayingOverlay
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
            }, rightFloatingOverlayContent.Add, true);

            loadComponentSingleFile<IDialogOverlay>(dialogOverlay = new DialogOverlay(), topMostOverlayContent.Add, true);

            loadComponentSingleFile<BeatmapStore>(detachedBeatmapStore = new RealmDetachedBeatmapStore(), Add, true);

            Add(new MusicKeyBindingHandler());

            // side overlays which cancel each other.
            var singleDisplaySideOverlays = new OverlayContainer[] { Settings };

            foreach (var overlay in singleDisplaySideOverlays)
            {
                overlay.State.ValueChanged += state =>
                {
                    if (state.NewValue == Visibility.Hidden) return;

                    singleDisplaySideOverlays.Where(o => o != overlay).ForEach(o => o.Hide());
                };
            }

            OverlayActivationMode.ValueChanged += mode =>
            {
                if (mode.NewValue != OverlayActivation.All) CloseAllOverlays();
            };

            // Importantly, this should be run after binding PostNotification to the import handlers so they can present the import after game startup.
            handleStartupImport();

        }

        private void handleBackButton()
        {
            // TODO: this is SUPER SUPER bad.
            // It can potentially exit the wrong screen if screens are not loaded yet.
            // ScreenFooter / ScreenBackButton should be aware of which screen it is currently being handled by.
            if (!(ScreenStack.CurrentScreen is IOsuScreen currentScreen)) return;

            if (!((Drawable)currentScreen).IsLoaded || (currentScreen.AllowUserExit && !currentScreen.OnBackButton())) ScreenStack.Exit();
        }

        private void handleStartupImport()
        {
            if (args?.Length > 0)
            {
                string[] paths = args.Where(a => !a.StartsWith('-')).ToArray();

                if (paths.Length > 0)
                {
                    string firstPath = paths.First();

                    Task.Run(() => Import(paths));
                }
            }
        }

        private void showOverlayAboveOthers(OverlayContainer overlay, OverlayContainer[] otherOverlays)
        {
            otherOverlays.Where(o => o != overlay).ForEach(o => o.Hide());

            Settings.Hide();
            Notifications.Hide();

            // Partially visible so leave it at the current depth.
            if (overlay.IsPresent)
                return;

            // Show above all other overlays.
            if (overlay.IsLoaded)
                overlayContent.ChangeChildDepth(overlay, (float)-Clock.CurrentTime);
            else
                overlay.Depth = (float)-Clock.CurrentTime;
        }

        private void forwardGeneralLogToNotifications(LogEntry entry)
        {
            if (entry.Level < LogLevel.Important || entry.Target > LoggingTarget.Database || entry.Target == null) return;

            const int short_term_display_limit = 3;

            if (generalLogRecentCount < short_term_display_limit)
            {
                LocalisableString message = entry.Message.Truncate(256);

                Schedule(() => Notifications.Post(new SimpleErrorNotification
                {
                    Icon = entry.Level == LogLevel.Important ? FontAwesome.Solid.ExclamationCircle : FontAwesome.Solid.Bomb,
                    Text = message
                }));
            }
            else if (generalLogRecentCount == short_term_display_limit)
            {
                string logFile = Logger.GetLogger(entry.Target.Value).Filename;

                Schedule(() => Notifications.Post(new SimpleNotification
                {
                    Icon = FontAwesome.Solid.EllipsisH,
                    Text = NotificationsStrings.SubsequentMessagesLogged,
                    Activated = () =>
                    {
                        Logger.Storage.PresentFileExternally(logFile);
                        return true;
                    }
                }));
            }

            Interlocked.Increment(ref generalLogRecentCount);
            Scheduler.AddDelayed(() => Interlocked.Decrement(ref generalLogRecentCount), general_log_debounce);
        }

        private void forwardTabletLogToNotifications(LogEntry entry)
        {
            if (entry.Level < LogLevel.Important || entry.Target != LoggingTarget.Input || !entry.Message.StartsWith(tablet_log_prefix, StringComparison.OrdinalIgnoreCase))
                return;

            string message = entry.Message.Replace(tablet_log_prefix, string.Empty);

            if (entry.Level == LogLevel.Error)
            {
                if (!tabletLogNotifyOnError)
                    return;

                tabletLogNotifyOnError = false;

                Schedule(() =>
                {
                    Notifications.Post(new SimpleNotification
                    {
                        Text = NotificationsStrings.TabletSupportDisabledDueToError(message),
                        Icon = FontAwesome.Solid.PenSquare,
                        IconColour = Colours.RedDark,
                    });

                    // We only have one tablet handler currently.
                    // The loop here is weakly guarding against a future where more than one is added.
                    // If this is ever the case, this logic needs adjustment as it should probably only
                    // disable the relevant tablet handler rather than all.
                    foreach (var tabletHandler in Host.AvailableInputHandlers.OfType<ITabletHandler>())
                        tabletHandler.Enabled.Value = false;
                });
            }
            else if (tabletLogNotifyOnWarning)
            {
                Schedule(() => Notifications.Post(new SimpleNotification
                {
                    Text = NotificationsStrings.EncounteredTabletWarning,
                    Icon = FontAwesome.Solid.PenSquare,
                    IconColour = Colours.YellowDark,
                    Activated = () =>
                    {
                        ExternalBrowser.Open(ExternalBrowserDestination.TabletList);
                        return true;
                    }
                }));

                tabletLogNotifyOnWarning = false;
            }
        }

        private Task asyncLoadStream;

        /// <summary>
        /// Queues loading the provided component in sequential fashion.
        /// This operation is limited to a single thread to avoid saturating all cores.
        /// </summary>
        /// <param name="component">The component to load.</param>
        /// <param name="loadCompleteAction">An action to invoke on load completion (generally to add the component to the hierarchy).</param>
        /// <param name="cache">Whether to cache the component as type <typeparamref name="T"/> into the game dependencies before any scheduling.</param>
        private T loadComponentSingleFile<T>(T component, Action<Drawable> loadCompleteAction, bool cache = false)
            where T : class
        {
            if (cache)
                dependencies.CacheAs(component);

            var drawableComponent = component as Drawable ?? throw new ArgumentException($"Component must be a {nameof(Drawable)}", nameof(component));

            if (component is OsuFocusedOverlayContainer overlay)
                focusedOverlays.Add(overlay);

            // schedule is here to ensure that all component loads are done after LoadComplete is run (and thus all dependencies are cached).
            // with some better organisation of LoadComplete to do construction and dependency caching in one step, followed by calls to loadComponentSingleFile,
            // we could avoid the need for scheduling altogether.
            Schedule(() =>
            {
                var previousLoadStream = asyncLoadStream;

                // chain with existing load stream
                asyncLoadStream = Task.Run(async () =>
                {
                    if (previousLoadStream != null)
                        await previousLoadStream.ConfigureAwait(false);

                    try
                    {
                        Logger.Log($"Loading {component}...");

                        // Since this is running in a separate thread, it is possible for OsuGame to be disposed after LoadComponentAsync has been called
                        // throwing an exception. To avoid this, the call is scheduled on the update thread, which does not run if IsDisposed = true
                        Task task = null;
                        var del = new ScheduledDelegate(() => task = LoadComponentAsync(drawableComponent, loadCompleteAction));
                        Scheduler.Add(del);

                        // The delegate won't complete if OsuGame has been disposed in the meantime
                        while (!IsDisposed && !del.Completed)
                            await Task.Delay(10).ConfigureAwait(false);

                        // Either we're disposed or the load process has started successfully
                        if (IsDisposed)
                            return;

                        Debug.Assert(task != null);

                        await task.ConfigureAwait(false);

                        Logger.Log($"Loaded {component}!");
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            });

            return component;
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.DecreaseVolume:
                case GlobalAction.IncreaseVolume:
                    return volume.Adjust(e.Action);
            }

            // All actions below this point don't allow key repeat.
            if (e.Repeat)
                return false;

            if (menuScreen == null) return false;

            switch (e.Action)
            {
                case GlobalAction.ToggleMute:
                case GlobalAction.NextVolumeMeter:
                case GlobalAction.PreviousVolumeMeter:
                    return volume.Adjust(e.Action);

                case GlobalAction.ToggleFPSDisplay:
                    fpsCounter.ToggleVisibility();
                    return true;

                case GlobalAction.ResetInputSettings:
                    Host.ResetInputHandlers();
                    frameworkConfig.GetBindable<ConfineMouseMode>(FrameworkSetting.ConfineMouseMode).SetDefault();
                    return true;

                case GlobalAction.ToggleGameplayMouseButtons:
                    var mouseDisableButtons = LocalConfig.GetBindable<bool>(OsuSetting.MouseDisableButtons);
                    mouseDisableButtons.Value = !mouseDisableButtons.Value;
                    return true;
            }

            return false;
        }

        public override bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            const float adjustment_increment = 0.05f;

            switch (e.Action)
            {
                case PlatformAction.ZoomIn:
                    uiScale.Value += adjustment_increment;
                    return true;

                case PlatformAction.ZoomOut:
                    uiScale.Value -= adjustment_increment;
                    return true;

                case PlatformAction.ZoomDefault:
                    uiScale.SetDefault();
                    return true;
            }

            return base.OnPressed(e);
        }

        #region Inactive audio dimming

        private readonly BindableDouble inactiveVolumeFade = new BindableDouble();

        private void updateActiveState(bool isActive)
        {
            if (isActive)
                this.TransformBindableTo(inactiveVolumeFade, 1, 400, Easing.OutQuint);
            else
                this.TransformBindableTo(inactiveVolumeFade, LocalConfig.Get<double>(OsuSetting.VolumeInactive), 4000, Easing.OutQuint);
        }

        #endregion

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        protected override bool OnExiting()
        {
            if (ScreenStack.CurrentScreen is Loader)
                return false;

            return base.OnExiting();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            ScreenOffsetContainer.Padding = new MarginPadding { Top = toolbarOffset };
            overlayOffsetContainer.Padding = new MarginPadding { Top = toolbarOffset };

            adjustGlobalScreenOffset();

            GlobalCursorDisplay.ShowCursor = (ScreenStack.CurrentScreen as IOsuScreen)?.CursorVisible ?? false;
        }

        private float horizontalOffsetAdjust;

        /// <summary>
        /// When a screen-edge overlay is present, we push the game content slightly in its direction to create a sense of depth.
        /// </summary>
        private void adjustGlobalScreenOffset()
        {
            float adjust = 0f;

            if (Settings.IsLoaded && Settings.State.Value == Visibility.Visible)
                adjust += SettingsPanel.WIDTH * SIDE_OVERLAY_OFFSET_RATIO;

            horizontalOffsetAdjust = (float)Interpolation.DampContinuously(horizontalOffsetAdjust, adjust, 100, Time.Elapsed);
            // Avoid having everything on the screen moving by miniscule amounts (can create overhead on busy screens).
            if (adjust == 0 && Math.Abs(horizontalOffsetAdjust) < 0.2f)
                horizontalOffsetAdjust = 0;

            ScreenOffsetContainer.X = horizontalOffsetAdjust;
            overlayContent.X = horizontalOffsetAdjust * 1.2f;
        }

        protected virtual void ScreenChanged([CanBeNull] IOsuScreen current, [CanBeNull] IOsuScreen newScreen)
        {
            switch (current)
            {
                case Player player:
                    player.PlayingState.UnbindFrom(UserPlayingState);

                    // reset for sanity.
                    UserPlayingState.Value = LocalUserPlayingState.NotPlaying;
                    break;
            }

            switch (newScreen)
            {
                case MainMenu menu:
                    menuScreen = menu;
                    devBuildBanner?.Show();
                    break;

                case Player player:
                    player.PlayingState.BindTo(UserPlayingState);
                    break;

                default:
                    devBuildBanner?.Hide();
                    break;
            }

            if (current != null)
            {
                OverlayActivationMode.UnbindFrom(current.OverlayActivationMode);
            }

            // Bind to new screen.
            if (newScreen is OsuScreen newOsuScreen)
            {
                OverlayActivationMode.BindTo(newScreen.OverlayActivationMode);

                // Handle various configuration updates based on new screen settings.
                GlobalCursorDisplay.MenuCursor.HideCursorOnNonMouseInput = newScreen.HideMenuCursorOnNonMouseInput;

                if (newScreen.HideOverlaysOnEnter)
                    CloseAllOverlays();
                else
                    Toolbar.Show();
            }

            updateWindowTitle();
        }

        private void screenPushed(IScreen lastScreen, IScreen newScreen) => ScreenChanged((OsuScreen)lastScreen, (OsuScreen)newScreen);

        private void screenExited(IScreen lastScreen, IScreen newScreen)
        {
            ScreenChanged((OsuScreen)lastScreen, (OsuScreen)newScreen);

            if (newScreen == null)
                Exit();
        }
    }
}

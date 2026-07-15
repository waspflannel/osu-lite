// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Input.Bindings
{
    public partial class GlobalActionContainer : DatabasedKeyBindingContainer<GlobalAction>, IHandleGlobalKeyboardInput, IKeyBindingHandler<GlobalAction>
    {
        protected override bool Prioritised => true;

        private readonly IKeyBindingHandler<GlobalAction>? handler;

        public GlobalActionContainer(OsuGameBase? game)
            : base(matchingMode: KeyCombinationMatchingMode.Modifiers)
        {
            if (game is IKeyBindingHandler<GlobalAction> h)
                handler = h;
        }

        /// <summary>
        /// All default key bindings across all categories, ordered with highest priority first.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Take care when changing order of the items in the enumerable.
        /// It is used to decide the order of precedence, with the earlier items having higher precedence.
        /// </remarks>
        public override IEnumerable<IKeyBinding> DefaultKeyBindings => globalKeyBindings
                                                                        .Concat(inGameKeyBindings)
                                                                       .Concat(replayKeyBindings)
                                                                       .Concat(songSelectKeyBindings)
                                                                       .Concat(audioControlKeyBindings)
                                                                       // Overlay bindings may conflict with more local cases like the editor so they are checked last.
                                                                       // It has generally been agreed on that local screens like the editor should have priority,
                                                                       // based on such usages potentially requiring a lot more key bindings that may be "shared" with global ones.
                                                                       .Concat(overlayKeyBindings);

        public static IEnumerable<KeyBinding> GetDefaultBindingsFor(GlobalActionCategory category)
        {
            switch (category)
            {
                case GlobalActionCategory.General:
                    return globalKeyBindings;

                case GlobalActionCategory.InGame:
                    return inGameKeyBindings;

                case GlobalActionCategory.Replay:
                    return replayKeyBindings;

                case GlobalActionCategory.SongSelect:
                    return songSelectKeyBindings;

                case GlobalActionCategory.AudioControl:
                    return audioControlKeyBindings;

                case GlobalActionCategory.Overlays:
                    return overlayKeyBindings;

                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, $"Unexpected {nameof(GlobalActionCategory)}");
            }
        }

        public static IEnumerable<GlobalAction> GetGlobalActionsFor(GlobalActionCategory category)
            => GetDefaultBindingsFor(category).Select(binding => binding.Action).Cast<GlobalAction>().Distinct();

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e) => handler?.OnPressed(e) == true;

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) => handler?.OnReleased(e);

        private static IEnumerable<KeyBinding> globalKeyBindings => new[]
        {
            new KeyBinding(InputKey.Up, GlobalAction.SelectPrevious),
            new KeyBinding(InputKey.Down, GlobalAction.SelectNext),

            new KeyBinding(InputKey.Space, GlobalAction.Select),
            new KeyBinding(InputKey.Enter, GlobalAction.Select),
            new KeyBinding(InputKey.KeypadEnter, GlobalAction.Select),

            new KeyBinding(InputKey.Escape, GlobalAction.Back),
            new KeyBinding(InputKey.ExtraMouseButton1, GlobalAction.Back),

            new KeyBinding(new[] { InputKey.Alt, InputKey.Home }, GlobalAction.Home),

            new KeyBinding(InputKey.None, GlobalAction.ToggleFPSDisplay),
            new KeyBinding(new[] { InputKey.Control, InputKey.T }, GlobalAction.ToggleToolbar),

            new KeyBinding(new[] { InputKey.Control, InputKey.Alt, InputKey.R }, GlobalAction.ResetInputSettings),

            new KeyBinding(InputKey.F10, GlobalAction.ToggleGameplayMouseButtons),
            new KeyBinding(InputKey.F12, GlobalAction.TakeScreenshot),
        };

        private static IEnumerable<KeyBinding> overlayKeyBindings => new[]
        {
            new KeyBinding(InputKey.F6, GlobalAction.ToggleNowPlaying),
            new KeyBinding(new[] { InputKey.Control, InputKey.O }, GlobalAction.ToggleSettings),
        };

        private static IEnumerable<KeyBinding> inGameKeyBindings => new[]
        {
            new KeyBinding(InputKey.Space, GlobalAction.SkipCutscene),
            new KeyBinding(InputKey.ExtraMouseButton2, GlobalAction.SkipCutscene),
            new KeyBinding(InputKey.Tilde, GlobalAction.QuickRetry),
            new KeyBinding(new[] { InputKey.Control, InputKey.R }, GlobalAction.QuickRetry),
            new KeyBinding(new[] { InputKey.Control, InputKey.Tilde }, GlobalAction.QuickExit),
            new KeyBinding(new[] { InputKey.Shift, InputKey.Tab }, GlobalAction.ToggleInGameInterface),
            new KeyBinding(InputKey.Tab, GlobalAction.ToggleInGameLeaderboard),
            new KeyBinding(InputKey.MouseMiddle, GlobalAction.PauseGameplay),
            new KeyBinding(InputKey.Control, GlobalAction.HoldForHUD),
            new KeyBinding(InputKey.F1, GlobalAction.SaveReplay),
            new KeyBinding(InputKey.F2, GlobalAction.ExportReplay),
            new KeyBinding(InputKey.Plus, GlobalAction.IncreaseOffset),
            new KeyBinding(InputKey.Minus, GlobalAction.DecreaseOffset),
        };

        private static IEnumerable<KeyBinding> replayKeyBindings => new[]
        {
            new KeyBinding(InputKey.Space, GlobalAction.TogglePauseReplay),
            new KeyBinding(InputKey.MouseMiddle, GlobalAction.TogglePauseReplay),
            new KeyBinding(InputKey.Shift, GlobalAction.FastForwardReplay),
            new KeyBinding(InputKey.Left, GlobalAction.SeekReplayBackward),
            new KeyBinding(InputKey.Right, GlobalAction.SeekReplayForward),
            new KeyBinding(InputKey.Comma, GlobalAction.StepReplayBackward),
            new KeyBinding(InputKey.Period, GlobalAction.StepReplayForward),
            new KeyBinding(new[] { InputKey.Control, InputKey.H }, GlobalAction.ToggleReplaySettings),
        };

        private static IEnumerable<KeyBinding> songSelectKeyBindings => new[]
        {
            new KeyBinding(InputKey.Left, GlobalAction.ActivatePreviousSet),
            new KeyBinding(InputKey.Right, GlobalAction.ActivateNextSet),

            new KeyBinding(new[] { InputKey.Shift, InputKey.Left }, GlobalAction.ExpandPreviousGroup),
            new KeyBinding(new[] { InputKey.Shift, InputKey.Right }, GlobalAction.ExpandNextGroup),

            new KeyBinding(new[] { InputKey.Shift, InputKey.Enter }, GlobalAction.ToggleCurrentGroup),

            new KeyBinding(InputKey.F2, GlobalAction.SelectNextRandom),
            new KeyBinding(new[] { InputKey.Shift, InputKey.F2 }, GlobalAction.SelectPreviousRandom),
            new KeyBinding(InputKey.F3, GlobalAction.ToggleBeatmapOptions),
            new KeyBinding(InputKey.None, GlobalAction.AbsoluteScrollSongList),
        };

        private static IEnumerable<KeyBinding> audioControlKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Alt, InputKey.Up }, GlobalAction.IncreaseVolume),
            new KeyBinding(new[] { InputKey.Alt, InputKey.Down }, GlobalAction.DecreaseVolume),

            new KeyBinding(new[] { InputKey.Alt, InputKey.Left }, GlobalAction.PreviousVolumeMeter),
            new KeyBinding(new[] { InputKey.Alt, InputKey.Right }, GlobalAction.NextVolumeMeter),

            new KeyBinding(new[] { InputKey.Control, InputKey.F4 }, GlobalAction.ToggleMute),

            new KeyBinding(InputKey.TrackPrevious, GlobalAction.MusicPrev),
            new KeyBinding(InputKey.F1, GlobalAction.MusicPrev),
            new KeyBinding(InputKey.TrackNext, GlobalAction.MusicNext),
            new KeyBinding(InputKey.F5, GlobalAction.MusicNext),
            new KeyBinding(InputKey.PlayPause, GlobalAction.MusicPlay),
            new KeyBinding(InputKey.F3, GlobalAction.MusicPlay)
        };
    }

    public enum GlobalAction
    {
        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ResetInputSettings))]
        ResetInputSettings = 0,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleToolbar))]
        ToggleToolbar = 1,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleSettings))]
        ToggleSettings = 2,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.IncreaseVolume))]
        IncreaseVolume = 3,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.DecreaseVolume))]
        DecreaseVolume = 4,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleMute))]
        ToggleMute = 5,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SkipCutscene))]
        SkipCutscene = 6,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.QuickRetry))]
        QuickRetry = 7,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.TakeScreenshot))]
        TakeScreenshot = 8,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleGameplayMouseButtons))]
        ToggleGameplayMouseButtons = 9,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.Back))]
        Back = 10,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.Select))]
        Select = 11,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.QuickExit))]
        QuickExit = 12,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.MusicNext))]
        MusicNext = 13,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.MusicPrev))]
        MusicPrev = 14,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.MusicPlay))]
        MusicPlay = 15,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleNowPlaying))]
        ToggleNowPlaying = 16,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SelectPrevious))]
        SelectPrevious = 17,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SelectNext))]
        SelectNext = 18,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.Home))]
        Home = 19,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.PauseGameplay))]
        PauseGameplay = 20,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.HoldForHUD))]
        HoldForHUD = 21,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.TogglePauseReplay))]
        TogglePauseReplay = 22,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleInGameInterface))]
        ToggleInGameInterface = 23,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SelectNextRandom))]
        SelectNextRandom = 24,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SelectPreviousRandom))]
        SelectPreviousRandom = 25,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleBeatmapOptions))]
        ToggleBeatmapOptions = 26,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.PreviousVolumeMeter))]
        PreviousVolumeMeter = 27,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.NextVolumeMeter))]
        NextVolumeMeter = 28,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SeekReplayForward))]
        SeekReplayForward = 29,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SeekReplayBackward))]
        SeekReplayBackward = 30,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ActivatePreviousSet))]
        ActivatePreviousSet = 31,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ActivateNextSet))]
        ActivateNextSet = 32,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleFPSCounter))]
        ToggleFPSDisplay = 33,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.SaveReplay))]
        SaveReplay = 34,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ExportReplay))]
        ExportReplay = 35,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleReplaySettings))]
        ToggleReplaySettings = 36,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleInGameLeaderboard))]
        ToggleInGameLeaderboard = 37,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.IncreaseOffset))]
        IncreaseOffset = 38,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.DecreaseOffset))]
        DecreaseOffset = 39,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.StepReplayForward))]
        StepReplayForward = 40,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.StepReplayBackward))]
        StepReplayBackward = 41,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.AbsoluteScrollSongList))]
        AbsoluteScrollSongList = 42,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ExpandPreviousGroup))]
        ExpandPreviousGroup = 43,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ExpandNextGroup))]
        ExpandNextGroup = 44,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.ToggleCurrentGroup))]
        ToggleCurrentGroup = 45,

        [LocalisableDescription(typeof(GlobalActionKeyBindingStrings), nameof(GlobalActionKeyBindingStrings.FastForwardReplay))]
        FastForwardReplay = 46
    }

    public enum GlobalActionCategory
    {
        General,
        InGame,
        Replay,
        SongSelect,
        AudioControl,
        Overlays,
    }
}

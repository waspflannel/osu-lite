// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Configuration.Tracking;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Configuration
{
    public class OsuConfigManager : IniConfigManager<OsuSetting>, IGameplaySettings
    {
        public OsuConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            // General
            SetDefault(OsuSetting.LocalPlayerName, @"Player");

            // Song Select
            SetDefault(OsuSetting.DisplayStarsMinimum, 0.0, 0, 10, 0.1);
            SetDefault(OsuSetting.DisplayStarsMaximum, 10.1, 0, 10.1, 0.1);
            SetDefault(OsuSetting.SongSelectGroupMode, GroupMode.None);
            SetDefault(OsuSetting.SongSelectSortingMode, SortMode.Title);
            SetDefault(OsuSetting.RandomSelectAlgorithm, RandomSelectAlgorithm.RandomPermutation);
            SetDefault(OsuSetting.SongSelectBackgroundBlur, false);

            // Audio
            SetDefault(OsuSetting.VolumeInactive, 0.25, 0, 1, 0.01);
            SetDefault(OsuSetting.AudioOffset, 0, -500.0, 500.0, 1);
            SetDefault(OsuSetting.AutomaticallyAdjustBeatmapOffset, false);

            // Input
            SetDefault(OsuSetting.MenuCursorSize, 1.0f, 0.5f, 2f, 0.01f);
            SetDefault(OsuSetting.GameplayCursorSize, 1.0f, 0.1f, 2f, 0.01f);
            SetDefault(OsuSetting.MouseDisableButtons, false);
            SetDefault(OsuSetting.MouseDisableWheel, false);
            SetDefault(OsuSetting.ConfineMouseMode, OsuConfineMouseMode.DuringGameplay);

            // Graphics
            SetDefault(OsuSetting.ShowFpsDisplay, false);
            SetDefault(OsuSetting.ScreenshotFormat, ScreenshotFormat.Jpg);
            SetDefault(OsuSetting.ScreenshotCaptureMenuCursor, false);
            SetDefault(OsuSetting.UIScale, 1f, 0.8f, 1.6f, 0.01f);

            // Gameplay
            SetDefault(OsuSetting.ShowStoryboard, true);
            SetDefault(OsuSetting.BeatmapSkins, true);
            SetDefault(OsuSetting.BeatmapColours, true);
            SetDefault(OsuSetting.BeatmapHitsounds, true);
            SetDefault(OsuSetting.CursorRotation, true);
            SetDefault(OsuSetting.PositionalHitsoundsLevel, 0.2f, 0, 1, 0.01f);
            SetDefault(OsuSetting.DimLevel, 0.7, 0, 1, 0.01);
            SetDefault(OsuSetting.BlurLevel, 0, 0, 1, 0.01);
            SetDefault(OsuSetting.LightenDuringBreaks, true);
            SetDefault(OsuSetting.HitLighting, true);
            SetDefault(OsuSetting.HUDVisibilityMode, HUDVisibilityMode.Always);
            SetDefault(OsuSetting.FadePlayfieldWhenHealthLow, true);
            SetDefault(OsuSetting.KeyOverlay, false);
            SetDefault(OsuSetting.GameplayLeaderboard, true);
            SetDefault(OsuSetting.AlwaysPlayFirstComboBreak, true);
            SetDefault(OsuSetting.GameplayDisableWinKey, true);
            SetDefault(OsuSetting.ComboColourNormalisationAmount, 0.2f, 0f, 1f, 0.01f);
        }

        public override TrackedSettings CreateTrackedSettings()
        {
            return new TrackedSettings
            {
                new TrackedSetting<bool>(OsuSetting.ShowFpsDisplay, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.ToggleFPSCounter,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleFPSDisplay))
                ),
                new TrackedSetting<bool>(OsuSetting.MouseDisableButtons, disabledState => new SettingDescription(
                    rawValue: !disabledState,
                    name: GlobalActionKeyBindingStrings.ToggleGameplayMouseButtons,
                    value: disabledState ? CommonStrings.Disabled.ToLower() : CommonStrings.Enabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleGameplayMouseButtons))
                ),
                new TrackedSetting<bool>(OsuSetting.GameplayLeaderboard, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.ToggleInGameLeaderboard,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleInGameLeaderboard))
                ),
                new TrackedSetting<HUDVisibilityMode>(OsuSetting.HUDVisibilityMode, visibilityMode => new SettingDescription(
                    rawValue: visibilityMode,
                    name: GameplaySettingsStrings.HUDVisibilityMode,
                    value: visibilityMode.GetLocalisableDescription(),
                    shortcut: new TranslatableString(@"_", @"{0}: {1} {2}: {3}",
                        GlobalActionKeyBindingStrings.ToggleInGameInterface,
                        LookupKeyBindings(GlobalAction.ToggleInGameInterface),
                        GlobalActionKeyBindingStrings.HoldForHUD,
                        LookupKeyBindings(GlobalAction.HoldForHUD)))
                ),
            };
        }

        public Func<GlobalAction, LocalisableString> LookupKeyBindings { private get; set; } = _ => @"unknown";

        IBindable<float> IGameplaySettings.ComboColourNormalisationAmount => GetOriginalBindable<float>(OsuSetting.ComboColourNormalisationAmount);
        IBindable<float> IGameplaySettings.PositionalHitsoundsLevel => GetOriginalBindable<float>(OsuSetting.PositionalHitsoundsLevel);
    }

    public enum OsuSetting
    {
        LocalPlayerName,
        MenuCursorSize,
        GameplayCursorSize,
        DimLevel,
        BlurLevel,
        LightenDuringBreaks,
        ShowStoryboard,
        KeyOverlay,
        GameplayLeaderboard,
        PositionalHitsoundsLevel,
        AlwaysPlayFirstComboBreak,
        HUDVisibilityMode,
        FadePlayfieldWhenHealthLow,
        MouseDisableButtons,
        MouseDisableWheel,
        ConfineMouseMode,
        AudioOffset,
        VolumeInactive,
        CursorRotation,
        DisplayStarsMinimum,
        DisplayStarsMaximum,
        SongSelectGroupMode,
        SongSelectSortingMode,
        RandomSelectAlgorithm,
        ShowFpsDisplay,
        SongSelectBackgroundBlur,
        ScreenshotFormat,
        ScreenshotCaptureMenuCursor,
        BeatmapSkins,
        BeatmapColours,
        BeatmapHitsounds,
        UIScale,
        HitLighting,
        GameplayDisableWinKey,
        ComboColourNormalisationAmount,
        AutomaticallyAdjustBeatmapOffset,
    }
}

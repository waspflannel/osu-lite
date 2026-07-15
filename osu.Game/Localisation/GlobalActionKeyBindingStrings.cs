// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

#pragma warning disable OLOC002, OLOC003 // The retained wrapper set follows the existing localisation resource keys.

namespace osu.Game.Localisation
{
    public static class GlobalActionKeyBindingStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";

        /// <summary>"Reset input settings"</summary>
        public static LocalisableString ResetInputSettings => new TranslatableString(getKey(@"reset_input_settings"), @"Reset input settings");
        /// <summary>"Toggle toolbar"</summary>
        public static LocalisableString ToggleToolbar => new TranslatableString(getKey(@"toggle_toolbar"), @"Toggle toolbar");
        /// <summary>"Toggle settings"</summary>
        public static LocalisableString ToggleSettings => new TranslatableString(getKey(@"toggle_settings"), @"Toggle settings");
        /// <summary>"Increase volume"</summary>
        public static LocalisableString IncreaseVolume => new TranslatableString(getKey(@"increase_volume"), @"Increase volume");
        /// <summary>"Decrease volume"</summary>
        public static LocalisableString DecreaseVolume => new TranslatableString(getKey(@"decrease_volume"), @"Decrease volume");
        /// <summary>"Toggle mute"</summary>
        public static LocalisableString ToggleMute => new TranslatableString(getKey(@"toggle_mute"), @"Toggle mute");
        /// <summary>"Skip cutscene"</summary>
        public static LocalisableString SkipCutscene => new TranslatableString(getKey(@"skip_cutscene"), @"Skip cutscene");
        /// <summary>"Quick retry (hold)"</summary>
        public static LocalisableString QuickRetry => new TranslatableString(getKey(@"quick_retry"), @"Quick retry (hold)");
        /// <summary>"Take screenshot"</summary>
        public static LocalisableString TakeScreenshot => new TranslatableString(getKey(@"take_screenshot"), @"Take screenshot");
        /// <summary>"Toggle gameplay mouse buttons"</summary>
        public static LocalisableString ToggleGameplayMouseButtons => new TranslatableString(getKey(@"toggle_gameplay_mouse_buttons"), @"Toggle gameplay mouse buttons");
        /// <summary>"Back"</summary>
        public static LocalisableString Back => new TranslatableString(getKey(@"back"), @"Back");
        /// <summary>"Select"</summary>
        public static LocalisableString Select => new TranslatableString(getKey(@"select"), @"Select");
        /// <summary>"Quick exit (hold)"</summary>
        public static LocalisableString QuickExit => new TranslatableString(getKey(@"quick_exit"), @"Quick exit (hold)");
        /// <summary>"Next track"</summary>
        public static LocalisableString MusicNext => new TranslatableString(getKey(@"music_next"), @"Next track");
        /// <summary>"Previous track"</summary>
        public static LocalisableString MusicPrev => new TranslatableString(getKey(@"music_prev"), @"Previous track");
        /// <summary>"Play / pause"</summary>
        public static LocalisableString MusicPlay => new TranslatableString(getKey(@"music_play"), @"Play / pause");
        /// <summary>"Toggle now playing overlay"</summary>
        public static LocalisableString ToggleNowPlaying => new TranslatableString(getKey(@"toggle_now_playing"), @"Toggle now playing overlay");
        /// <summary>"Previous selection"</summary>
        public static LocalisableString SelectPrevious => new TranslatableString(getKey(@"select_previous"), @"Previous selection");
        /// <summary>"Next selection"</summary>
        public static LocalisableString SelectNext => new TranslatableString(getKey(@"select_next"), @"Next selection");
        /// <summary>"Activate previous set"</summary>
        public static LocalisableString ActivatePreviousSet => new TranslatableString(getKey(@"activate_previous_set"), @"Activate previous set");
        /// <summary>"Activate next set"</summary>
        public static LocalisableString ActivateNextSet => new TranslatableString(getKey(@"activate_next_set"), @"Activate next set");
        /// <summary>"Home"</summary>
        public static LocalisableString Home => new TranslatableString(getKey(@"home"), @"Home");
        /// <summary>"Pause / resume gameplay"</summary>
        public static LocalisableString PauseGameplay => new TranslatableString(getKey(@"pause_gameplay"), @"Pause / resume gameplay");
        /// <summary>"Hold for HUD"</summary>
        public static LocalisableString HoldForHUD => new TranslatableString(getKey(@"hold_for_hud"), @"Hold for HUD");
        /// <summary>"Pause / resume replay"</summary>
        public static LocalisableString TogglePauseReplay => new TranslatableString(getKey(@"toggle_pause_replay"), @"Pause / resume replay");
        /// <summary>"Toggle in-game interface"</summary>
        public static LocalisableString ToggleInGameInterface => new TranslatableString(getKey(@"toggle_in_game_interface"), @"Toggle in-game interface");
        /// <summary>"Random"</summary>
        public static LocalisableString SelectNextRandom => new TranslatableString(getKey(@"select_next_random"), @"Random");
        /// <summary>"Rewind"</summary>
        public static LocalisableString SelectPreviousRandom => new TranslatableString(getKey(@"select_previous_random"), @"Rewind");
        /// <summary>"Beatmap options"</summary>
        public static LocalisableString ToggleBeatmapOptions => new TranslatableString(getKey(@"toggle_beatmap_options"), @"Beatmap options");
        /// <summary>"Previous volume meter"</summary>
        public static LocalisableString PreviousVolumeMeter => new TranslatableString(getKey(@"previous_volume_meter"), @"Previous volume meter");
        /// <summary>"Next volume meter"</summary>
        public static LocalisableString NextVolumeMeter => new TranslatableString(getKey(@"next_volume_meter"), @"Next volume meter");
        /// <summary>"Seek replay forward"</summary>
        public static LocalisableString SeekReplayForward => new TranslatableString(getKey(@"seek_replay_forward"), @"Seek replay forward");
        /// <summary>"Seek replay backward"</summary>
        public static LocalisableString SeekReplayBackward => new TranslatableString(getKey(@"seek_replay_backward"), @"Seek replay backward");
        /// <summary>"Toggle FPS counter"</summary>
        public static LocalisableString ToggleFPSCounter => new TranslatableString(getKey(@"toggle_fps_counter"), @"Toggle FPS counter");
        /// <summary>"Save replay"</summary>
        public static LocalisableString SaveReplay => new TranslatableString(getKey(@"save_replay"), @"Save replay");
        /// <summary>"Export replay"</summary>
        public static LocalisableString ExportReplay => new TranslatableString(getKey(@"export_replay"), @"Export replay");
        /// <summary>"Toggle replay settings"</summary>
        public static LocalisableString ToggleReplaySettings => new TranslatableString(getKey(@"toggle_replay_settings"), @"Toggle replay settings");
        /// <summary>"Toggle in-game leaderboard"</summary>
        public static LocalisableString ToggleInGameLeaderboard => new TranslatableString(getKey(@"toggle_in_game_leaderboard"), @"Toggle in-game leaderboard");
        /// <summary>"Increase offset"</summary>
        public static LocalisableString IncreaseOffset => new TranslatableString(getKey(@"increase_offset"), @"Increase offset");
        /// <summary>"Decrease offset"</summary>
        public static LocalisableString DecreaseOffset => new TranslatableString(getKey(@"decrease_offset"), @"Decrease offset");
        /// <summary>"Step replay forward one frame"</summary>
        public static LocalisableString StepReplayForward => new TranslatableString(getKey(@"step_replay_forward"), @"Step replay forward one frame");
        /// <summary>"Step replay backward one frame"</summary>
        public static LocalisableString StepReplayBackward => new TranslatableString(getKey(@"step_replay_backward"), @"Step replay backward one frame");
        /// <summary>"Absolute scroll song list"</summary>
        public static LocalisableString AbsoluteScrollSongList => new TranslatableString(getKey(@"absolute_scroll_song_list"), @"Absolute scroll song list");
        /// <summary>"Expand previous group"</summary>
        public static LocalisableString ExpandPreviousGroup => new TranslatableString(getKey(@"expand_previous_group"), @"Expand previous group");
        /// <summary>"Expand next group"</summary>
        public static LocalisableString ExpandNextGroup => new TranslatableString(getKey(@"expand_next_group"), @"Expand next group");
        /// <summary>"Toggle expansion of current group"</summary>
        public static LocalisableString ToggleCurrentGroup => new TranslatableString(getKey(@"toggle_current_group"), @"Toggle expansion of current group");
        /// <summary>"Fast forward replay"</summary>
        public static LocalisableString FastForwardReplay => new TranslatableString(getKey(@"fast_forward_replay"), @"Fast forward replay");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}

#pragma warning restore OLOC002, OLOC003

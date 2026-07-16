// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class UserInterfaceStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.UserInterface";

        /// <summary>
        /// "User Interface"
        /// </summary>
        public static LocalisableString UserInterfaceSectionHeader => new TranslatableString(getKey(@"user_interface_section_header"), @"User Interface");

        /// <summary>
        /// "Rotate cursor when dragging"
        /// </summary>
        public static LocalisableString CursorRotation => new TranslatableString(getKey(@"cursor_rotation"), @"Rotate cursor when dragging");

        /// <summary>
        /// "Menu cursor size"
        /// </summary>
        public static LocalisableString MenuCursorSize => new TranslatableString(getKey(@"menu_cursor_size"), @"Menu cursor size");

        /// <summary>
        /// "Parallax"
        /// </summary>
        public static LocalisableString Parallax => new TranslatableString(getKey(@"parallax"), @"Parallax");

        /// <summary>
        /// "Hold-to-confirm activation time"
        /// </summary>
        public static LocalisableString HoldToConfirmActivationTime => new TranslatableString(getKey(@"hold_to_confirm_activation_time"), @"Hold-to-confirm activation time");

        /// <summary>
        /// "Song Select"
        /// </summary>
        public static LocalisableString SongSelectHeader => new TranslatableString(getKey(@"song_select_header"), @"Song Select");

        /// <summary>
        /// "Right mouse drag to absolute scroll"
        /// </summary>
        public static LocalisableString RightMouseScroll => new TranslatableString(getKey(@"right_mouse_scroll"), @"Right mouse drag to absolute scroll");

        /// <summary>
        /// "Display beatmaps from"
        /// </summary>
        public static LocalisableString StarsMinimum => new TranslatableString(getKey(@"stars_minimum"), @"Display beatmaps from");

        /// <summary>
        /// "up to"
        /// </summary>
        public static LocalisableString StarsMaximum => new TranslatableString(getKey(@"stars_maximum"), @"up to");

        /// <summary>
        /// "Random selection algorithm"
        /// </summary>
        public static LocalisableString RandomSelectionAlgorithm => new TranslatableString(getKey(@"random_selection_algorithm"), @"Random selection algorithm");

        /// <summary>
        /// "no limit"
        /// </summary>
        public static LocalisableString NoLimit => new TranslatableString(getKey(@"no_limit"), @"no limit");

        /// <summary>
        /// "Beatmap (with storyboard / video)"
        /// </summary>
        public static LocalisableString BeatmapWithStoryboard => new TranslatableString(getKey(@"beatmap_with_storyboard"), @"Beatmap (with storyboard / video)");

        /// <summary>
        /// "Never repeat"
        /// </summary>
        public static LocalisableString NeverRepeat => new TranslatableString(getKey(@"never_repeat_random"), @"Never repeat");

        /// <summary>
        /// "True random"
        /// </summary>
        public static LocalisableString TrueRandom => new TranslatableString(getKey(@"true_random"), @"True random");

        /// <summary>
        /// "Selected Mods"
        /// </summary>
        public static LocalisableString SelectedMods => new TranslatableString(getKey(@"selected_mods"), @"Selected Mods");

        /// <summary>
        /// "hold for menu"
        /// </summary>
        public static LocalisableString HoldForMenu => new TranslatableString(getKey(@"hold_for_menu"), @"hold for menu");

        /// <summary>
        /// "press for menu"
        /// </summary>
        public static LocalisableString PressForMenu => new TranslatableString(getKey(@"press_for_menu"), @"press for menu");

        /// <summary>
        /// "Device"
        /// </summary>
        public static LocalisableString Device => new TranslatableString(getKey(@"device"), @"Device");

        /// <summary>
        /// "Show hidden"
        /// </summary>
        public static LocalisableString ShowHidden => new TranslatableString(getKey(@"show_hidden"), @"Show hidden");

        /// <summary>
        /// "Currently online"
        /// </summary>
        public static LocalisableString CurrentlyOnline => new TranslatableString(getKey(@"currently_online"), @"Currently online");

        /// <summary>
        /// "User search"
        /// </summary>
        public static LocalisableString UserSearch => new TranslatableString(getKey(@"user_search"), @"User search");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}

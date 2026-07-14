// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class GeneralSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GeneralSettings";

        /// <summary>
        /// "Language"
        /// </summary>
        public static LocalisableString LanguageHeader => new TranslatableString(getKey(@"language_header"), @"Language");

        /// <summary>
        /// "Language"
        /// </summary>
        public static LocalisableString LanguageDropdown => new TranslatableString(getKey(@"language_dropdown"), @"Language");

        /// <summary>
        /// "Prefer metadata in original language"
        /// </summary>
        public static LocalisableString PreferOriginalMetadataLanguage => new TranslatableString(getKey(@"prefer_original"), @"Prefer metadata in original language");

        /// <summary>
        /// "Prefer 24-hour time display"
        /// </summary>
        public static LocalisableString Prefer24HourTimeDisplay => new TranslatableString(getKey(@"prefer_24_hour_time_display"), @"Prefer 24-hour time display");

        /// <summary>
        /// "Installation"
        /// </summary>
        public static LocalisableString InstallationHeader => new TranslatableString(getKey(@"installation_header"), @"Installation");

        /// <summary>
        /// "Quick Actions"
        /// </summary>
        public static LocalisableString QuickActionsHeader => new TranslatableString(getKey(@"quick_actions_header"), @"Quick Actions");

        /// <summary>
        /// "Open osu! folder"
        /// </summary>
        public static LocalisableString OpenOsuFolder => new TranslatableString(getKey(@"open_osu_folder"), @"Open osu! folder");

        /// <summary>
        /// "Export logs"
        /// </summary>
        public static LocalisableString ExportLogs => new TranslatableString(getKey(@"export_logs"), @"Export logs");

        /// <summary>
        /// "Change folder location..."
        /// </summary>
        public static LocalisableString ChangeFolderLocation => new TranslatableString(getKey(@"change_folder_location"), @"Change folder location...");

        /// <summary>
        /// "Run setup wizard"
        /// </summary>
        public static LocalisableString RunSetupWizard => new TranslatableString(getKey(@"run_setup_wizard"), @"Run setup wizard");

        /// <summary>
        /// "Learn more about lazer"
        /// </summary>
        public static LocalisableString LearnMoreAboutLazer => new TranslatableString(getKey(@"learn_more_about_lazer"), @"Learn more about lazer");

        /// <summary>
        /// "Check out the feature comparison and FAQ"
        /// </summary>
        public static LocalisableString LearnMoreAboutLazerTooltip => new TranslatableString(getKey(@"check_out_the_feature_comparison"), @"Check out the feature comparison and FAQ");

        /// <summary>
        /// "Report an issue"
        /// </summary>
        public static LocalisableString ReportIssue => new TranslatableString(getKey(@"report_issue"), @"Report an issue");

        /// <summary>
        /// "Report a problem with the game to the developers."
        /// </summary>
        public static LocalisableString ReportIssueTooltip => new TranslatableString(getKey(@"report_issue_tooltip"), @"Report a problem with the game to the developers.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}

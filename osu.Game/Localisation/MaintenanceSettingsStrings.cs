// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class MaintenanceSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.MaintenanceSettings";

        /// <summary>
        /// "Maintenance"
        /// </summary>
        public static LocalisableString MaintenanceSectionHeader => new TranslatableString(getKey(@"maintenance_section_header"), @"Maintenance");

        /// <summary>
        /// "Select directory"
        /// </summary>
        public static LocalisableString SelectDirectory => new TranslatableString(getKey(@"select_directory"), @"Select directory");

        /// <summary>
        /// "Migration in progress"
        /// </summary>
        public static LocalisableString MigrationInProgress => new TranslatableString(getKey(@"migration_in_progress"), @"Migration in progress");

        /// <summary>
        /// "This could take a few minutes depending on the speed of your disk(s)."
        /// </summary>
        public static LocalisableString MigrationDescription => new TranslatableString(getKey(@"migration_description"), @"This could take a few minutes depending on the speed of your disk(s).");

        /// <summary>
        /// "Please avoid interacting with the game!"
        /// </summary>
        public static LocalisableString ProhibitedInteractDuringMigration => new TranslatableString(getKey(@"prohibited_interact_during_migration"), @"Please avoid interacting with the game!");

        /// <summary>
        /// "Please select a new location"
        /// </summary>
        public static LocalisableString SelectNewLocation => new TranslatableString(getKey(@"select_new_location"), @"Please select a new location");

        /// <summary>
        /// "The target directory already seems to have an osu! install. Use that data instead? osu! will restart."
        /// </summary>
        public static LocalisableString TargetDirectoryAlreadyInstalledOsu => new TranslatableString(getKey(@"target_directory_already_installed_osu"), @"The target directory already seems to have an osu! install. Use that data instead? osu! will restart.");

        /// <summary>
        /// "Delete ALL beatmaps"
        /// </summary>
        public static LocalisableString DeleteAllBeatmaps => new TranslatableString(getKey(@"delete_all_beatmaps"), @"Delete ALL beatmaps");

        /// <summary>
        /// "Delete ALL beatmap videos"
        /// </summary>
        public static LocalisableString DeleteAllBeatmapVideos => new TranslatableString(getKey(@"delete_all_beatmap_videos"), @"Delete ALL beatmap videos");

        /// <summary>
        /// "Reset ALL beatmap offsets"
        /// </summary>
        public static LocalisableString ResetAllOffsets => new TranslatableString(getKey(@"reset_all_offsets"), @"Reset ALL beatmap offsets");

        /// <summary>
        /// "Delete ALL scores"
        /// </summary>
        public static LocalisableString DeleteAllScores => new TranslatableString(getKey(@"delete_all_scores"), @"Delete ALL scores");

        /// <summary>
        /// "Delete ALL skins"
        /// </summary>
        public static LocalisableString DeleteAllSkins => new TranslatableString(getKey(@"delete_all_skins"), @"Delete ALL skins");

        /// <summary>
        /// "Restore all hidden difficulties"
        /// </summary>
        public static LocalisableString RestoreAllHiddenDifficulties => new TranslatableString(getKey(@"restore_all_hidden_difficulties"), @"Restore all hidden difficulties");

        /// <summary>
        /// "Restore all recently deleted beatmaps"
        /// </summary>
        public static LocalisableString RestoreAllRecentlyDeletedBeatmaps => new TranslatableString(getKey(@"restore_all_recently_deleted_beatmaps"), @"Restore all recently deleted beatmaps");

        /// <summary>
        /// "Please select your osu!stable install location"
        /// </summary>
        public static LocalisableString StableDirectorySelectHeader => new TranslatableString(getKey(@"stable_directory_select_header"), @"Please select your osu!stable install location");

        /// <summary>
        /// "All offsets have been reset!"
        /// </summary>
        public static LocalisableString AllOffsetsReset => new TranslatableString(getKey(@"all_offsets_reset"), @"All offsets have been reset!");

        /// <summary>
        /// "No videos found to delete!"
        /// </summary>
        public static LocalisableString NoVideosFoundToDelete => new TranslatableString(getKey(@"no_videos_found_to_delete"), @"No videos found to delete!");

        private static string getKey(string key) => $"{prefix}:{key}";
    }
}

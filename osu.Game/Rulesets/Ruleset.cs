// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Filter;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets
{
    public abstract class Ruleset
    {
        public RulesetInfo RulesetInfo { get; }

        /// <summary>
        /// Version history:
        /// 2022.205.0   FramedReplayInputHandler.CollectPendingInputs renamed to FramedReplayHandler.CollectReplayInputs.
        /// 2022.822.0   All strings return values have been converted to LocalisableString to allow for localisation support.
        /// </summary>
        public const string CURRENT_RULESET_API_VERSION = "2022.822.0";

        /// <summary>
        /// Define the ruleset API version supported by this ruleset.
        /// Ruleset implementations should be updated to support the latest version to ensure they can still be loaded.
        /// </summary>
        /// <remarks>
        /// Generally, all ruleset implementations should point this directly to <see cref="CURRENT_RULESET_API_VERSION"/>.
        /// This will ensure that each time you compile a new release, it will pull in the most recent version.
        /// See https://github.com/ppy/osu/wiki/Breaking-Changes for full details on required ongoing changes.
        /// </remarks>
        public virtual string RulesetAPIVersionSupported => string.Empty;

        /// <summary>
        /// Create a transformer which adds lookups specific to a ruleset to skin sources.
        /// </summary>
        /// <param name="skin">The source skin.</param>
        /// <param name="beatmap">The current beatmap.</param>
        /// <returns>A skin with a transformer applied, or null if no transformation is provided by this ruleset.</returns>
        public virtual ISkin? CreateSkinTransformer(ISkin skin, IBeatmap beatmap) => null;

        protected Ruleset()
        {
            RulesetInfo = new RulesetInfo
            {
                Name = Description,
                ShortName = ShortName,
                OnlineID = (this as ILegacyRuleset)?.LegacyID ?? -1,
            };
        }

        /// <summary>
        /// Attempt to create a hit renderer for a beatmap
        /// </summary>
        /// <param name="beatmap">The beatmap to create the hit renderer for.</param>
        /// <exception cref="BeatmapInvalidForRulesetException">Unable to successfully load the beatmap to be usable with this ruleset.</exception>
        public abstract DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap);

        /// <summary>
        /// Creates a <see cref="ScoreProcessor"/> for this <see cref="Ruleset"/>.
        /// </summary>
        /// <returns>The score processor.</returns>
        public virtual ScoreProcessor CreateScoreProcessor() => new ScoreProcessor(this);

        /// <summary>
        /// Creates an autoplay replay for this ruleset, if supported.
        /// </summary>
        public virtual Score? CreateAutoplayScore(IBeatmap beatmap) => null;

        /// <summary>
        /// Creates a <see cref="HealthProcessor"/> for this <see cref="Ruleset"/>.
        /// </summary>
        /// <returns>The health processor.</returns>
        public virtual HealthProcessor CreateHealthProcessor(double drainStartTime) => new DrainingHealthProcessor(drainStartTime);

        /// <summary>
        /// Creates a <see cref="IBeatmapConverter"/> to convert a <see cref="IBeatmap"/> to one that is applicable for this <see cref="Ruleset"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> to be converted.</param>
        /// <returns>The <see cref="IBeatmapConverter"/>.</returns>
        public abstract IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap);

        /// <summary>
        /// Optionally creates a <see cref="IBeatmapProcessor"/> to alter a <see cref="IBeatmap"/> after it has been converted.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> to be processed.</param>
        /// <returns>The <see cref="IBeatmapProcessor"/>.</returns>
        public virtual IBeatmapProcessor? CreateBeatmapProcessor(IBeatmap beatmap) => null;

        public abstract DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap);

        /// <summary>
        /// Optionally creates a <see cref="PerformanceCalculator"/> to generate performance data from the provided score.
        /// </summary>
        /// <returns>A performance calculator instance for the provided score.</returns>
        public virtual PerformanceCalculator? CreatePerformanceCalculator() => null;

        public virtual Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.QuestionCircle };

        public virtual IResourceStore<byte[]> CreateResourceStore() => new NamespacedResourceStore<byte[]>(new DllResourceStore(GetType().Assembly), @"Resources");

        public abstract string Description { get; }

        public virtual RulesetSettingsSubsection? CreateSettings() => null;

        /// <summary>
        /// Creates the <see cref="IRulesetConfigManager"/> for this <see cref="Ruleset"/>.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsStore"/> to store the settings.</param>
        public virtual IRulesetConfigManager? CreateConfig(SettingsStore? settings) => null;

        /// <summary>
        /// A unique short name to reference this ruleset in online requests.
        /// </summary>
        public abstract string ShortName { get; }

        /// <summary>
        /// A list of available variant ids.
        /// </summary>
        public virtual IEnumerable<int> AvailableVariants => new[] { 0 };

        /// <summary>
        /// Get a list of default keys for the specified variant.
        /// </summary>
        /// <param name="variant">A variant.</param>
        /// <returns>A list of valid <see cref="KeyBinding"/>s.</returns>
        public virtual IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => Array.Empty<KeyBinding>();

        /// <summary>
        /// Text that describes what variants in a ruleset are.
        /// Override this to provide better copy than the generic "Variant" text which may not tell users much.
        /// </summary>
        public virtual LocalisableString VariantDescription => "Variant";

        /// <summary>
        /// Gets the name for a key binding variant. This is used for display in the settings overlay.
        /// </summary>
        /// <param name="variant">The variant.</param>
        /// <returns>A descriptive name of the variant.</returns>
        public virtual LocalisableString GetVariantName(int variant) => string.Empty;

        public virtual int GetVariantForBeatmap(IBeatmapInfo beatmapInfo) => 0;

        /// <summary>
        /// For rulesets which support legacy (osu-stable) replay conversion, this method will create an empty replay frame
        /// for conversion use.
        /// </summary>
        /// <returns>An empty frame for the current ruleset, or null if unsupported.</returns>
        public virtual IConvertibleReplayFrame? CreateConvertibleReplayFrame() => null;

        /// <summary>
        /// Creates the statistics for a <see cref="ScoreInfo"/> to be displayed in the results screen.
        /// </summary>
        /// <param name="score">The <see cref="ScoreInfo"/> to create the statistics for. The score is guaranteed to have <see cref="ScoreInfo.HitEvents"/> populated.</param>
        /// <param name="playableBeatmap">The <see cref="IBeatmap"/> converted for this <see cref="Ruleset"/>.</param>
        /// <returns>The <see cref="StatisticItem"/>s to display.</returns>
        public virtual StatisticItem[] CreateStatisticsForScore(ScoreInfo score, IBeatmap playableBeatmap) => Array.Empty<StatisticItem>();

        /// <summary>
        /// Get all <see cref="HitResult"/>s for this ruleset which are important enough to displayed to the end user.
        /// Used for results display purposes, where it can't be determined if zero-count means the user has not achieved any or the type is not used by this ruleset.
        /// </summary>
        /// <remarks>
        /// <see cref="HitResult.Miss"/> is implicitly included. Special types like <see cref="HitResult.IgnoreHit"/> are not returned by this method.
        /// Values are returned as ordered by <see cref="OrderAttribute"/>.
        /// </remarks>
        /// <returns>
        /// All relevant <see cref="HitResult"/>s along with a display-friendly name.
        /// </returns>
        public IEnumerable<(HitResult result, LocalisableString displayName)> GetHitResultsForDisplay()
        {
            var validResults = GetValidHitResults();

            // enumerate over ordered list to guarantee return order is stable.
            foreach (var result in EnumExtensions.GetValuesInOrder<HitResult>())
            {
                switch (result)
                {
                    // hard blocked types, should never be displayed even if the ruleset tells us to.
                    case HitResult.None:
                    case HitResult.IgnoreHit:
                    case HitResult.IgnoreMiss:
                    case HitResult.ComboBreak:
                    // display is handled as a completion count with corresponding "hit" type.
                    case HitResult.LargeTickMiss:
                    case HitResult.SmallTickMiss:
                        continue;
                }

                if (result == HitResult.Miss || validResults.Contains(result))
                    yield return (result, GetDisplayNameForHitResult(result));
            }
        }

        /// <summary>
        /// Get all valid <see cref="HitResult"/>s for this ruleset.
        /// Used for strict validation purposes. The ruleset should return ALL applicable <see cref="HitResult"/> types here
        /// (except <see cref="HitResult.None"/> and obsolete types).
        /// </summary>
        public virtual IEnumerable<HitResult> GetValidHitResults() => EnumExtensions.GetValuesInOrder<HitResult>();

        /// <summary>
        /// Get a display friendly name for the specified result type.
        /// </summary>
        /// <param name="result">The result type to get the name for.</param>
        /// <returns>The display name.</returns>
        public virtual LocalisableString GetDisplayNameForHitResult(HitResult result) => result.GetLocalisableDescription();

        public virtual IEnumerable<RulesetBeatmapAttribute> GetBeatmapAttributesForDisplay(IBeatmapInfo beatmapInfo)
        {
            var originalDifficulty = beatmapInfo.Difficulty;

            yield return new RulesetBeatmapAttribute(SongSelectStrings.CircleSize, @"CS", originalDifficulty.CircleSize, originalDifficulty.CircleSize, 10);
            yield return new RulesetBeatmapAttribute(SongSelectStrings.ApproachRate, @"AR", originalDifficulty.ApproachRate, originalDifficulty.ApproachRate, 10);
            yield return new RulesetBeatmapAttribute(SongSelectStrings.Accuracy, @"OD", originalDifficulty.OverallDifficulty, originalDifficulty.OverallDifficulty, 10);
            yield return new RulesetBeatmapAttribute(SongSelectStrings.HPDrain, @"HP", originalDifficulty.DrainRate, originalDifficulty.DrainRate, 10);
        }

        /// <summary>
        /// Creates ruleset-specific beatmap filter criteria to be used on the song select screen.
        /// </summary>
        public virtual IRulesetFilterCriteria? CreateRulesetFilterCriteria() => null;
    }
}

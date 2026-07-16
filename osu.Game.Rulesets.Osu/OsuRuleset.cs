// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Osu.Skinning.Legacy;
using osu.Game.Rulesets.Osu.Statistics;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;
using osu.Game.Skinning;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu
{
    public class OsuRuleset : Ruleset, ILegacyRuleset
    {
        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap) => new DrawableOsuRuleset(this, beatmap);

        public override ScoreProcessor CreateScoreProcessor() => new OsuScoreProcessor();

        public override Score CreateAutoplayScore(IBeatmap beatmap) => OsuAutoGenerator.CreateScore(beatmap);

        public override HealthProcessor CreateHealthProcessor(double drainStartTime) => new OsuHealthProcessor(drainStartTime);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) => new OsuBeatmapConverter(beatmap, this);

        public override IBeatmapProcessor CreateBeatmapProcessor(IBeatmap beatmap) => new OsuBeatmapProcessor(beatmap);

        public const string SHORT_NAME = "osu";

        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => new[]
        {
            new KeyBinding(InputKey.Z, OsuAction.LeftButton),
            new KeyBinding(InputKey.X, OsuAction.RightButton),
            new KeyBinding(InputKey.C, OsuAction.Smoke),
            new KeyBinding(InputKey.MouseLeft, OsuAction.LeftButton),
            new KeyBinding(InputKey.MouseRight, OsuAction.RightButton),
        };

        public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.RulesetOsu };

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) => new OsuDifficultyCalculator(RulesetInfo, beatmap);

        public override PerformanceCalculator CreatePerformanceCalculator() => new OsuPerformanceCalculator();

        public override string Description => "osu!";

        public override string ShortName => SHORT_NAME;

        public override RulesetSettingsSubsection CreateSettings() => new OsuSettingsSubsection(this);

        public override ISkin? CreateSkinTransformer(ISkin skin, IBeatmap beatmap)
        {
            switch (skin)
            {
                case LegacySkin:
                    return new OsuLegacySkinTransformer(skin);

            }

            return null;
        }

        public int LegacyID => 0;

        public ILegacyScoreSimulator CreateLegacyScoreSimulator() => new OsuLegacyScoreSimulator();

        public override IConvertibleReplayFrame CreateConvertibleReplayFrame() => new OsuReplayFrame();

        public override IRulesetConfigManager CreateConfig(SettingsStore? settings) => new OsuRulesetConfigManager(settings, RulesetInfo);

        public override IEnumerable<HitResult> GetValidHitResults()
        {
            return new[]
            {
                HitResult.Great,
                HitResult.Ok,
                HitResult.Meh,
                HitResult.Miss,

                HitResult.LargeTickHit,
                HitResult.LargeTickMiss,
                HitResult.SmallTickHit,
                HitResult.SmallTickMiss,
                HitResult.SliderTailHit,
                HitResult.SmallBonus,
                HitResult.LargeBonus,
                HitResult.IgnoreHit,
                HitResult.IgnoreMiss,
            };
        }

        public override LocalisableString GetDisplayNameForHitResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.LargeTickHit:
                    return "slider tick";

                case HitResult.SliderTailHit:
                case HitResult.SmallTickHit:
                    return "slider end";

                case HitResult.SmallBonus:
                    return "spinner spin";

                case HitResult.LargeBonus:
                    return "spinner bonus";
            }

            return base.GetDisplayNameForHitResult(result);
        }

        public override StatisticItem[] CreateStatisticsForScore(ScoreInfo score, IBeatmap playableBeatmap)
        {
            var timedHitEvents = score.HitEvents.Where(e => e.HitObject is HitCircle && !(e.HitObject is SliderTailCircle)).ToList();

            return new[]
            {
                new StatisticItem("Performance Breakdown", () => new PerformanceBreakdownChart(score, playableBeatmap)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }),
                new StatisticItem("Timing Distribution", () => new HitEventTimingDistributionGraph(timedHitEvents)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 250
                }, true),
                new StatisticItem("Accuracy Heatmap", () => new AccuracyHeatmap(score, playableBeatmap)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 250
                }, true),
                new StatisticItem("Statistics", () => new SimpleStatisticTable(2, new SimpleStatisticItem[]
                {
                    new AverageHitError(timedHitEvents),
                    new UnstableRate(timedHitEvents)
                }), true)
            };
        }

        public override IEnumerable<RulesetBeatmapAttribute> GetBeatmapAttributesForDisplay(IBeatmapInfo beatmapInfo)
        {
            var originalDifficulty = beatmapInfo.Difficulty;
            var colours = new OsuColour();

            yield return new RulesetBeatmapAttribute(SongSelectStrings.CircleSize, @"CS", originalDifficulty.CircleSize, originalDifficulty.CircleSize, 10)
            {
                Description = "Affects the size of hit circles and sliders.",
                AdditionalMetrics =
                [
                    new RulesetBeatmapAttribute.AdditionalMetric("Hit circle radius", (OsuHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(originalDifficulty.CircleSize, applyFudge: true)).ToLocalisableString("0.#"))
                ]
            };

            yield return new RulesetBeatmapAttribute(SongSelectStrings.ApproachRate, @"AR", originalDifficulty.ApproachRate, originalDifficulty.ApproachRate, 10)
            {
                Description = "Affects how early objects appear on screen relative to their hit time.",
                AdditionalMetrics =
                [
                    new RulesetBeatmapAttribute.AdditionalMetric("Approach time",
                        LocalisableString.Interpolate($@"{IBeatmapDifficultyInfo.DifficultyRangeInt(originalDifficulty.ApproachRate, OsuHitObject.PREEMPT_RANGE):#,0.##} ms"))
                ]
            };

            var hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(originalDifficulty.OverallDifficulty);
            yield return new RulesetBeatmapAttribute(SongSelectStrings.Accuracy, @"OD", originalDifficulty.OverallDifficulty, originalDifficulty.OverallDifficulty, 10)
            {
                Description = "Affects timing requirements for hit circles and spin speed requirements for spinners.",
                AdditionalMetrics = hitWindows.GetAllAvailableWindows()
                                              .Reverse()
                                              .Select(window => new RulesetBeatmapAttribute.AdditionalMetric(
                                                  $"{window.result.GetDescription().ToUpperInvariant()} hit window",
                                                   LocalisableString.Interpolate($@"±{hitWindows.WindowFor(window.result):0.##} ms"),
                                                  colours.ForHitResult(window.result)
                                              )).Concat([
                                                   new RulesetBeatmapAttribute.AdditionalMetric("RPM required to clear spinners", LocalisableString.Interpolate($@"{IBeatmapDifficultyInfo.DifficultyRange(originalDifficulty.OverallDifficulty, Spinner.CLEAR_RPM_RANGE):N0} RPM")),
                                                   new RulesetBeatmapAttribute.AdditionalMetric("RPM required to get full spinner bonus", LocalisableString.Interpolate($@"{IBeatmapDifficultyInfo.DifficultyRange(originalDifficulty.OverallDifficulty, Spinner.COMPLETE_RPM_RANGE):N0} RPM")),
                                              ]).ToArray()
            };

            yield return new RulesetBeatmapAttribute(SongSelectStrings.HPDrain, @"HP", originalDifficulty.DrainRate, originalDifficulty.DrainRate, 10)
            {
                Description = "Affects the harshness of health drain and the health penalties for missing."
            };
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Utils;
using Realms;

namespace osu.Game.Scoring
{
    /// <summary>
    /// A realm model containing metadata for a single score.
    /// </summary>
    [MapTo("Score")]
    public class ScoreInfo : RealmObject, IHasGuidPrimaryKey, IHasRealmFiles, ISoftDelete, IEquatable<ScoreInfo>
    {
        [PrimaryKey]
        public Guid ID { get; set; }

        /// <summary>
        /// The <see cref="BeatmapInfo"/> this score was made against.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property may be <see langword="null"/> if the score was set on a beatmap (or a version of the beatmap) that is not available locally
        /// e.g. due to online updates, or local modifications to the beatmap.
        /// The property will only link to a <see cref="BeatmapInfo"/> if its <see cref="Beatmaps.BeatmapInfo.Hash"/> matches <see cref="BeatmapHash"/>.
        /// </para>
        /// <para>
        /// Due to the above, whenever setting this, make sure to also set <see cref="BeatmapHash"/> to allow relational consistency when a beatmap is potentially changed.
        /// </para>
        /// </remarks>
        public BeatmapInfo? BeatmapInfo { get; set; }

        /// <summary>
        /// The version of the client this score was set using.
        /// Sourced from <see cref="OsuGameBase.Version"/> at the point of score submission.
        /// </summary>
        public string ClientVersion { get; set; } = string.Empty;

        /// <summary>
        /// The <see cref="osu.Game.Beatmaps.BeatmapInfo.Hash"/> at the point in time when the score was set.
        /// </summary>
        [Indexed]
        public string BeatmapHash { get; set; } = string.Empty;

        public RulesetInfo Ruleset { get; set; } = null!;

        public IList<RealmNamedFileUsage> Files { get; } = null!;

        public string Hash { get; set; } = string.Empty;

        public bool DeletePending { get; set; }

        /// <summary>
        /// The total number of points awarded for the score.
        /// </summary>
        public long TotalScore { get; set; }

        /// <summary>
        /// The version of processing applied to calculate total score as stored in the database.
        /// If this does not match <see cref="LegacyScoreEncoder.LATEST_VERSION"/>,
        /// the total score has not yet been updated to reflect the current scoring values.
        ///
        /// Stores the score format version used when this score was created.
        /// </summary>
        /// <remarks>
        /// This may not match the version stored in the replay files.
        /// </remarks>
        public int TotalScoreVersion { get; set; } = LegacyScoreEncoder.LATEST_VERSION;

        /// <summary>
        /// Used to preserve the total score for legacy scores.
        /// </summary>
        /// <remarks>
        /// Not populated if <see cref="IsLegacyScore"/> is <c>false</c>.
        /// </remarks>
        public long? LegacyTotalScore { get; set; }

        /// <summary>
        /// If background processing of this beatmap failed in some way, this flag will become <c>true</c>.
        /// Should be used to ensure we don't repeatedly attempt to reprocess the same scores each startup even though we already know they will fail.
        /// </summary>
        /// <remarks>
        /// See https://github.com/ppy/osu/issues/24301 for one example of how this can occur (missing beatmap file on disk).
        /// </remarks>
        public bool BackgroundReprocessingFailed { get; set; }

        public int MaxCombo { get; set; }

        public double Accuracy { get; set; }

        public DateTimeOffset Date { get; set; }

        public double? PP { get; set; }

        [MapTo("Statistics")]
        public string StatisticsJson { get; set; } = string.Empty;

        [MapTo("MaximumStatistics")]
        public string MaximumStatisticsJson { get; set; } = string.Empty;

        public IList<int> Pauses { get; } = null!;

        public ScoreInfo(BeatmapInfo? beatmap = null, RulesetInfo? ruleset = null)
        {
            Ruleset = ruleset ?? new RulesetInfo();
            BeatmapInfo = beatmap ?? new BeatmapInfo();
            BeatmapHash = BeatmapInfo.Hash;
            ID = Guid.NewGuid();
        }

        [UsedImplicitly] // Realm
        protected ScoreInfo()
        {
        }

        [Ignored]
        public ScoreRank Rank
        {
            get => (ScoreRank)RankInt;
            set => RankInt = (int)value;
        }

        [MapTo(nameof(Rank))]
        public int RankInt { get; set; }

        #region Properties required to make things work with existing usages

        [Ignored]
        public List<HitEvent> HitEvents { get; set; } = new List<HitEvent>();

        public ScoreInfo DeepClone()
        {
            var clone = (ScoreInfo)this.Detach().MemberwiseClone();

            clone.Statistics = new Dictionary<HitResult, int>(clone.Statistics);
            clone.MaximumStatistics = new Dictionary<HitResult, int>(clone.MaximumStatistics);
            clone.HitEvents = new List<HitEvent>(clone.HitEvents);

            return clone;
        }

        [Ignored]
        public bool Passed { get; set; } = true;

        public int Combo { get; set; }

        /// <summary>
        /// The position of this score, starting at 1.
        /// </summary>
        [Ignored]
        public int? Position { get; set; } // TODO: remove after all calls to `CreateScoreInfo` are gone.

        [Ignored]
        public LocalisableString DisplayAccuracy => Accuracy.FormatAccuracy();

        /// <summary>
        /// Whether this <see cref="ScoreInfo"/> represents a legacy (osu!stable) score.
        /// </summary>
        public bool IsLegacyScore { get; set; }

        private Dictionary<HitResult, int>? statistics;

        [Ignored]
        public Dictionary<HitResult, int> Statistics
        {
            get
            {
                if (statistics != null)
                    return statistics;

                if (!string.IsNullOrEmpty(StatisticsJson))
                    statistics = JsonConvert.DeserializeObject<Dictionary<HitResult, int>>(StatisticsJson);

                return statistics ??= new Dictionary<HitResult, int>();
            }
            set => statistics = value;
        }

        private Dictionary<HitResult, int>? maximumStatistics;

        [Ignored]
        public Dictionary<HitResult, int> MaximumStatistics
        {
            get
            {
                if (maximumStatistics != null)
                    return maximumStatistics;

                if (!string.IsNullOrEmpty(MaximumStatisticsJson))
                    maximumStatistics = JsonConvert.DeserializeObject<Dictionary<HitResult, int>>(MaximumStatisticsJson);

                return maximumStatistics ??= new Dictionary<HitResult, int>();
            }
            set => maximumStatistics = value;
        }

        public IEnumerable<HitResultDisplayStatistic> GetStatisticsForDisplay()
        {
            foreach (var r in Ruleset.CreateInstance().GetHitResultsForDisplay())
            {
                int value = Statistics.GetValueOrDefault(r.result);

                switch (r.result)
                {
                    case HitResult.SmallTickHit:
                    case HitResult.LargeTickHit:
                    case HitResult.SliderTailHit:
                    case HitResult.LargeBonus:
                    case HitResult.SmallBonus:
                        if (MaximumStatistics.TryGetValue(r.result, out int count) && count > 0)
                            yield return new HitResultDisplayStatistic(r.result, value, count, r.displayName);

                        break;

                    case HitResult.SmallTickMiss:
                    case HitResult.LargeTickMiss:
                        break;

                    default:
                        yield return new HitResultDisplayStatistic(r.result, value, null, r.displayName);

                        break;
                }
            }
        }

        #endregion

        public bool Equals(ScoreInfo? other) => other?.ID == ID;

        public override string ToString() => this.GetDisplayTitle();
    }
}

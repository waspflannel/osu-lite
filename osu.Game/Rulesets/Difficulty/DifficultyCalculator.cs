// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Lists;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Difficulty
{
    public abstract class DifficultyCalculator
    {
        /// <summary>
        /// The beatmap for which difficulty will be calculated.
        /// </summary>
        protected IBeatmap Beatmap { get; private set; }

        /// <summary>
        /// The working beatmap for which difficulty will be calculated.
        /// </summary>
        protected readonly IWorkingBeatmap WorkingBeatmap;

        private readonly IRulesetInfo ruleset;

        /// <summary>
        /// A yymmdd version which is used to discern when reprocessing is required.
        /// </summary>
        public virtual int Version => 0;

        protected DifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
        {
            this.ruleset = ruleset;
            WorkingBeatmap = beatmap;
        }

        /// <summary>
        /// Calculates the difficulty of the beatmap with no mods applied.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A structure describing the difficulty of the beatmap.</returns>
        public DifficultyAttributes Calculate(CancellationToken cancellationToken = default)
            => calculate(cancellationToken);

        private DifficultyAttributes calculate(CancellationToken cancellationToken)
        {
            using var timedCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            if (!cancellationToken.CanBeCanceled)
                cancellationToken = timedCancellationSource.Token;

            cancellationToken.ThrowIfCancellationRequested();
            // ReSharper disable once PossiblyMistakenUseOfCancellationToken
            preProcess(cancellationToken);

            var skills = CreateSkills(Beatmap);

            if (!Beatmap.HitObjects.Any())
                return CreateDifficultyAttributes(Beatmap, skills);

            foreach (var hitObject in getDifficultyHitObjects())
            {
                foreach (var skill in skills)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    skill.Process(hitObject);
                }
            }

            return CreateDifficultyAttributes(Beatmap, skills);
        }

        /// <summary>
        /// Calculates the difficulty of the beatmap with no mods applied and returns a set of <see cref="TimedDifficultyAttributes"/> representing the difficulty at every relevant time value in the beatmap.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The set of <see cref="TimedDifficultyAttributes"/>.</returns>
        public List<TimedDifficultyAttributes> CalculateTimed(CancellationToken cancellationToken = default)
            => calculateTimed(cancellationToken);

        private List<TimedDifficultyAttributes> calculateTimed(CancellationToken cancellationToken)
        {
            using var timedCancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            if (!cancellationToken.CanBeCanceled)
                cancellationToken = timedCancellationSource.Token;

            cancellationToken.ThrowIfCancellationRequested();
            // ReSharper disable once PossiblyMistakenUseOfCancellationToken
            preProcess(cancellationToken);

            var attribs = new List<TimedDifficultyAttributes>();

            if (!Beatmap.HitObjects.Any())
                return attribs;

            var skills = CreateSkills(Beatmap);
            var progressiveBeatmap = new ProgressiveCalculationBeatmap(Beatmap);
            var difficultyObjects = getDifficultyHitObjects().ToArray();

            int currentIndex = 0;

            foreach (var obj in Beatmap.HitObjects)
            {
                progressiveBeatmap.HitObjects.Add(obj);

                while (currentIndex < difficultyObjects.Length && difficultyObjects[currentIndex].BaseObject.GetEndTime() <= obj.GetEndTime())
                {
                    foreach (var skill in skills)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        skill.Process(difficultyObjects[currentIndex]);
                    }

                    currentIndex++;
                }

                attribs.Add(new TimedDifficultyAttributes(obj.GetEndTime(), CreateDifficultyAttributes(progressiveBeatmap, skills)));
            }

            return attribs;
        }

        /// <summary>
        /// Retrieves the <see cref="DifficultyHitObject"/>s to calculate against.
        /// </summary>
        private IEnumerable<DifficultyHitObject> getDifficultyHitObjects() => SortObjects(CreateDifficultyHitObjects(Beatmap));

        /// <summary>
        /// Performs required tasks before every calculation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void preProcess(CancellationToken cancellationToken)
        {
            Beatmap = WorkingBeatmap.GetPlayableBeatmap(ruleset, cancellationToken);
        }

        /// <summary>
        /// Sorts a given set of <see cref="DifficultyHitObject"/>s.
        /// </summary>
        /// <param name="input">The <see cref="DifficultyHitObject"/>s to sort.</param>
        /// <returns>The sorted <see cref="DifficultyHitObject"/>s.</returns>
        protected virtual IEnumerable<DifficultyHitObject> SortObjects(IEnumerable<DifficultyHitObject> input)
            => input.OrderBy(h => h.BaseObject.StartTime);

        /// <summary>
        /// Creates <see cref="DifficultyAttributes"/> to describe beatmap's calculated difficulty.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> whose difficulty was calculated.
        /// This may differ from <see cref="Beatmap"/> in the case of timed calculation.</param>
        /// <param name="skills">The skills which processed the beatmap.</param>
        protected abstract DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Skill[] skills);

        /// <summary>
        /// Enumerates <see cref="DifficultyHitObject"/>s to be processed from <see cref="HitObject"/>s in the <see cref="IBeatmap"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> providing the <see cref="HitObject"/>s to enumerate.</param>
        /// <returns>The enumerated <see cref="DifficultyHitObject"/>s.</returns>
        protected abstract IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap);

        /// <summary>
        /// Creates the <see cref="Skill"/>s to calculate the difficulty of an <see cref="IBeatmap"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> whose difficulty will be calculated.
        /// This may differ from <see cref="Beatmap"/> in the case of timed calculation.</param>
        /// <returns>The <see cref="Skill"/>s.</returns>
        protected abstract Skill[] CreateSkills(IBeatmap beatmap);

        /// <summary>
        /// Used to calculate timed difficulty attributes, where only a subset of hitobjects should be visible at any point in time.
        /// </summary>
        private class ProgressiveCalculationBeatmap : IBeatmap
        {
            private readonly IBeatmap baseBeatmap;

            public ProgressiveCalculationBeatmap(IBeatmap baseBeatmap)
            {
                this.baseBeatmap = baseBeatmap;
            }

            public readonly List<HitObject> HitObjects = new List<HitObject>();

            IReadOnlyList<HitObject> IBeatmap.HitObjects => HitObjects;

            #region Delegated IBeatmap implementation

            public BeatmapInfo BeatmapInfo
            {
                get => baseBeatmap.BeatmapInfo;
                set => baseBeatmap.BeatmapInfo = value;
            }

            public ControlPointInfo ControlPointInfo
            {
                get => baseBeatmap.ControlPointInfo;
                set => baseBeatmap.ControlPointInfo = value;
            }

            public BeatmapMetadata Metadata => baseBeatmap.Metadata;

            public BeatmapDifficulty Difficulty
            {
                get => baseBeatmap.Difficulty;
                set => baseBeatmap.Difficulty = value;
            }

            public SortedList<BreakPeriod> Breaks
            {
                get => baseBeatmap.Breaks;
                set => baseBeatmap.Breaks = value;
            }

            public double TotalBreakTime => baseBeatmap.TotalBreakTime;
            public IEnumerable<BeatmapStatistic> GetStatistics() => baseBeatmap.GetStatistics();
            public double GetMostCommonBeatLength() => baseBeatmap.GetMostCommonBeatLength();
            public int BeatmapVersion => baseBeatmap.BeatmapVersion;
            public IBeatmap Clone() => new ProgressiveCalculationBeatmap(baseBeatmap.Clone());

            public double AudioLeadIn
            {
                get => baseBeatmap.AudioLeadIn;
                set => baseBeatmap.AudioLeadIn = value;
            }

            public float StackLeniency
            {
                get => baseBeatmap.StackLeniency;
                set => baseBeatmap.StackLeniency = value;
            }

            public bool SpecialStyle
            {
                get => baseBeatmap.SpecialStyle;
                set => baseBeatmap.SpecialStyle = value;
            }

            public bool LetterboxInBreaks
            {
                get => baseBeatmap.LetterboxInBreaks;
                set => baseBeatmap.LetterboxInBreaks = value;
            }

            public bool WidescreenStoryboard
            {
                get => baseBeatmap.WidescreenStoryboard;
                set => baseBeatmap.WidescreenStoryboard = value;
            }

            public bool EpilepsyWarning
            {
                get => baseBeatmap.EpilepsyWarning;
                set => baseBeatmap.EpilepsyWarning = value;
            }

            public bool SamplesMatchPlaybackRate
            {
                get => baseBeatmap.SamplesMatchPlaybackRate;
                set => baseBeatmap.SamplesMatchPlaybackRate = value;
            }

            public double DistanceSpacing
            {
                get => baseBeatmap.DistanceSpacing;
                set => baseBeatmap.DistanceSpacing = value;
            }

            public int GridSize
            {
                get => baseBeatmap.GridSize;
                set => baseBeatmap.GridSize = value;
            }

            public double TimelineZoom
            {
                get => baseBeatmap.TimelineZoom;
                set => baseBeatmap.TimelineZoom = value;
            }

            public CountdownType Countdown
            {
                get => baseBeatmap.Countdown;
                set => baseBeatmap.Countdown = value;
            }

            public int CountdownOffset
            {
                get => baseBeatmap.CountdownOffset;
                set => baseBeatmap.CountdownOffset = value;
            }

            public int[] Bookmarks
            {
                get => baseBeatmap.Bookmarks;
                set => baseBeatmap.Bookmarks = value;
            }

            public double[] SliderVelocityPresets
            {
                get => baseBeatmap.SliderVelocityPresets;
                set => baseBeatmap.SliderVelocityPresets = value;
            }

            #endregion
        }
    }
}

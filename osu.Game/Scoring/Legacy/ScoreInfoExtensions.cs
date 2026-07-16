// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring.Legacy
{
    public static class ScoreInfoExtensions
    {
        public static long GetDisplayScore(this ScoreProcessor scoreProcessor, ScoringMode mode)
            => getDisplayScore(scoreProcessor.TotalScore.Value, mode, scoreProcessor.MaximumStatistics);

        public static long GetDisplayScore(this ScoreInfo scoreInfo, ScoringMode mode)
            => getDisplayScore(scoreInfo.TotalScore, mode, scoreInfo.MaximumStatistics);

        private static long getDisplayScore(long score, ScoringMode mode, IReadOnlyDictionary<HitResult, int> maximumStatistics)
        {
            if (mode == ScoringMode.Standardised)
                return score;

            int maxBasicJudgements = maximumStatistics
                                      .Where(k => k.Key.IsBasic())
                                      .Select(k => k.Value)
                                      .DefaultIfEmpty(0)
                                      .Sum();

            return convertStandardisedToClassic(score, maxBasicJudgements);
        }

        private static long convertStandardisedToClassic(long standardisedTotalScore, int objectCount)
            => (long)Math.Round((Math.Pow(objectCount, 2) * 32.57 + 100000) * standardisedTotalScore / ScoreProcessor.MAX_SCORE);

        public static int? GetCountGeki(this ScoreInfo scoreInfo) => null;

        public static void SetCountGeki(this ScoreInfo scoreInfo, int value)
        {
        }

        public static int? GetCount300(this ScoreInfo scoreInfo) => getCount(scoreInfo, HitResult.Great);

        public static void SetCount300(this ScoreInfo scoreInfo, int value) => scoreInfo.Statistics[HitResult.Great] = value;

        public static int? GetCountKatu(this ScoreInfo scoreInfo) => null;

        public static void SetCountKatu(this ScoreInfo scoreInfo, int value)
        {
        }

        public static int? GetCount100(this ScoreInfo scoreInfo)
            => getCount(scoreInfo, HitResult.Ok);

        public static void SetCount100(this ScoreInfo scoreInfo, int value)
            => scoreInfo.Statistics[HitResult.Ok] = value;

        public static int? GetCount50(this ScoreInfo scoreInfo)
            => getCount(scoreInfo, HitResult.Meh);

        public static void SetCount50(this ScoreInfo scoreInfo, int value)
            => scoreInfo.Statistics[HitResult.Meh] = value;

        public static int? GetCountMiss(this ScoreInfo scoreInfo)
            => getCount(scoreInfo, HitResult.Miss);

        public static void SetCountMiss(this ScoreInfo scoreInfo, int value) =>
            scoreInfo.Statistics[HitResult.Miss] = value;

        private static int? getCount(ScoreInfo scoreInfo, HitResult result)
        {
            if (scoreInfo.Statistics.TryGetValue(result, out int existing))
                return existing;

            return null;
        }
    }
}

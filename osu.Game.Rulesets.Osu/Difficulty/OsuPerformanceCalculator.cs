// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuPerformanceCalculator : PerformanceCalculator
    {
        public const double PERFORMANCE_BASE_MULTIPLIER = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things.
        public const double PERFORMANCE_NORM_EXPONENT = 1.1;

        private double accuracy;
        private int scoreMaxCombo;
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;

        /// <summary>
        /// Missed slider ticks that includes missed reverse arrows. Will only be correct on non-classic scores
        /// </summary>
        private int countSliderTickMiss;

        /// <summary>
        /// Amount of missed slider tails that don't break combo. Will only be correct on non-classic scores
        /// </summary>
        private int countSliderEndsDropped;

        /// <summary>
        /// Estimated total amount of combo breaks
        /// </summary>
        private double effectiveMissCount;

        private double clockRate;
        private double greatHitWindow;
        private double okHitWindow;
        private double mehHitWindow;

        private double overallDifficulty;
        private double approachRate;
        private double drainRate;

        private double? speedDeviation;

        private double aimEstimatedSliderBreaks;
        private double speedEstimatedSliderBreaks;

        public static double DifficultyToPerformance(double difficulty) => 4.0 * DiffUtils.Pow(difficulty, 3);

        public OsuPerformanceCalculator()
            : base(new OsuRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var osuAttributes = (OsuDifficultyAttributes)attributes;

            accuracy = score.Accuracy;
            scoreMaxCombo = score.MaxCombo;
            countGreat = score.Statistics.GetValueOrDefault(HitResult.Great);
            countOk = score.Statistics.GetValueOrDefault(HitResult.Ok);
            countMeh = score.Statistics.GetValueOrDefault(HitResult.Meh);
            countMiss = score.Statistics.GetValueOrDefault(HitResult.Miss);
            countSliderEndsDropped = osuAttributes.SliderCount - score.Statistics.GetValueOrDefault(HitResult.SliderTailHit);
            countSliderTickMiss = score.Statistics.GetValueOrDefault(HitResult.LargeTickMiss);
            effectiveMissCount = countMiss;

            var difficulty = score.BeatmapInfo!.Difficulty.Clone();

            clockRate = 1;

            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(difficulty.OverallDifficulty);

            greatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate;
            okHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate;
            mehHitWindow = hitWindows.WindowFor(HitResult.Meh) / clockRate;

            approachRate = calculateRateAdjustedApproachRate(difficulty.ApproachRate, clockRate);
            overallDifficulty = (79.5 - greatHitWindow) / 6;
            drainRate = difficulty.DrainRate;

            double comboBasedEstimatedMissCount = calculateComboBasedEstimatedMissCount(osuAttributes);
            double? scoreBasedEstimatedMissCount = null;
            effectiveMissCount = comboBasedEstimatedMissCount;

            effectiveMissCount = Math.Max(countMiss, effectiveMissCount);
            effectiveMissCount = Math.Min(totalHits, effectiveMissCount);

            if (effectiveMissCount > 0)
            {
                aimEstimatedSliderBreaks = calculateEstimatedSliderBreaks(osuAttributes.AimTopWeightedSliderFactor, osuAttributes);
                speedEstimatedSliderBreaks = calculateEstimatedSliderBreaks(osuAttributes.SpeedTopWeightedSliderFactor, osuAttributes);
            }

            double multiplier = PERFORMANCE_BASE_MULTIPLIER;

            speedDeviation = calculateSpeedDeviation(osuAttributes);

            double aimValue = computeAimValue(score, osuAttributes);
            double speedValue = computeSpeedValue(score, osuAttributes);
            double accuracyValue = computeAccuracyValue(score, osuAttributes);

            double readingValue = computeReadingValue(osuAttributes);
            double flashlightValue = computeFlashlightValue(score, osuAttributes);
            double cognitionValue = OsuDifficultyCalculator.SumCognitionDifficulty(readingValue, flashlightValue);

            double totalValue = DiffUtils.Norm(PERFORMANCE_NORM_EXPONENT, aimValue, speedValue, accuracyValue, cognitionValue) * multiplier;

            return new OsuPerformanceAttributes
            {
                Aim = aimValue,
                Speed = speedValue,
                Accuracy = accuracyValue,
                Flashlight = flashlightValue,
                Reading = readingValue,
                EffectiveMissCount = effectiveMissCount,
                ComboBasedEstimatedMissCount = comboBasedEstimatedMissCount,
                ScoreBasedEstimatedMissCount = scoreBasedEstimatedMissCount,
                AimEstimatedSliderBreaks = aimEstimatedSliderBreaks,
                SpeedEstimatedSliderBreaks = speedEstimatedSliderBreaks,
                SpeedDeviation = speedDeviation,
                Total = totalValue
            };
        }

        private double computeAimValue(ScoreInfo score, OsuDifficultyAttributes attributes)
        {
            double aimDifficulty = attributes.AimDifficulty;

            if (attributes.SliderCount > 0 && attributes.AimDifficultSliderCount > 0)
            {
                double estimateImproperlyFollowedDifficultSliders;

                estimateImproperlyFollowedDifficultSliders = Math.Clamp(countSliderEndsDropped + countSliderTickMiss, 0, attributes.AimDifficultSliderCount);

                double sliderNerfFactor = (1 - attributes.SliderFactor) * DiffUtils.Pow(1 - estimateImproperlyFollowedDifficultSliders / attributes.AimDifficultSliderCount, 3) + attributes.SliderFactor;
                aimDifficulty *= sliderNerfFactor;
            }

            double aimValue = DifficultyToPerformance(aimDifficulty);

            double lengthBonus = 0.95 + 0.35 * Math.Min(1.0, totalHits / 2000.0) +
                                 (totalHits > 2000 ? Math.Log10(totalHits / 2000.0) * 0.5 : 0.0);
            aimValue *= lengthBonus;

            if (effectiveMissCount > 0)
            {
                double relevantMissCount = Math.Min(effectiveMissCount + aimEstimatedSliderBreaks, totalImperfectHits + countSliderTickMiss);

                aimValue *= calculateMissPenalty(relevantMissCount, attributes.AimDifficultStrainCount);
            }

            aimValue *= accuracy;

            return aimValue;
        }

        private double computeSpeedValue(ScoreInfo score, OsuDifficultyAttributes attributes)
        {
            if (speedDeviation == null)
                return 0.0;

            double speedValue = HarmonicSkill.DifficultyToPerformance(attributes.SpeedDifficulty);

            if (effectiveMissCount > 0)
            {
                double relevantMissCount = Math.Min(effectiveMissCount + speedEstimatedSliderBreaks, totalImperfectHits + countSliderTickMiss);

                speedValue *= calculateMissPenalty(relevantMissCount, attributes.SpeedDifficultStrainCount);
            }

            double speedHighDeviationMultiplier = calculateSpeedHighDeviationNerf(attributes);
            speedValue *= speedHighDeviationMultiplier;

            // An effective hit window is created based on the speed SR. The higher the speed difficulty, the shorter the hit window.
            // For example, a speed SR of 4.0 leads to an effective hit window of 20ms, which is OD 10.
            double effectiveHitWindow = 20 * DiffUtils.Pow(4 / attributes.SpeedDifficulty, 0.35);

            // Find the proportion of 300s on speed notes assuming the hit window was the effective hit window.
            double effectiveAccuracy = DiffUtils.Erf(effectiveHitWindow / (double)speedDeviation);

            // Scale speed value by normalized accuracy.
            speedValue *= DiffUtils.Pow(effectiveAccuracy, 2);

            return speedValue;
        }

        private double computeAccuracyValue(ScoreInfo score, OsuDifficultyAttributes attributes)
        {
            // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window.
            double betterAccuracyPercentage;
            int amountHitObjectsWithAccuracy = attributes.HitCircleCount;
            amountHitObjectsWithAccuracy += attributes.SliderCount;

            if (amountHitObjectsWithAccuracy > 0)
                betterAccuracyPercentage = ((countGreat - Math.Max(totalHits - amountHitObjectsWithAccuracy, 0)) * 6 + countOk * 2 + countMeh) / (double)(amountHitObjectsWithAccuracy * 6);
            else
                betterAccuracyPercentage = 0;

            // It is possible to reach a negative accuracy with this formula. Cap it at zero - zero points.
            if (betterAccuracyPercentage < 0)
                betterAccuracyPercentage = 0;

            // Lots of arbitrary values from testing.
            // Considering to use derivation from perfect accuracy in a probabilistic manner - assume normal distribution.
            double accuracyValue = DiffUtils.Pow(1.52163, overallDifficulty) * DiffUtils.Pow(betterAccuracyPercentage, 24) * 2.83;

            // Bonus for many hitcircles - it's harder to keep good accuracy up for longer.
            accuracyValue *= amountHitObjectsWithAccuracy < 1000
                ? DiffUtils.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.3)
                : DiffUtils.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.1);

            return accuracyValue;
        }

        private double computeFlashlightValue(ScoreInfo score, OsuDifficultyAttributes attributes) => 0;

        private double computeReadingValue(OsuDifficultyAttributes attributes)
        {
            double readingValue = HarmonicSkill.DifficultyToPerformance(attributes.ReadingDifficulty);

            if (effectiveMissCount > 0)
                readingValue *= calculateMissPenalty(effectiveMissCount + aimEstimatedSliderBreaks, attributes.ReadingDifficultNoteCount);

            // Scale the reading value with accuracy _harshly_.
            readingValue *= DiffUtils.Pow(accuracy, 3);

            return readingValue;
        }

        private double calculateComboBasedEstimatedMissCount(OsuDifficultyAttributes attributes)
        {
            if (attributes.SliderCount <= 0)
                return countMiss;

            double missCount = countMiss;

            double fullComboThreshold = attributes.MaxCombo - countSliderEndsDropped;

            if (scoreMaxCombo < fullComboThreshold)
                missCount = fullComboThreshold / Math.Max(1.0, scoreMaxCombo);

            missCount = Math.Min(missCount, countSliderTickMiss + countMiss);

            return missCount;
        }

        private double calculateEstimatedSliderBreaks(double topWeightedSliderFactor, OsuDifficultyAttributes attributes)
        {
            return 0;
        }

        /// <summary>
        /// Estimates player's deviation on speed notes using <see cref="calculateDeviation"/>, assuming worst-case.
        /// Treats all speed notes as hit circles.
        /// </summary>
        private double? calculateSpeedDeviation(OsuDifficultyAttributes attributes)
        {
            if (totalSuccessfulHits == 0)
                return null;

            // Calculate accuracy assuming the worst case scenario
            double speedNoteCount = attributes.SpeedNoteCount;
            speedNoteCount += (totalHits - attributes.SpeedNoteCount) * 0.1;

            // Assume worst case: all mistakes were on speed notes
            double relevantCountMiss = Math.Min(countMiss, speedNoteCount);
            double relevantCountMeh = Math.Min(countMeh, speedNoteCount - relevantCountMiss);
            double relevantCountOk = Math.Min(countOk, speedNoteCount - relevantCountMiss - relevantCountMeh);
            double relevantCountGreat = Math.Max(0, speedNoteCount - relevantCountMiss - relevantCountMeh - relevantCountOk);

            return calculateDeviation(relevantCountGreat, relevantCountOk, relevantCountMeh);
        }

        /// <summary>
        /// Estimates the player's tap deviation based on the OD, given number of greats, oks, mehs and misses,
        /// assuming the player's mean hit error is 0. The estimation is consistent in that two SS scores on the same map with the same settings
        /// will always return the same deviation. Misses are ignored because they are usually due to misaiming.
        /// Greats and oks are assumed to follow a normal distribution, whereas mehs are assumed to follow a uniform distribution.
        /// </summary>
        private double? calculateDeviation(double relevantCountGreat, double relevantCountOk, double relevantCountMeh)
        {
            if (relevantCountGreat + relevantCountOk + relevantCountMeh <= 0)
                return null;

            // The sample proportion of successful hits.
            double n = Math.Max(1, relevantCountGreat + relevantCountOk);
            double p = relevantCountGreat / n;

            // 99% critical value for the normal distribution (one-tailed).
            const double z = 2.32634787404;

            // We can be 99% confident that the population proportion is at least this value.
            double pLowerBound = Math.Min(p, (n * p + z * z / 2) / (n + z * z) - z / (n + z * z) * Math.Sqrt(n * p * (1 - p) + z * z / 4));

            double deviation;

            // Tested max precision for the deviation calculation.
            if (pLowerBound > 0.01)
            {
                // Compute deviation assuming greats and oks are normally distributed.
                deviation = greatHitWindow / (DiffUtils.SQRT2 * DiffUtils.ErfInv(pLowerBound));

                // Subtract the deviation provided by tails that land outside the ok hit window from the deviation computed above.
                // This is equivalent to calculating the deviation of a normal distribution truncated at +-okHitWindow.
                double okHitWindowTailAmount = Math.Sqrt(2 / Math.PI) * okHitWindow * Math.Exp(-0.5 * DiffUtils.Pow(okHitWindow / deviation, 2))
                                               / (deviation * DiffUtils.Erf(okHitWindow / (DiffUtils.SQRT2 * deviation)));

                deviation *= Math.Sqrt(1 - okHitWindowTailAmount);
            }
            else
            {
                // A tested limit value for the case of a score only containing oks.
                deviation = okHitWindow / Math.Sqrt(3);
            }

            // Compute and add the variance for mehs, assuming that they are uniformly distributed.
            double mehVariance = (mehHitWindow * mehHitWindow + okHitWindow * mehHitWindow + okHitWindow * okHitWindow) / 3;

            deviation = Math.Sqrt(((relevantCountGreat + relevantCountOk) * DiffUtils.Pow(deviation, 2) + relevantCountMeh * mehVariance) / (relevantCountGreat + relevantCountOk + relevantCountMeh));

            return deviation;
        }

        // Calculates multiplier for speed to account for improper tapping based on the deviation and speed difficulty
        // https://www.desmos.com/calculator/dmogdhzofn
        private double calculateSpeedHighDeviationNerf(OsuDifficultyAttributes attributes)
        {
            if (speedDeviation == null)
                return 0;

            double speedValue = HarmonicSkill.DifficultyToPerformance(attributes.SpeedDifficulty);

            // Decides a point where the PP value achieved compared to the speed deviation is assumed to be tapped improperly. Any PP above this point is considered "excess" speed difficulty.
            // This is used to cause PP above the cutoff to scale logarithmically towards the original speed value thus nerfing the value.
            double excessSpeedDifficultyCutoff = 100 + 220 * DiffUtils.Pow(22 / speedDeviation.Value, 6.5);

            if (speedValue <= excessSpeedDifficultyCutoff)
                return 1.0;

            const double scale = 50;
            double adjustedSpeedValue = scale * (Math.Log((speedValue - excessSpeedDifficultyCutoff) / scale + 1) + excessSpeedDifficultyCutoff / scale);

            // 220 UR and less are considered tapped correctly to ensure that normal scores will be punished as little as possible
            double lerp = 1 - DiffUtils.ReverseLerp(speedDeviation.Value, 22.0, 27.0);
            adjustedSpeedValue = double.Lerp(adjustedSpeedValue, speedValue, lerp);

            return adjustedSpeedValue / speedValue;
        }

        /// <summary>
        /// Calculates a visibility bonus that is applicable to Traceable.
        /// </summary>
        private double calculateTraceableBonus(double sliderFactor = 1)
        {
            // We want to reward slider aim less, more so at lower AR
            double highApproachRateSliderVisibilityFactor = 0.5 + (DiffUtils.Pow(sliderFactor, 6) / 2);
            double lowApproachRateSliderVisibilityFactor = DiffUtils.Pow(sliderFactor, 6);

            // Start from normal curve, rewarding lower AR up to AR7
            double traceableBonus = 0.0275;
            traceableBonus += 0.025 * (12.0 - Math.Max(approachRate, 7)) * highApproachRateSliderVisibilityFactor;

            // For AR up to 0 - reduce reward for very low ARs when object is visible
            if (approachRate < 7)
                traceableBonus += 0.025 * (7.0 - Math.Max(approachRate, 0)) * lowApproachRateSliderVisibilityFactor;

            // Starting from AR0 - cap values so they won't grow to infinity
            if (approachRate < 0)
                traceableBonus += 0.025 * (1 - DiffUtils.Pow(1.5, approachRate)) * lowApproachRateSliderVisibilityFactor;

            return traceableBonus;
        }

        // Miss penalty assumes that a player will miss on the hardest parts of a map,
        // so we use the amount of relatively difficult sections to adjust miss penalty
        // to make it more punishing on maps with lower amount of hard sections.
        private double calculateMissPenalty(double missCount, double difficultStrainCount) => 0.93 / (missCount / (4 * Math.Log(Math.Max(1, difficultStrainCount))) + 1);
        private double getComboScalingFactor(OsuDifficultyAttributes attributes) => attributes.MaxCombo <= 0 ? 1.0 : Math.Min(DiffUtils.Pow(scoreMaxCombo, 0.8) / DiffUtils.Pow(attributes.MaxCombo, 0.8), 1.0);

        private double calculateRateAdjustedApproachRate(double approachRate, double clockRate)
        {
            double preempt = IBeatmapDifficultyInfo.DifficultyRange(approachRate, OsuHitObject.PREEMPT_MAX, OsuHitObject.PREEMPT_MID, OsuHitObject.PREEMPT_MIN) / clockRate;
            return IBeatmapDifficultyInfo.InverseDifficultyRange(preempt, OsuHitObject.PREEMPT_MAX, OsuHitObject.PREEMPT_MID, OsuHitObject.PREEMPT_MIN);
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;
        private int totalSuccessfulHits => countGreat + countOk + countMeh;
        private int totalImperfectHits => countOk + countMeh + countMiss;
    }
}

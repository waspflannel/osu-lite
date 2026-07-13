// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Rulesets;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Recommends a suitable difficulty for the local user from a beatmap set.
    /// osu! lite is offline and has no user performance data, so no recommendation
    /// is made and callers fall back to their own default difficulty selection.
    /// </summary>
    public partial class DifficultyRecommender : Component
    {
        public double? GetRecommendedStarRatingFor(RulesetInfo ruleset) => null;

        /// <summary>
        /// Find the recommended difficulty from a selection of available difficulties for the current local user.
        /// </summary>
        /// <param name="beatmaps">A collection of beatmaps to select a difficulty from.</param>
        /// <returns>The recommended difficulty, or null if a recommendation could not be provided.</returns>
        public BeatmapInfo? GetRecommendedBeatmap(IEnumerable<BeatmapInfo> beatmaps) => null;
    }
}

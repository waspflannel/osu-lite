// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Mods
{
    /// <summary>
    /// A mod which creates full replay data, which is to be played back in place of a local user playing the game.
    /// </summary>
    public interface ICreateReplayData
    {
        /// <summary>
        /// Create replay data.
        /// </summary>
        /// <param name="beatmap">The beatmap to create replay data for.</param>
        /// <param name="mods">The mods to take into account when creating the replay data.</param>
        /// <returns>A <see cref="ModReplayData"/> structure, containing the generated replay data.</returns>
        /// <remarks>
        /// For callers that want to receive a directly usable <see cref="Score"/> instance,
        /// the <see cref="ModExtensions.CreateScoreFromReplayData"/> extension method is provided for convenience.
        /// </remarks>
        ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods);
    }

    /// <summary>
    /// Data created by a mod that implements <see cref="ICreateReplayData"/>.
    /// </summary>
    public class ModReplayData
    {
        /// <summary>
        /// The full replay data.
        /// </summary>
        public readonly Replay Replay;

        public ModReplayData(Replay replay)
        {
            Replay = replay;
        }
    }
}

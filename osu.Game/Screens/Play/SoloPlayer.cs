// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.Play
{
    public partial class SoloPlayer : Player
    {
        public SoloPlayer([CanBeNull] PlayerConfiguration configuration = null)
            : base(configuration)
        {
        }

        protected override ResultsScreen CreateResults(ScoreInfo score) => new SoloResultsScreen(score);
    }
}

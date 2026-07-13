// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Database;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.Screens.Ranking
{
    public partial class SoloResultsScreen : ResultsScreen
    {
        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        public SoloResultsScreen(ScoreInfo score)
            : base(score)
        {
        }

        protected override Task<ScoreInfo[]> FetchScores()
        {
            Debug.Assert(Score != null);

            // osu! lite is offline: present other local scores set on the same beatmap.
            ScoreInfo[] scores = realm.Run(r =>
            {
                var localScores = r.All<ScoreInfo>()
                                   .Filter($"{nameof(ScoreInfo.BeatmapHash)} == $0 && {nameof(ScoreInfo.DeletePending)} == false", Score.BeatmapHash)
                                   .AsEnumerable()
                                   .Where(s => !s.Equals(Score))
                                   .Select(s => s.Detach());

                return localScores.OrderByTotalScore().ToArray();
            });

            List<ScoreInfo> sortedScores = scores.ToList();

            for (int i = 0; i < sortedScores.Count; i++)
                sortedScores[i].Position = i + 1;

            return Task.FromResult(sortedScores.ToArray());
        }
    }
}

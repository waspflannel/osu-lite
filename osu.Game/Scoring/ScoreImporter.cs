// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.IO.Archives;
using osu.Game.Rulesets;
using osu.Game.Scoring.Legacy;
using osu.Game.Utils;
using Realms;

namespace osu.Game.Scoring
{
    public class ScoreImporter : RealmArchiveModelImporter<ScoreInfo>
    {
        public override IEnumerable<string> HandledExtensions => new[] { ".osr" };

        protected override string[] HashableFileTypes => new[] { ".osr" };

        private readonly RulesetStore rulesets;
        private readonly Func<BeatmapManager> beatmaps;

        public ScoreImporter(RulesetStore rulesets, Func<BeatmapManager> beatmaps, Storage storage, RealmAccess realm)
            : base(storage, realm)
        {
            this.rulesets = rulesets;
            this.beatmaps = beatmaps;
        }

        protected override ScoreInfo? CreateModel(ArchiveReader archive, ImportParameters parameters)
        {
            string name = archive.Filenames.First(f => f.EndsWith(".osr", StringComparison.OrdinalIgnoreCase));

            using (var stream = archive.GetStream(name))
            {
                try
                {
                    return new DatabasedLegacyScoreDecoder(rulesets, beatmaps()).Parse(stream).ScoreInfo;
                }
                catch (LegacyScoreDecoder.BeatmapNotFoundException notFound)
                {
                    Logger.Log($@"Score '{archive.Name}' failed to import: no corresponding beatmap with the hash '{notFound.Hash}' could be found.", LoggingTarget.Database);
                    return null;
                }
                catch (Exception e)
                {
                    Logger.Log($@"Failed to parse headers of score '{archive.Name}': {e}.", LoggingTarget.Database);
                    return null;
                }
            }
        }

        public Score GetScore(ScoreInfo score) => new LegacyDatabasedScore(score, rulesets, beatmaps(), Files.Store);

        protected override void Populate(ScoreInfo model, ArchiveReader? archive, Realm realm, CancellationToken cancellationToken = default)
        {
            Debug.Assert(model.BeatmapInfo != null);

            // Ensure the beatmap is not detached.
            if (!model.BeatmapInfo.IsManaged)
                model.BeatmapInfo = realm.Find<BeatmapInfo>(model.BeatmapInfo.ID)!;

            if (!model.Ruleset.IsManaged)
                model.Ruleset = realm.Find<RulesetInfo>(model.Ruleset.ShortName)!;

            // These properties are known to be non-null, but these final checks ensure a null hasn't come from somewhere (or the refetch has failed).
            // Under no circumstance do we want these to be written to realm as null.
            ArgumentNullException.ThrowIfNull(model.BeatmapInfo);
            ArgumentNullException.ThrowIfNull(model.Ruleset);

            if (!ModUtils.CheckCompatibleSet(model.Mods))
                throw new InvalidOperationException(@"The score specifies an incompatible set of mods!");

            if (string.IsNullOrEmpty(model.StatisticsJson))
                model.StatisticsJson = JsonConvert.SerializeObject(model.Statistics);

            if (string.IsNullOrEmpty(model.MaximumStatisticsJson))
                model.MaximumStatisticsJson = JsonConvert.SerializeObject(model.MaximumStatistics);
        }

        protected override void PostImport(ScoreInfo model, Realm realm, ImportParameters parameters)
        {
            base.PostImport(model, realm, parameters);

            Debug.Assert(model.BeatmapInfo != null);

            // Every stored replay belongs to the local player.
            if (model.BeatmapInfo.LastPlayed == null || model.Date > model.BeatmapInfo.LastPlayed)
                model.BeatmapInfo.LastPlayed = model.Date;
        }
    }
}

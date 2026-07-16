// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Formats;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.IO.Archives;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Handles the storage and retrieval of Beatmaps/WorkingBeatmaps.
    /// </summary>
    public class BeatmapImporter : RealmArchiveModelImporter<BeatmapSetInfo>
    {
        public override IEnumerable<string> HandledExtensions => new[] { ".osz" };

        protected override string[] HashableFileTypes => new[] { ".osu" };

        public ProcessBeatmapDelegate? ProcessBeatmap { private get; set; }

        public BeatmapImporter(Storage storage, RealmAccess realm)
            : base(storage, realm)
        {
        }

        protected override bool ShouldDeleteArchive(string path) => HandledExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        protected override void Populate(BeatmapSetInfo beatmapSet, ArchiveReader? archive, Realm realm, CancellationToken cancellationToken = default)
        {
            if (archive != null)
                beatmapSet.Beatmaps.AddRange(createBeatmapDifficulties(beatmapSet, realm));

            beatmapSet.DateAdded = getDateAdded(archive);

            foreach (BeatmapInfo b in beatmapSet.Beatmaps)
            {
                b.BeatmapSet = beatmapSet;

                if (!b.Ruleset.IsManaged)
                    b.Ruleset = realm.Find<RulesetInfo>(b.Ruleset.ShortName) ?? throw new ArgumentNullException(nameof(b.Ruleset));
            }
        }

        protected override void PostImport(BeatmapSetInfo model, Realm realm, ImportParameters parameters)
        {
            base.PostImport(model, realm, parameters);

            foreach (BeatmapInfo beatmap in model.Beatmaps)
            {
                beatmap.UpdateLocalScores(realm);
            }

            ProcessBeatmap?.Invoke(model);
        }

        public override string HumanisedModelName => "beatmap";

        protected override BeatmapSetInfo? CreateModel(ArchiveReader reader, ImportParameters parameters)
        {
            string? mapName = reader.Filenames.FirstOrDefault(f => f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(mapName))
            {
                Logger.Log($"No beatmap files found in the beatmap archive ({reader.Name}).", LoggingTarget.Database);
                return null;
            }

            Beatmap beatmap;

            using (var stream = new LineBufferedReader(reader.GetStream(mapName)))
            {
                if (stream.PeekLine() == null)
                {
                    Logger.Log($"No content found in first .osu file of beatmap archive ({reader.Name} / {mapName})", LoggingTarget.Database);
                    return null;
                }

                beatmap = Decoder.GetDecoder<Beatmap>(stream).Decode(stream);
            }

            return new BeatmapSetInfo();
        }

        private DateTimeOffset getDateAdded(ArchiveReader? reader)
        {
            DateTimeOffset dateAdded = DateTimeOffset.UtcNow;

            if (reader is DirectoryArchiveReader legacyReader)
            {
                var beatmaps = reader.Filenames.Where(f => f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));

                dateAdded = File.GetLastWriteTimeUtc(legacyReader.GetFullPath(beatmaps.First()));

                foreach (string beatmapName in beatmaps)
                {
                    var currentDateAdded = File.GetLastWriteTimeUtc(legacyReader.GetFullPath(beatmapName));

                    if (currentDateAdded < dateAdded)
                        dateAdded = currentDateAdded;
                }
            }

            return dateAdded;
        }

        private List<BeatmapInfo> createBeatmapDifficulties(BeatmapSetInfo beatmapSet, Realm realm)
        {
            var beatmaps = new List<BeatmapInfo>();

            foreach (var file in beatmapSet.Files.Where(f => !f.Filename.Contains('/') && f.Filename.EndsWith(@".osu", StringComparison.OrdinalIgnoreCase)))
            {
                using (var memoryStream = new MemoryStream(Files.Store.Get(file.File.GetStoragePath())))
                {
                    IBeatmap decoded;

                    using (var lineReader = new LineBufferedReader(memoryStream, true))
                    {
                        if (lineReader.PeekLine() == null)
                        {
                            LogForModel(beatmapSet, $"No content found in beatmap file {file.Filename}.");
                            continue;
                        }

                        decoded = Decoder.GetDecoder<Beatmap>(lineReader).Decode(lineReader);
                    }

                    string hash = memoryStream.ComputeSHA2Hash();

                    if (beatmaps.Any(b => b.Hash == hash))
                    {
                        LogForModel(beatmapSet, $"Skipping import of {file.Filename} due to duplicate file content.");
                        continue;
                    }

                    var decodedInfo = decoded.BeatmapInfo;
                    var decodedDifficulty = decodedInfo.Difficulty;

                    var ruleset = realm.Find<RulesetInfo>(decodedInfo.Ruleset.ShortName);

                    if (ruleset == null)
                    {
                        LogForModel(beatmapSet, $"Skipping import of {file.Filename} due to missing local ruleset \"{decodedInfo.Ruleset.ShortName}\".");
                        continue;
                    }

                    var difficulty = new BeatmapDifficulty
                    {
                        DrainRate = decodedDifficulty.DrainRate,
                        CircleSize = decodedDifficulty.CircleSize,
                        OverallDifficulty = decodedDifficulty.OverallDifficulty,
                        ApproachRate = decodedDifficulty.ApproachRate,
                        SliderMultiplier = decodedDifficulty.SliderMultiplier,
                        SliderTickRate = decodedDifficulty.SliderTickRate
                    };

                    var metadata = new BeatmapMetadata
                    {
                        Title = decoded.Metadata.Title,
                        TitleUnicode = decoded.Metadata.TitleUnicode,
                        Artist = decoded.Metadata.Artist,
                        ArtistUnicode = decoded.Metadata.ArtistUnicode,
                        Creator = decoded.Metadata.Creator,
                        Source = decoded.Metadata.Source,
                        Tags = decoded.Metadata.Tags,
                        PreviewTime = decoded.Metadata.PreviewTime,
                        AudioFile = decoded.Metadata.AudioFile,
                        BackgroundFile = decoded.Metadata.BackgroundFile,
                    };

                    var beatmap = new BeatmapInfo(ruleset, difficulty, metadata)
                    {
                        Hash = hash,
                        DifficultyName = decodedInfo.DifficultyName,
                        BeatDivisor = decodedInfo.BeatDivisor,
                        MD5Hash = memoryStream.ComputeMD5Hash(),
                        EndTimeObjectCount = decoded.HitObjects.Count(h => h is IHasDuration),
                        TotalObjectCount = decoded.HitObjects.Count
                    };

                    beatmaps.Add(beatmap);
                }
            }

            if (!beatmaps.Any())
                throw new ArgumentException("No valid beatmap files found in the beatmap archive.");

            return beatmaps;
        }
    }
}

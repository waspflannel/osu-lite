// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring.Legacy
{
    /// <summary>
    /// A minified score metadata payload retrofit onto the end of legacy replay files (.osr),
    /// containing the minimum data required to support storage of non-legacy replays.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LegacyReplaySoloScoreInfo
    {
        [JsonProperty("mods")]
        public SerialisedMod[] Mods { get; set; } = Array.Empty<SerialisedMod>();

        [JsonProperty("statistics")]
        public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

        [JsonProperty("maximum_statistics")]
        public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new Dictionary<HitResult, int>();

        [JsonProperty("client_version")]
        public string ClientVersion = string.Empty;

        [JsonProperty("rank")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreRank? Rank;

        [JsonProperty("total_score_without_mods")]
        public long? TotalScoreWithoutMods { get; set; }

        [JsonProperty("pauses")]
        public int[] Pauses { get; set; } = [];

        public static LegacyReplaySoloScoreInfo FromScore(ScoreInfo score) => new LegacyReplaySoloScoreInfo
        {
            Mods = score.SerialisedMods,
            Statistics = score.Statistics.Where(kvp => kvp.Value != 0).ToDictionary(),
            MaximumStatistics = score.MaximumStatistics.Where(kvp => kvp.Value != 0).ToDictionary(),
            ClientVersion = score.ClientVersion,
            Rank = score.Rank,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods > 0 ? score.TotalScoreWithoutMods : null,
            Pauses = score.Pauses.ToArray(),
        };
    }
}

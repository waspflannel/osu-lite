// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using MessagePack;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    [Serializable]
    [MessagePackObject]
    public class FrameHeader
    {
        [Key(0)]
        public long TotalScore { get; set; }

        [Key(1)]
        public double Accuracy { get; set; }

        [Key(2)]
        public int Combo { get; set; }

        [Key(3)]
        public int MaxCombo { get; set; }

        [Key(4)]
        public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

        [Key(5)]
        public ScoreProcessorStatistics ScoreProcessorStatistics { get; set; } = null!;

        [Key(6)]
        public DateTimeOffset ReceivedTime { get; set; }

        public FrameHeader()
        {
        }

        public FrameHeader(ScoreInfo score, ScoreProcessorStatistics statistics)
        {
            TotalScore = score.TotalScore;
            Accuracy = score.Accuracy;
            Combo = score.Combo;
            MaxCombo = score.MaxCombo;
            Statistics = new Dictionary<HitResult, int>(score.Statistics);
            Pauses = new List<int>(score.Pauses).ToArray();
            ScoreProcessorStatistics = statistics;
        }

        [Key(9)]
        public int[]? Pauses { get; set; }
    }
}

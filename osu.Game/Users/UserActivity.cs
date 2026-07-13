// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osuTK.Graphics;

namespace osu.Game.Users
{
    /// <summary>
    /// Base class for all structures describing the user's current activity.
    /// </summary>
    [Serializable]
    public abstract class UserActivity
    {
        public abstract string GetStatus(bool hideIdentifiableInformation = false);
        public virtual string? GetDetails(bool hideIdentifiableInformation = false) => null;

        public virtual Color4 GetAppropriateColour(OsuColour colours) => colours.GreenDarker;

        /// <summary>
        /// Returns the ID of the beatmap involved in this activity, if applicable and/or available.
        /// </summary>
        public virtual int? GetBeatmapID(bool hideIdentifiableInformation = false) => null;

        public class ChoosingBeatmap : UserActivity
        {
            public override string GetStatus(bool hideIdentifiableInformation = false) => "Choosing a beatmap";
        }

        public abstract class InGame : UserActivity
        {
            public int BeatmapID { get; set; }

            public string BeatmapDisplayTitle { get; set; } = string.Empty;

            public int RulesetID { get; set; }

            public string RulesetPlayingVerb { get; set; } = string.Empty;

            protected InGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset)
            {
                BeatmapID = beatmapInfo.OnlineID;
                BeatmapDisplayTitle = beatmapInfo.GetDisplayTitle();

                RulesetID = ruleset.OnlineID;
                RulesetPlayingVerb = ruleset.CreateInstance().PlayingVerb;
            }

            protected InGame() { }

            public override string GetStatus(bool hideIdentifiableInformation = false) => RulesetPlayingVerb;
            public override string GetDetails(bool hideIdentifiableInformation = false) => BeatmapDisplayTitle;
            public override int? GetBeatmapID(bool hideIdentifiableInformation = false) => BeatmapID;
        }

        public class InSoloGame : InGame
        {
            public InSoloGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset)
                : base(beatmapInfo, ruleset)
            {
            }

            public InSoloGame() { }
        }

        public class WatchingReplay : UserActivity
        {
            public long ScoreID { get; set; }

            public string PlayerName { get; set; } = string.Empty;

            public int BeatmapID { get; set; }

            public string? BeatmapDisplayTitle { get; set; }

            public WatchingReplay(ScoreInfo score)
            {
                ScoreID = score.OnlineID;
                PlayerName = score.User.Username;
                BeatmapID = score.BeatmapInfo?.OnlineID ?? -1;
                BeatmapDisplayTitle = score.BeatmapInfo?.GetDisplayTitle();
            }

            public WatchingReplay() { }

            public override string GetStatus(bool hideIdentifiableInformation = false) => hideIdentifiableInformation ? @"Watching a replay" : $@"Watching {PlayerName}'s replay";
            public override string? GetDetails(bool hideIdentifiableInformation = false) => BeatmapDisplayTitle;
        }
    }
}

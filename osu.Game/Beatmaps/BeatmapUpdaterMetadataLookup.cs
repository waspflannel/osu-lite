// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Platform;
using osu.Game.Extensions;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A component which populates beatmap metadata/online IDs from the bundled local metadata cache.
    /// osu! lite is offline, so there is no remote metadata source.
    /// </summary>
    public class BeatmapUpdaterMetadataLookup : IDisposable
    {
        private readonly IOnlineBeatmapMetadataSource localCachedMetadataSource;

        public BeatmapUpdaterMetadataLookup(Storage storage)
            : this(new LocalCachedBeatmapMetadataSource(storage))
        {
        }

        internal BeatmapUpdaterMetadataLookup(IOnlineBeatmapMetadataSource localCachedMetadataSource)
        {
            this.localCachedMetadataSource = localCachedMetadataSource;
        }

        /// <summary>
        /// Queue an update for a beatmap set.
        /// </summary>
        /// <remarks>
        /// This may happen during initial import, or at a later stage in response to a user action or server event.
        /// </remarks>
        /// <param name="beatmapSet">The beatmap set to update. Updates will be applied directly (so a transaction should be started if this instance is managed).</param>
        /// <param name="preferOnlineFetch">Whether metadata from an online source should be preferred. If <c>true</c>, the local cache will be skipped to ensure the freshest data state possible.</param>
        public void Update(BeatmapSetInfo beatmapSet, bool preferOnlineFetch)
        {
            var lookupResults = new List<OnlineBeatmapMetadata?>();

            foreach (var beatmapInfo in beatmapSet.Beatmaps)
            {
                // note that these lookups DO NOT ACTUALLY FULLY GUARANTEE that the beatmap is what it claims it is,
                // i.e. the correctness of this lookup should be treated as APPROXIMATE AT WORST.
                // this is because the beatmap filename is used as a fallback in some scenarios where the MD5 of the beatmap may mismatch.
                // this is considered to be an acceptable casualty so that things can continue to work as expected for users in some rare scenarios
                // (stale beatmap files in beatmap packs, beatmap mirror desyncs).
                // however, all this means that other places such as score submission ARE EXPECTED TO VERIFY THE MD5 OF THE BEATMAP AGAINST THE ONLINE ONE EXPLICITLY AGAIN.
                //
                // additionally note that the online ID stored to the map is EXPLICITLY NOT USED because some users in a silly attempt to "fix" things for themselves on stable
                // would reuse online IDs of already submitted beatmaps, which means that information is pretty much expected to be bogus in a nonzero number of beatmapsets.
                if (!tryLookup(beatmapInfo, preferOnlineFetch, out var res))
                    continue;

                if (res == null)
                {
                    beatmapInfo.ResetOnlineInfo();
                    lookupResults.Add(null); // mark lookup failure
                    continue;
                }

                lookupResults.Add(res);

                beatmapInfo.OnlineID = res.BeatmapID;
                beatmapInfo.OnlineMD5Hash = res.MD5Hash;
                beatmapInfo.LastOnlineUpdate = res.LastUpdated;

                Debug.Assert(beatmapInfo.BeatmapSet != null);
                beatmapInfo.BeatmapSet.OnlineID = res.BeatmapSetID;

                // Some metadata should only be applied if there's no local changes.
                if (beatmapInfo.MatchesOnlineVersion)
                {
                    beatmapInfo.Status = res.BeatmapStatus;
                    beatmapInfo.Metadata.Author.OnlineID = res.AuthorID;
                    beatmapInfo.Metadata.UserTags.Clear();
                    beatmapInfo.Metadata.UserTags.AddRange(res.UserTags);
                }
            }

            if (beatmapSet.Beatmaps.All(b => b.MatchesOnlineVersion)
                && lookupResults.All(r => r != null)
                && lookupResults.Select(r => r!.BeatmapSetID).Distinct().Count() == 1)
            {
                var representative = lookupResults.First()!;

                beatmapSet.Status = representative.BeatmapSetStatus ?? BeatmapOnlineStatus.None;
                beatmapSet.DateRanked = representative.DateRanked;
                beatmapSet.DateSubmitted = representative.DateSubmitted;
            }
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="OnlineBeatmapMetadata"/> for the given <paramref name="beatmapInfo"/>.
        /// </summary>
        /// <param name="beatmapInfo">The beatmap to perform the online lookup for.</param>
        /// <param name="preferOnlineFetch">Whether online sources should be preferred for the lookup.</param>
        /// <param name="result">The result of the lookup. Can be <see langword="null"/> if no matching beatmap was found (or the lookup failed).</param>
        /// <returns>
        /// <see langword="true"/> if the local metadata cache was available and returned a valid <paramref name="result"/>.
        /// <see langword="false"/> if the cache was unavailable, or if there was insufficient data to return a valid <paramref name="result"/>.
        /// </returns>
        /// <remarks>
        /// When this returns <see langword="false"/>, the online ID read from the .osu file will be preserved, which may not necessarily be what we want.
        /// </remarks>
        private bool tryLookup(BeatmapInfo beatmapInfo, bool preferOnlineFetch, out OnlineBeatmapMetadata? result)
        {
            if (localCachedMetadataSource.TryLookup(beatmapInfo, out result))
                return true;

            result = null;
            return false;
        }

        public void Dispose()
        {
            localCachedMetadataSource.Dispose();
        }
    }
}

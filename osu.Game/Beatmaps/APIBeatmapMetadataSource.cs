// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// osu! lite is offline, so online metadata lookups are never available and always fail.
    /// </summary>
    public class APIBeatmapMetadataSource : IOnlineBeatmapMetadataSource
    {
        public APIBeatmapMetadataSource(IAPIProvider api)
        {
        }

        public bool Available => false;

        public bool TryLookup(BeatmapInfo beatmapInfo, out OnlineBeatmapMetadata? onlineMetadata)
        {
            onlineMetadata = null;
            return false;
        }

        public void Dispose()
        {
        }
    }
}

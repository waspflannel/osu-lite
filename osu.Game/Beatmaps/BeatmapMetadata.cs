// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Game.Screens.Select;
using osu.Game.Utils;
using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A realm model containing metadata for a beatmap.
    /// </summary>
    /// <remarks>
    /// An instance of this object is stored against each beatmap difficulty.
    /// It is also provided via <see cref="BeatmapSetInfo"/> for convenience and historical purposes.
    /// Note that accessing the metadata via <see cref="BeatmapSetInfo"/> may result in indeterminate results
    /// as metadata can meaningfully differ per beatmap in a set.
    ///
    /// Note that difficulty name is not stored in this metadata but in <see cref="BeatmapInfo"/>.
    /// </remarks>
    [Serializable]
    [MapTo("BeatmapMetadata")]
    public class BeatmapMetadata : RealmObject, IBeatmapMetadataInfo, IDeepCloneable<BeatmapMetadata>
    {
        public string Title { get; set; } = string.Empty;

        [JsonProperty("title_unicode")]
        public string TitleUnicode { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        [JsonProperty("artist_unicode")]
        public string ArtistUnicode { get; set; } = string.Empty;

        public string Creator { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        [JsonProperty(@"tags")]
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// The time in milliseconds to begin playing the track for preview purposes.
        /// If -1, the track should begin playing at 40% of its length.
        /// </summary>
        public int PreviewTime { get; set; } = -1;

        public string AudioFile { get; set; } = string.Empty;
        public string BackgroundFile { get; set; } = string.Empty;

        public override string ToString() => this.GetDisplayTitle();

        public BeatmapMetadata DeepClone() => new BeatmapMetadata
        {
            Title = Title,
            TitleUnicode = TitleUnicode,
            Artist = Artist,
            ArtistUnicode = ArtistUnicode,
            Creator = Creator,
            Source = Source,
            Tags = Tags,
            PreviewTime = PreviewTime,
            AudioFile = AudioFile,
            BackgroundFile = BackgroundFile
        };
    }
}

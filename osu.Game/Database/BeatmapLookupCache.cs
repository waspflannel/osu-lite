// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Database
{
    /// <summary>
    /// osu! lite is offline, so online beatmap lookups always return null.
    /// </summary>
    public partial class BeatmapLookupCache : Component
    {
        public Task<APIBeatmap?> GetBeatmapAsync(int beatmapId, CancellationToken token = default) => Task.FromResult<APIBeatmap?>(null);

        public Task<APIBeatmap?[]> GetBeatmapsAsync(int[] beatmapIds, CancellationToken token = default) => Task.FromResult(beatmapIds.Select(_ => (APIBeatmap?)null).ToArray());
    }
}

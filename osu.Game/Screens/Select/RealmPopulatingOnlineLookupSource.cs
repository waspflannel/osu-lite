// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// Persists portions of online beatmap data to realm for later local use.
    /// osu! lite is offline, so no online lookups are performed.
    /// </summary>
    public partial class RealmPopulatingOnlineLookupSource : Component
    {
        public Task<APIBeatmapSet?> GetBeatmapSetAsync(int id, CancellationToken token = default) => Task.FromResult<APIBeatmapSet?>(null);
    }
}

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
    /// osu! lite is offline, so online user lookups always return null.
    /// </summary>
    public partial class UserLookupCache : Component
    {
        public Task<APIUser?> GetUserAsync(int userId, CancellationToken token = default) => Task.FromResult<APIUser?>(null);

        public Task<APIUser?[]> GetUsersAsync(int[] userIds, CancellationToken token = default) => Task.FromResult(userIds.Select(_ => (APIUser?)null).ToArray());
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online.API
{
    public enum APIState
    {
        /// <summary>
        /// We cannot login (not enough credentials).
        /// </summary>
        Offline,

        /// <summary>
        /// We are having connectivity issues.
        /// </summary>
        Failing,

        /// <summary>
        /// Waiting on second factor authentication.
        /// </summary>
        RequiresSecondFactorAuth,

        /// <summary>
        /// We are in the process of (re-)connecting.
        /// </summary>
        Connecting,

        /// <summary>
        /// We are online.
        /// </summary>
        Online
    }
}

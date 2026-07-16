// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Game.Configuration
{
    /// <summary>
    /// The sole identity used by the local player.
    /// </summary>
    public sealed class LocalPlayerName
    {
        public Bindable<string> Value { get; }

        public LocalPlayerName(OsuConfigManager config)
        {
            Value = config.GetBindable<string>(OsuSetting.LocalPlayerName);
        }
    }
}

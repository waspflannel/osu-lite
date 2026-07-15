// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.IO.Stores;
using osu.Game.IO;
using osuTK.Graphics;

namespace osu.Game.Skinning
{
    public class DefaultLegacySkin : LegacySkin
    {
        public static readonly List<Color4> DEFAULT_COMBO_COLOURS = new List<Color4>
        {
            new Color4(255, 192, 0, 255),
            new Color4(0, 202, 0, 255),
            new Color4(18, 124, 255, 255),
            new Color4(242, 24, 57, 255)
        };

        public DefaultLegacySkin(IStorageResourceProvider resources)
            : base(
                "osu! \"classic\" (2013)",
                resources,
                new NamespacedResourceStore<byte[]>(resources.Resources, "Skins/Legacy")
            )
        {
            Configuration.CustomColours["SliderBall"] = new Color4(2, 170, 255, 255);
            Configuration.CustomComboColours = DEFAULT_COMBO_COLOURS;

            Configuration.ConfigDictionary[nameof(SkinConfiguration.LegacySetting.AllowSliderBallTint)] = @"true";

            Configuration.LegacyVersion = 2.7m;
            Configuration.IsLatestVersion = true;
        }
    }
}

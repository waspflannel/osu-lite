// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.Maintenance;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class DataSection : SettingsSection
    {
        public override LocalisableString Header => "Data";

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = OsuIcon.Maintenance
        };

        public DataSection()
        {
            Children = new Drawable[]
            {
                new GeneralSettings(),
                new BeatmapSettings(),
                new ScoreSettings(),
            };
        }
    }
}

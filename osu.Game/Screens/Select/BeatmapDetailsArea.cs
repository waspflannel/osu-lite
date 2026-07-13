// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// The left portion of the song select screen which houses the beatmap metadata wedge.
    /// osu! lite is offline, so there is no leaderboard view to switch to.
    /// </summary>
    public partial class BeatmapDetailsArea : VisibilityContainer
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new ShearAligningWrapper(new BeatmapMetadataWedge
            {
                Shear = -OsuGame.SHEAR,
                RelativeSizeAxes = Axes.Both,
            });
        }

        protected override void PopIn()
        {
            this.MoveToX(0, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeIn(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        protected override void PopOut()
        {
            this.MoveToX(-150, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeOut(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        public void Refresh()
        {
        }
    }
}

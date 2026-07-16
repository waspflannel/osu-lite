// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Ranking.Expanded
{
    /// <summary>
    /// The content that appears in the middle section of the <see cref="ScorePanel"/>.
    /// </summary>
    public partial class ExpandedPanelTopContent : CompositeDrawable
    {
        private Sample appearanceSample;

        private readonly bool playAppearanceSound;

        /// <summary>
        /// Creates a new <see cref="ExpandedPanelTopContent"/>.
        /// </summary>
        /// <param name="playAppearanceSound">Whether the appearance sample should play</param>
        public ExpandedPanelTopContent(bool playAppearanceSound = false)
        {
            this.playAppearanceSound = playAppearanceSound;
            Anchor = Anchor.TopCentre;
            Origin = Anchor.Centre;

            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, LocalPlayerName playerName)
        {
            appearanceSample = audio.Samples.Get(@"Results/score-panel-top-appear");

            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Font = OsuFont.GetFont(size: 22, weight: FontWeight.Bold),
                        Text = playerName.Value.Value,
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (playAppearanceSound)
                appearanceSample?.Play();
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osuTK;

namespace osu.Game.Overlays.Notifications
{
    public partial class SimpleNotification : Notification
    {
        private LocalisableString text;

        public override LocalisableString Text
        {
            get => text;
            set
            {
                text = value;
                TextFlow.Text = text;
            }
        }

        private IconUsage icon = FontAwesome.Solid.InfoCircle;

        public IconUsage Icon
        {
            get => icon;
            set
            {
                icon = value;
                IconDrawable.Icon = icon;
            }
        }

        protected TextFlowContainer TextFlow { get; }
        protected SpriteIcon IconDrawable { get; }

        public SimpleNotification()
        {
            AddInternal(new FillFlowContainer
            {
                Direction = FillDirection.Horizontal,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(10, 0),
                Padding = new MarginPadding(10),
                Children = new Drawable[]
                {
                    IconDrawable = new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Icon = icon,
                        Size = new Vector2(16),
                    },
                    TextFlow = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 14, weight: FontWeight.Medium))
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Text = text
                    },
                }
            });
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;

namespace osu.Game.Overlays.Notifications
{
    public abstract partial class Notification : Container
    {
        public event Action? Closed;

        public abstract LocalisableString Text { get; set; }

        public bool WasClosed { get; private set; }

        public Func<bool>? Activated;

        protected const float CORNER_RADIUS = 6;

        private Box background = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        protected Notification()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = CORNER_RADIUS;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider?.Background3 ?? Framework.Graphics.Colour.Colour4.DarkGray,
                    Depth = float.MaxValue
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(200);
        }

        public virtual void Close()
        {
            if (WasClosed) return;

            WasClosed = true;
            Closed?.Invoke();

            Schedule(() =>
            {
                this.FadeOut(200);
                Expire();
            });
        }

        protected override bool OnClick(Framework.Input.Events.ClickEvent e)
        {
            if (Activated?.Invoke() == false)
                return true;

            Close();
            return true;
        }
    }
}

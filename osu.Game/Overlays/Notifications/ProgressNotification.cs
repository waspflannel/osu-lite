// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Notifications
{
    public partial class ProgressNotification : Notification
    {
        public readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource;

        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        public bool Ongoing => State != ProgressNotificationState.Completed && State != ProgressNotificationState.Cancelled;

        public Action<Notification>? CompletionTarget { get; set; }

        private LocalisableString text;

        public override LocalisableString Text
        {
            get => text;
            set
            {
                text = value;
                textDrawable.Text = text;
            }
        }

        public LocalisableString CompletionText { get; set; } = "Task has completed!";

        private float progress;

        public float Progress
        {
            get => progress;
            set
            {
                progress = value;
                progressBar.Progress = value;
            }
        }

        private ProgressNotificationState state;

        public ProgressNotificationState State
        {
            get => state;
            set
            {
                if (state == value) return;

                state = value;

                switch (state)
                {
                    case ProgressNotificationState.Cancelled:
                        CancellationTokenSource.Cancel();
                        break;

                    case ProgressNotificationState.Completed:
                        attemptPostCompletion();
                        break;
                }
            }
        }

        private int completionSent;

        private void attemptPostCompletion()
        {
            if (CompletionTarget == null) return;

            if (Interlocked.Exchange(ref completionSent, 1) == 1) return;

            CompletionTarget.Invoke(new SimpleNotification
            {
                Text = CompletionText,
                Icon = FontAwesome.Solid.Check,
            });

            Close();
        }

        private readonly TextFlowContainer textDrawable;
        private readonly ProgressBar progressBar;

        public ProgressNotification()
        {
            AddInternal(new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding(10),
                Spacing = new Vector2(5, 0),
                Children = new Drawable[]
                {
                    textDrawable = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 14, weight: FontWeight.Medium))
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                    },
                    progressBar = new ProgressBar()
                }
            });

            State = ProgressNotificationState.Queued;
        }
    }

    public enum ProgressNotificationState
    {
        Queued,
        Active,
        Completed,
        Cancelled
    }

    internal partial class ProgressBar : Container
    {
        private readonly Box box;

        private float progress;

        public float Progress
        {
            get => progress;
            set
            {
                if (progress == value) return;

                progress = value;
                box.ResizeTo(new Vector2(progress, 1), 100, Easing.OutQuad);
            }
        }

        public ProgressBar()
        {
            Height = 5;
            RelativeSizeAxes = Axes.X;

            Children = new[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Gray,
                },
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0,
                    Colour = Colour4.DodgerBlue,
                }
            };
        }
    }
}

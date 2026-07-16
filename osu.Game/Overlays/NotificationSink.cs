// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using osu.Game.Overlays.Notifications;
using osuTK;

namespace osu.Game.Overlays
{
    public partial class NotificationSink : CompositeDrawable, INotificationSink
    {
        private FillFlowContainer<Notification> toastFlow = null!;
        private readonly List<ProgressNotification> ongoing = new List<ProgressNotification>();

        public IEnumerable<ProgressNotification> OngoingOperations => ongoing;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomRight;
            Origin = Anchor.BottomRight;

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    AutoSizeAxes = Axes.Both,
                    Margin = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        toastFlow = new FillFlowContainer<Notification>
                        {
                            Direction = FillDirection.Vertical,
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            AutoSizeAxes = Axes.Both,
                            Spacing = new Vector2(0, 5),
                        }
                    }
                }
            };
        }

        public void Post(Notification notification)
        {
            Scheduler.Add(() =>
            {
                if (notification is ProgressNotification progressNotification)
                {
                    progressNotification.CompletionTarget = Post;
                    ongoing.Add(progressNotification);
                    progressNotification.Closed += () =>
                    {
                        ongoing.Remove(progressNotification);
                    };
                }

                notification.Closed += () =>
                {
                    Schedule(() => toastFlow.Remove(notification, true));
                };

                toastFlow.Add(notification);
            });
        }

        public new void Hide()
        {
        }
    }
}

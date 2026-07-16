// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Overlays
{
    [Cached]
    public interface INotificationSink
    {
        void Post(Notification notification);

        void Hide();

        bool HasOngoingOperations => OngoingOperations.Any();

        IEnumerable<ProgressNotification> OngoingOperations { get; }
    }
}

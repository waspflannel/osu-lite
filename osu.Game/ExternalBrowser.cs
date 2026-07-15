// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Game
{
    public enum ExternalBrowserDestination
    {
        IssueTracker,
        TabletList,
        TabletFaq,
    }

    /// <summary>
    /// Opens the small, fixed set of browser destinations supported by osu! lite.
    /// </summary>
    public sealed class ExternalBrowser
    {
        private const string issue_tracker_url = @"https://github.com/waspflannel/osu-lite/issues/new/choose";
        private const string tablet_list_url = @"https://opentabletdriver.net/Tablets";
        private const string tablet_faq_url = @"https://opentabletdriver.net/Wiki/FAQ/General";

        private readonly GameHost host;

        public ExternalBrowser(GameHost host)
        {
            this.host = host;
        }

        public void Open(ExternalBrowserDestination destination)
        {
            host.OpenUrlExternally(destination switch
            {
                ExternalBrowserDestination.IssueTracker => issue_tracker_url,
                ExternalBrowserDestination.TabletList => tablet_list_url,
                ExternalBrowserDestination.TabletFaq => tablet_faq_url,
                _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null),
            });
        }
    }
}

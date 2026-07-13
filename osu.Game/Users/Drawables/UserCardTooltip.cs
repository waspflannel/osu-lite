// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Game.Graphics.Cursor;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Users.Drawables
{
    public partial class UserCardTooltip : VisibilityContainer, ITooltip<APIUser?>
    {
        public UserCardTooltip()
        {
            AutoSizeAxes = Axes.Both;
        }

        protected override void PopIn() => this.FadeIn(150, Easing.OutQuint);
        protected override void PopOut() => this.Delay(150).FadeOut(500, Easing.OutQuint);

        public void Move(Vector2 pos) => Position = pos;

        private APIUser? user;

        public void SetContent(APIUser? content)
        {
            if (content == user && Children.Any())
                return;

            user = content;

            // osu! lite is offline, so there is no rich user card to show; fall back to a simple tooltip.
            var tooltip = new OsuTooltipContainer.OsuTooltip();
            tooltip.SetContent(ContextMenuStrings.ViewProfile);
            tooltip.Show();

            Child = tooltip;
        }
    }
}

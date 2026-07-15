// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;

namespace osu.Game.Graphics.Containers
{
    public partial class LinkFlowContainer : OsuTextFlowContainer
    {
        public LinkFlowContainer(Action<SpriteText> defaultCreationParameters = null)
            : base(defaultCreationParameters)
        {
        }

        public void AddLink(LocalisableString text, Action action, string tooltipText = null, Action<SpriteText> creationParameters = null)
            => createLink(CreateChunkFor(text, true, CreateSpriteText, creationParameters), tooltipText, action);

        private void createLink(ITextPart textPart, LocalisableString tooltipText, Action action) => AddPart(new TextLink(textPart, tooltipText, action));

        private class TextLink : TextPart
        {
            private readonly ITextPart innerPart;
            private readonly LocalisableString tooltipText;
            private readonly Action action;

            public TextLink(ITextPart innerPart, LocalisableString tooltipText, Action action)
            {
                this.innerPart = innerPart;
                this.tooltipText = tooltipText;
                this.action = action;
            }

            protected override IEnumerable<Drawable> CreateDrawablesFor(TextFlowContainer textFlowContainer)
            {
                var linkFlowContainer = (LinkFlowContainer)textFlowContainer;

                innerPart.RecreateDrawablesFor(linkFlowContainer);
                var drawables = innerPart.Drawables.ToList();

                drawables.Add(linkFlowContainer.CreateLinkCompiler(innerPart).With(c =>
                {
                    c.RelativeSizeAxes = Axes.Both;
                    c.TooltipText = tooltipText;
                    c.Action = action;
                }));

                return drawables;
            }
        }

        protected virtual DrawableLinkCompiler CreateLinkCompiler(ITextPart textPart) => new DrawableLinkCompiler(textPart);

        protected override InnerFlow CreateFlow() => new LinkFlow();

        private partial class LinkFlow : InnerFlow
        {
            // We want the compilers to always be visible no matter where they are, so RelativeSizeAxes is used.
            // However due to https://github.com/ppy/osu-framework/issues/2073, it's possible for the compilers to be relative size in the flow's auto-size axes - an unsupported operation.
            // Since the compilers don't display any content and don't affect the layout, it's simplest to exclude them from the flow.
            public override IEnumerable<Drawable> FlowingChildren => base.FlowingChildren.Where(c => !(c is DrawableLinkCompiler));
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ListExtensions;
using osu.Framework.Lists;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

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

                drawables.Add(new LinkInteraction(innerPart).With(c =>
                {
                    c.RelativeSizeAxes = Axes.Both;
                    c.TooltipText = tooltipText;
                    c.Action = action;
                }));

                return drawables;
            }
        }

        protected override InnerFlow CreateFlow() => new LinkFlow();

        private partial class LinkInteraction : OsuHoverContainer
        {
            private readonly SlimReadOnlyListWrapper<Drawable> parts;

            public LinkInteraction(ITextPart textPart)
            {
                parts = textPart.Drawables.OfType<SpriteText>().Cast<Drawable>().ToList().AsSlimReadOnly();
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => parts.Any(part => part.ReceivePositionalInputAt(screenSpacePos));

            protected override IEnumerable<Drawable> EffectTargets => parts;

            protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new LinkHoverSounds(sampleSet, parts);

            [Resolved(canBeNull: true)]
            private OverlayColourProvider overlayColourProvider { get; set; }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                IdleColour ??= overlayColourProvider?.Light2 ?? colours.Blue;
            }

            private partial class LinkHoverSounds : HoverClickSounds
            {
                private readonly SlimReadOnlyListWrapper<Drawable> parts;

                public LinkHoverSounds(HoverSampleSet sampleSet, SlimReadOnlyListWrapper<Drawable> parts)
                    : base(sampleSet)
                {
                    this.parts = parts;
                }

                public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => parts.Any(part => part.ReceivePositionalInputAt(screenSpacePos));
            }
        }

        private partial class LinkFlow : InnerFlow
        {
            // We want the interaction targets to always be visible no matter where they are, so RelativeSizeAxes is used.
            // However due to https://github.com/ppy/osu-framework/issues/2073, it's possible for targets to be relative size in the flow's auto-size axes - an unsupported operation.
            // Since the compilers don't display any content and don't affect the layout, it's simplest to exclude them from the flow.
            public override IEnumerable<Drawable> FlowingChildren => base.FlowingChildren.Where(c => !(c is LinkInteraction));
        }
    }
}

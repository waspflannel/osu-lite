// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Skinning
{
    /// <summary>
    /// A container which reloads a fixed skin-provided component layout.
    /// </summary>
    public partial class SkinnableContainer : SkinReloadableDrawable
    {
        private Container? content;

        public GlobalSkinnableContainerLookup Lookup { get; }

        public IReadOnlyList<Drawable> Components => components;

        private readonly List<Drawable> components = new List<Drawable>();

        public override bool IsPresent => base.IsPresent || Scheduler.HasPendingTasks;

        public bool ComponentsLoaded { get; private set; }

        private CancellationTokenSource? cancellationSource;

        public SkinnableContainer(GlobalSkinnableContainerLookup lookup)
        {
            Lookup = lookup;
        }

        public void Reload() => Reload(CurrentSkin.GetDrawableComponent(Lookup) as Container);

        public void Reload(Container? componentsContainer)
        {
            ClearInternal();
            components.Clear();
            ComponentsLoaded = false;

            content = componentsContainer ?? new Container { RelativeSizeAxes = Axes.Both };
            cancellationSource?.Cancel();
            cancellationSource = null;

            LoadComponentAsync(content, wrapper =>
            {
                AddInternal(wrapper);
                components.AddRange(wrapper.Children);
                ComponentsLoaded = true;
            }, (cancellationSource = new CancellationTokenSource()).Token);
        }

        protected override void SkinChanged(ISkinSource skin)
        {
            base.SkinChanged(skin);
            Reload();
        }
    }
}

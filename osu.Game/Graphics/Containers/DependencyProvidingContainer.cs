// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Graphics.Containers
{
    /// <summary>
    /// A container which provides a set of dependencies to its children.
    /// </summary>
    public partial class DependencyProvidingContainer : Container
    {
        /// <summary>
        /// The dependencies provided to the children.
        /// </summary>
        public (Type, object)[] CachedDependencies { get; set; } = Array.Empty<(Type, object)>();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            foreach ((Type type, object value) in CachedDependencies)
                dependencies.CacheAs(type, value);

            return dependencies;
        }
    }
}

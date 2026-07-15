// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.IO.Stores;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.IO.Archives;

namespace osu.Game.Skinning
{
    /// <summary>
    /// The fixed default skin for osu! lite, bundled as an embedded resource (<c>Skinning/Resources/DefaultSkin.osk</c>).
    /// </summary>
    public class CustomDefaultSkin : LegacySkin
    {
        public static SkinInfo CreateInfo() => new SkinInfo
        {
            ID = Skinning.SkinInfo.CUSTOM_DEFAULT_SKIN,
            Name = "kanna 2.0 [OG] ultra lite",
            Creator = "Pirasto",
            Protected = true,
            InstantiationInfo = typeof(CustomDefaultSkin).GetInvariantInstantiationInfo()
        };

        public CustomDefaultSkin(IStorageResourceProvider resources)
            : this(CreateInfo(), resources)
        {
        }

        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public CustomDefaultSkin(SkinInfo skin, IStorageResourceProvider resources)
            : base(skin, resources, createArchiveStore())
        {
        }

        private static IResourceStore<byte[]> createArchiveStore()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DefaultSkin.osk")!;
            return new ZipArchiveReader(stream, "DefaultSkin.osk");
        }
    }
}

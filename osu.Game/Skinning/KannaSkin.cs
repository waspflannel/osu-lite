// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using osu.Framework.IO.Stores;
using osu.Game.IO;
using osu.Game.IO.Archives;

namespace osu.Game.Skinning
{
    /// <summary>
    /// The fixed Kanna skin bundled as an embedded archive.
    /// </summary>
    public sealed class KannaSkin : LegacySkin
    {
        public KannaSkin(IStorageResourceProvider resources)
            : base("kanna 2.0 [OG] ultra lite", resources, createArchiveStore())
        {
        }

        private static IResourceStore<byte[]> createArchiveStore()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DefaultSkin.osk")!;
            return new ZipArchiveReader(stream, "DefaultSkin.osk");
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SixLabors.ImageSharp.Memory;

namespace osu.Game.IO.Archives
{
    public sealed class ZipArchiveReader : ArchiveReader
    {
        /// <summary>
        /// Archives created by osu!stable still write out as Shift-JIS.
        /// We want to force this fallback rather than leave it up to the library/system.
        /// In the future we may want to change exports to set the zip UTF-8 flag and use that instead.
        /// </summary>
        public static readonly ArchiveEncoding DEFAULT_ENCODING;

        private readonly Stream archiveStream;
        private readonly IWritableArchive archive;

        static ZipArchiveReader()
        {
            // Required to support rare code pages.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DEFAULT_ENCODING = new ArchiveEncoding
            {
                Default = Encoding.GetEncoding(932),
                Password = Encoding.GetEncoding(932),
            };
        }

        public ZipArchiveReader(Stream archiveStream, string name = null)
            : base(name)
        {
            this.archiveStream = archiveStream;

            archive = ZipArchive.OpenArchive(archiveStream, new ReaderOptions
            {
                ArchiveEncoding = DEFAULT_ENCODING
            });
        }

        public override Stream GetStream(string name)
        {
            IArchiveEntry entry = archive.Entries.SingleOrDefault(e => e.Key == name);
            if (entry == null)
                return null;

            using (Stream s = entry.OpenEntryStream())
            {
                if (entry.Size > 0)
                {
                    var buffer = new byte[entry.Size];
                    s.ReadExactly(buffer);
                    return new MemoryStream(buffer);
                }

                // due to a sharpcompress bug (https://github.com/adamhathcock/sharpcompress/issues/88),
                // in rare instances the `ZipArchiveEntry` will not contain a correct `Size` but instead report 0.
                // this would lead to the block above reading nothing, and the game basically seeing an archive full of empty files.
                // since the bug is years old now, and this is a rather rare situation anyways (reported once in years),
                // work around this locally by falling back to reading as many bytes as possible and using a standard non-pooled memory stream.
                return new MemoryStream(s.ReadAllRemainingBytesToArray());
            }
        }

        public override void Dispose()
        {
            archive.Dispose();
            archiveStream.Dispose();
        }

        public override IEnumerable<string> Filenames => archive.Entries.Where(e => !e.IsDirectory).Select(e => e.Key).ExcludeSystemFileNames();
    }
}

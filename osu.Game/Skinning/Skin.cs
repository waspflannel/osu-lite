// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Database;
using osu.Game.IO;

namespace osu.Game.Skinning
{
    public abstract class Skin : IDisposable, ISkin
    {
        private readonly IStorageResourceProvider? resources;

        /// <summary>
        /// A texture store which can be used to perform user file lookups for this skin.
        /// </summary>
        protected TextureStore? Textures { get; }

        /// <summary>
        /// A sample store which can be used to perform user file lookups for this skin.
        /// </summary>
        protected internal ISampleStore? Samples { get; private set; }

        public SkinConfiguration Configuration { get; set; }

        public abstract ISample? GetSample(ISampleInfo sampleInfo);

        public Texture? GetTexture(string componentName) => GetTexture(componentName, default, default);

        public abstract Texture? GetTexture(string componentName, WrapMode wrapModeS, WrapMode wrapModeT);

        public abstract IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup)
            where TLookup : notnull
            where TValue : notnull;

        private readonly ResourceStore<byte[]> store = new ResourceStore<byte[]>();

        public string Name { get; }

        protected IResourceStore<byte[]>? FallbackStore { get; }

        /// <summary>
        /// Construct a new skin.
        /// </summary>
        /// <param name="name">The skin display name.</param>
        /// <param name="resources">Access to game-wide resources.</param>
        /// <param name="fallbackStore">An optional fallback store which will be used for file lookups that are not serviced by realm user storage.</param>
        /// <param name="configurationFilename">An optional filename to read the skin configuration from. If not provided, the configuration will be retrieved from the storage using "skin.ini".</param>
        protected Skin(string name, IStorageResourceProvider? resources, IResourceStore<byte[]>? fallbackStore = null, string configurationFilename = @"skin.ini")
        {
            this.resources = resources;

            Name = name;

            if (resources != null)
            {
                RecycleSamples();
                Textures = new TextureStore(resources.Renderer, CreateTextureLoaderStore(resources, store));
            }

            FallbackStore = fallbackStore;
            if (fallbackStore != null)
                store.AddStore(fallbackStore);

            var configurationStream = store.GetStream(configurationFilename);

            if (configurationStream != null)
            {
                // stream will be closed after use by LineBufferedReader.
                ParseConfigurationStream(configurationStream);
                Debug.Assert(Configuration != null);
            }
            else
            {
                Configuration = new SkinConfiguration
                {
                    // Beatmap-local skins may omit a skin.ini.
                    LegacyVersion = SkinConfiguration.LATEST_VERSION,
                    IsLatestVersion = true,
                };
            }

        }

        /// <summary>
        /// Recreates <see cref="Samples"/>.
        /// All users of samples from the skin are expected to manually re-retrieve their samples from this skin after this is called.
        /// Exposed as public for the purpose of e.g. editing flows where the skin's set of available samples changes.
        /// In such a scenario a full recycle of the store is required to avoid accidentally retrieving stale samples that don't exist in the skin anymore.
        /// </summary>
        public void RecycleSamples()
        {
            Samples?.Dispose();

            var samples = resources?.AudioManager?.GetSampleStore(store);

            if (samples != null)
            {
                samples.PlaybackConcurrency = OsuGameBase.SAMPLE_CONCURRENCY;

                // osu-stable performs audio lookups in order of wav -> mp3 -> ogg.
                // The GetSampleStore() call above internally adds wav and mp3, so ogg is added at the end to ensure expected ordering.
                samples.AddExtension(@"ogg");
            }

            Samples = samples;
        }

        protected virtual IResourceStore<TextureUpload> CreateTextureLoaderStore(IStorageResourceProvider resources, IResourceStore<byte[]> storage)
            => new MaxDimensionLimitedTextureLoaderStore(resources.CreateTextureLoaderStore(storage));

        protected virtual void ParseConfigurationStream(Stream stream)
        {
            using (LineBufferedReader reader = new LineBufferedReader(stream, true))
                Configuration = new LegacySkinDecoder().Decode(reader);
        }

        public virtual Drawable? GetDrawableComponent(ISkinComponentLookup lookup) => null;

        #region Disposal

        ~Skin()
        {
            // required to potentially clean up sample store from audio hierarchy.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Textures?.Dispose();
            Samples?.Dispose();
            FallbackStore?.Dispose();

            store.Dispose();
        }

        #endregion

        public override string ToString() => $"{GetType().ReadableName()} {{ Name: {Name} }}";

        private static readonly ThreadLocal<int> nested_level = new ThreadLocal<int>(() => 0);

        [Conditional("SKIN_LOOKUP_DEBUG")]
        internal static void LogLookupDebug(object callingClass, object lookup, LookupDebugType type, [CallerMemberName] string callerMethod = "")
        {
            string icon = string.Empty;
            int level = nested_level.Value;

            switch (type)
            {
                case LookupDebugType.Hit:
                    icon = "🟢 hit";
                    break;

                case LookupDebugType.Miss:
                    icon = "🔴 miss";
                    break;

                case LookupDebugType.Enter:
                    nested_level.Value++;
                    break;

                case LookupDebugType.Exit:
                    nested_level.Value--;
                    if (nested_level.Value == 0)
                        Logger.Log(string.Empty);
                    return;
            }

            string lookupString = lookup.ToString() ?? string.Empty;
            string callingClassString = callingClass.ToString() ?? string.Empty;

            Logger.Log($"{string.Join(null, Enumerable.Repeat("|-", level))}{callingClassString}.{callerMethod}(lookup: {lookupString}) {icon}");
        }

        internal enum LookupDebugType
        {
            Hit,
            Miss,
            Enter,
            Exit
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Audio;
using osu.Game.Database;
using osu.Game.IO;

namespace osu.Game.Skinning
{
    /// <summary>
    /// Provides the fixed bundled Kanna skin and the classic fallback for missing legacy elements.
    /// </summary>
    public sealed class FixedSkinProvider : ISkinSource, IStorageResourceProvider
    {
        private readonly GameHost host;
        private readonly IResourceStore<byte[]> resources;
        private readonly AudioManager audio;
        private readonly RealmAccess realm;
        private readonly ResourceStore<byte[]> files = new ResourceStore<byte[]>();

        public Skin DefaultClassicSkin { get; }
        public Bindable<Skin> CurrentSkin { get; }

        public FixedSkinProvider(RealmAccess realm, GameHost host, IResourceStore<byte[]> resources, AudioManager audio)
        {
            this.realm = realm;
            this.host = host;
            this.resources = resources;
            this.audio = audio;

            DefaultClassicSkin = new DefaultLegacySkin(this);
            CurrentSkin = new Bindable<Skin>(new KannaSkin(this));
            CurrentSkin.ValueChanged += _ => SourceChanged?.Invoke();
        }

        public event Action? SourceChanged;

        public ISkin? FindProvider(Func<ISkin, bool> lookupFunction)
        {
            foreach (var source in AllSources)
            {
                if (lookupFunction(source))
                    return source;
            }

            return null;
        }

        public IEnumerable<ISkin> AllSources
        {
            get
            {
                yield return CurrentSkin.Value;
                yield return DefaultClassicSkin;
            }
        }

        public Drawable? GetDrawableComponent(ISkinComponentLookup lookup) => lookupWithFallback(s => s.GetDrawableComponent(lookup));
        public Texture? GetTexture(string componentName, WrapMode wrapModeS, WrapMode wrapModeT) => lookupWithFallback(s => s.GetTexture(componentName, wrapModeS, wrapModeT));
        public ISample? GetSample(ISampleInfo sampleInfo) => lookupWithFallback(s => s.GetSample(sampleInfo));

        public IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup)
            where TLookup : notnull
            where TValue : notnull
            => lookupWithFallback(s => s.GetConfig<TLookup, TValue>(lookup));

        private T? lookupWithFallback<T>(Func<ISkin, T?> lookup)
            where T : class
        {
            foreach (var source in AllSources)
            {
                if (lookup(source) is T result)
                    return result;
            }

            return null;
        }

        IRenderer IStorageResourceProvider.Renderer => host.Renderer;
        AudioManager IStorageResourceProvider.AudioManager => audio;
        IResourceStore<byte[]> IStorageResourceProvider.Files => files;
        IResourceStore<byte[]> IStorageResourceProvider.Resources => resources;
        RealmAccess IStorageResourceProvider.RealmAccess => realm;
        IResourceStore<TextureUpload> IStorageResourceProvider.CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore) => host.CreateTextureLoaderStore(underlyingStore);
    }
}

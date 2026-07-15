// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Game.Audio;
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Skinning
{
    public class LegacySkin : Skin
    {
        protected virtual bool UseCustomSampleBanks => false;

        public LegacySkin(string name, IStorageResourceProvider? resources)
            : this(name, resources, null)
        {
        }

        protected LegacySkin(string name, IStorageResourceProvider? resources, IResourceStore<byte[]>? fallbackStore, string configurationFilename = @"skin.ini")
            : base(name, resources, fallbackStore, configurationFilename)
        {
        }

        protected override IResourceStore<TextureUpload> CreateTextureLoaderStore(IStorageResourceProvider resources, IResourceStore<byte[]> storage)
            => new LegacyTextureLoaderStore(base.CreateTextureLoaderStore(resources, storage));

        public override IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup)
        {
            bool wasHit = true;

            try
            {
                switch (lookup)
                {
                    case GlobalSkinColours colour:
                        if (colour == GlobalSkinColours.ComboColours)
                        {
                            var comboColours = Configuration.ComboColours;
                            if (comboColours != null)
                                return SkinUtils.As<TValue>(new Bindable<IReadOnlyList<Color4>>(comboColours));
                        }
                        else
                            return SkinUtils.As<TValue>(getCustomColour(Configuration, colour.ToString()));

                        break;

                    case SkinComboColourLookup comboColour:
                        return SkinUtils.As<TValue>(GetComboColour(Configuration, comboColour.ColourIndex, comboColour.Combo));

                    case SkinCustomColourLookup customColour:
                        return SkinUtils.As<TValue>(getCustomColour(Configuration, customColour.Lookup.ToString() ?? string.Empty));

                    case SkinConfiguration.LegacySetting legacy:
                        return legacySettingLookup<TValue>(legacy);

                    default:
                        return genericLookup<TLookup, TValue>(lookup);
                }

                wasHit = false;
                return null;
            }
            finally
            {
                LogLookupDebug(this, lookup, wasHit ? LookupDebugType.Hit : LookupDebugType.Miss);
            }
        }

        protected virtual IBindable<Color4>? GetComboColour(IHasComboColours source, int colourIndex, IHasComboInformation combo)
        {
            var colour = source.ComboColours?[colourIndex % source.ComboColours.Count];
            return colour.HasValue ? new Bindable<Color4>(colour.Value) : null;
        }

        private IBindable<Color4>? getCustomColour(IHasCustomColours source, string lookup)
            => source.CustomColours.TryGetValue(lookup, out var col) ? new Bindable<Color4>(col) : null;

        private IBindable<TValue>? legacySettingLookup<TValue>(SkinConfiguration.LegacySetting legacySetting)
            where TValue : notnull
        {
            switch (legacySetting)
            {
                case SkinConfiguration.LegacySetting.Version:
                    return SkinUtils.As<TValue>(new Bindable<decimal>(Configuration.LegacyVersion ?? SkinConfiguration.LATEST_VERSION));

                case SkinConfiguration.LegacySetting.InputOverlayText:
                    return SkinUtils.As<TValue>(new Bindable<Colour4>(Configuration.CustomColours.TryGetValue(@"InputOverlayText", out var colour) ? colour : Colour4.Black));

                default:
                    return genericLookup<SkinConfiguration.LegacySetting, TValue>(legacySetting);
            }
        }

        private IBindable<TValue>? genericLookup<TLookup, TValue>(TLookup lookup)
            where TLookup : notnull
            where TValue : notnull
        {
            try
            {
                if (Configuration.ConfigDictionary.TryGetValue(lookup.ToString() ?? string.Empty, out string? val))
                {
                    if (typeof(TValue) == typeof(bool))
                        val = bool.TryParse(val, out bool boolVal)
                            ? Convert.ChangeType(boolVal, typeof(bool)).ToString()
                            : Convert.ChangeType(Convert.ToInt32(val), typeof(bool)).ToString();

                    var bindable = new Bindable<TValue>();
                    bindable.Parse(val, CultureInfo.InvariantCulture);
                    return bindable;
                }
            }
            catch
            {
            }

            return null;
        }

        public override Drawable? GetDrawableComponent(ISkinComponentLookup lookup)
        {
            switch (lookup)
            {
                case GlobalSkinnableContainerLookup containerLookup when containerLookup.Lookup == GlobalSkinnableContainers.MainHUDComponents:
                    if (containerLookup.Ruleset != null)
                    {
                        return new DefaultSkinComponentsContainer(container =>
                        {
                            var combo = container.OfType<LegacyDefaultComboCounter>().FirstOrDefault();
                            if (combo != null)
                            {
                                combo.Anchor = Anchor.BottomLeft;
                                combo.Origin = Anchor.BottomLeft;
                                combo.Scale = new Vector2(1.28f);
                            }
                        }) { new LegacyDefaultComboCounter() };
                    }

                    return new DefaultSkinComponentsContainer(container =>
                    {
                        var score = container.OfType<LegacyScoreCounter>().FirstOrDefault();
                        var accuracy = container.OfType<GameplayAccuracyCounter>().FirstOrDefault();
                        if (score != null && accuracy != null)
                            accuracy.Y = container.ToLocalSpace(score.ScreenSpaceDrawQuad.BottomRight).Y;

                        var songProgress = container.OfType<LegacySongProgress>().FirstOrDefault();
                        if (songProgress != null && accuracy != null)
                        {
                            songProgress.Anchor = Anchor.TopRight;
                            songProgress.Origin = Anchor.CentreRight;
                            songProgress.X = -accuracy.ScreenSpaceDeltaToParentSpace(accuracy.ScreenSpaceDrawQuad.Size).X - 18;
                            songProgress.Y = container.ToLocalSpace(accuracy.ScreenSpaceDrawQuad.TopLeft).Y + accuracy.ScreenSpaceDeltaToParentSpace(accuracy.ScreenSpaceDrawQuad.Size).Y / 2;
                        }

                        var hitError = container.OfType<HitErrorMeter>().FirstOrDefault();
                        if (hitError != null)
                        {
                            hitError.Anchor = Anchor.BottomCentre;
                            hitError.Origin = Anchor.CentreLeft;
                            hitError.Rotation = -90;
                        }
                    })
                    {
                        Children = new Drawable[]
                        {
                            new LegacyScoreCounter(), new LegacyAccuracyCounter(), new LegacySongProgress(), new BarHitErrorMeter(), new LegacyHealthDisplay(),
                        }
                    };

                case SkinComponentLookup<HitResult> resultComponent:
                    if (getJudgementAnimation(resultComponent.Component) != null)
                    {
                        Func<Drawable> createDrawable = () => getJudgementAnimation(resultComponent.Component).AsNonNull();
                        var particle = getParticleTexture(resultComponent.Component);
                        return particle != null
                            ? new LegacyJudgementPieceNew(resultComponent.Component, createDrawable, particle)
                            : new LegacyJudgementPieceOld(resultComponent.Component, createDrawable);
                    }

                    return null;
            }

            return base.GetDrawableComponent(lookup);
        }

        private Texture? getParticleTexture(HitResult result) => result switch
        {
            HitResult.Meh => GetTexture("particle50"),
            HitResult.Ok => GetTexture("particle100"),
            HitResult.Great => GetTexture("particle300"),
            _ => null,
        };

        private Drawable? getJudgementAnimation(HitResult result) => result switch
        {
            HitResult.Miss => this.GetAnimation("hit0", true, false),
            HitResult.LargeTickMiss => this.GetAnimation("slidertickmiss", true, false),
            HitResult.IgnoreMiss => this.GetAnimation("sliderendmiss", true, false),
            HitResult.Meh => this.GetAnimation("hit50", true, false),
            HitResult.Ok => this.GetAnimation("hit100", true, false),
            HitResult.Great => this.GetAnimation("hit300", true, false),
            _ => null,
        };

        protected virtual bool AllowHighResolutionSprites => true;

        public override Texture? GetTexture(string componentName, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            float ratio = 1;
            Texture? texture = null;
            if (AllowHighResolutionSprites)
            {
                componentName = componentName.Replace(@"@2x", string.Empty);
                texture = Textures?.Get($"{Path.ChangeExtension(componentName, null)}@2x{Path.GetExtension(componentName)}", wrapModeS, wrapModeT);
                if (texture != null) ratio = 2;
            }

            texture ??= Textures?.Get(componentName, wrapModeS, wrapModeT);
            if (texture != null) texture.ScaleAdjust = ratio;
            return texture;
        }

        public override ISample? GetSample(ISampleInfo sampleInfo)
        {
            IEnumerable<string> lookupNames = sampleInfo is HitSampleInfo hitSample ? getLegacyLookupNames(hitSample) : sampleInfo.LookupNames.SelectMany(getFallbackSampleNames);
            foreach (string lookup in lookupNames)
            {
                var sample = Samples?.Get(lookup);
                if (sample != null) return sample;
            }

            return null;
        }

        private IEnumerable<string> getLegacyLookupNames(HitSampleInfo hitSample)
        {
            var lookupNames = hitSample.LookupNames.SelectMany(getFallbackSampleNames);
            if (!string.IsNullOrEmpty(hitSample.Suffix))
                lookupNames = UseCustomSampleBanks
                    ? lookupNames.Where(name => name.EndsWith(hitSample.Suffix, StringComparison.Ordinal))
                    : lookupNames.Where(name => !name.EndsWith(hitSample.Suffix, StringComparison.Ordinal));

            foreach (string lookup in lookupNames) yield return lookup;
            yield return hitSample.Name;
        }

        private IEnumerable<string> getFallbackSampleNames(string name)
        {
            yield return name;
            yield return name.Split('/').Last();
        }
    }
}

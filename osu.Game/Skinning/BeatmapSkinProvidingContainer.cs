// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Audio;

namespace osu.Game.Skinning
{
    /// <summary>
    /// A container which overrides existing skin options with beatmap-local values.
    /// </summary>
    public partial class BeatmapSkinProvidingContainer : SkinProvidingContainer
    {
        public BindableWithCurrent<bool> BeatmapSkins = new BindableWithCurrent<bool>(true);
        public BindableWithCurrent<bool> BeatmapColours = new BindableWithCurrent<bool>(true);
        public BindableWithCurrent<bool> BeatmapHitsounds = new BindableWithCurrent<bool>(true);

        protected override bool AllowConfigurationLookup => BeatmapSkins.Value;

        protected override bool AllowColourLookup => BeatmapColours.Value;

        protected override bool AllowDrawableLookup(ISkinComponentLookup lookup) => BeatmapSkins.Value;

        protected override bool AllowTextureLookup(string componentName) => BeatmapSkins.Value;

        protected override bool AllowSampleLookup(ISampleInfo sampleInfo) => BeatmapHitsounds.Value;

        private readonly ISkin skin;
        private readonly ISkin? classicFallback;

        private Bindable<Skin> currentSkin = null!;

        public BeatmapSkinProvidingContainer(ISkin skin, ISkin? classicFallback = null)
            : base(skin)
        {
            this.skin = skin;
            this.classicFallback = classicFallback;
        }

        [BackgroundDependencyLoader]
        private void load(FixedSkinProvider skins)
        {
            BeatmapSkins.BindValueChanged(_ => TriggerSourceChanged());
            BeatmapColours.BindValueChanged(_ => TriggerSourceChanged());
            BeatmapHitsounds.BindValueChanged(_ => TriggerSourceChanged());

            currentSkin = skins.CurrentSkin.GetBoundCopy();
            currentSkin.BindValueChanged(_ =>
            {
                bool beatmapProvidingResources = skin is LegacySkinTransformer legacySkin && legacySkin.IsProvidingLegacyResources;

                if (beatmapProvidingResources && classicFallback != null)
                    SetSources(new[] { skin, classicFallback });
                else
                    SetSources(new[] { skin });
            }, true);
        }
    }
}

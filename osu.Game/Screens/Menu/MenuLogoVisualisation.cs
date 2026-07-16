// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    public partial class MenuLogoVisualisation : LogoVisualisation
    {
        private Bindable<Skin> skin = null!;

        [BackgroundDependencyLoader]
        private void load(FixedSkinProvider skinProvider)
        {
            skin = skinProvider.CurrentSkin.GetBoundCopy();

            skin.BindValueChanged(_ => UpdateColour(), true);
        }

        protected virtual void UpdateColour()
        {
            Colour = skin.Value.GetConfig<GlobalSkinColours, Color4>(GlobalSkinColours.MenuGlow)?.Value ?? Color4.White;
        }
    }
}

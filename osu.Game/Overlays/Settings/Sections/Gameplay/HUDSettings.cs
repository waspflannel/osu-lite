// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Gameplay
{
    public partial class HUDSettings : SettingsSubsection
    {
        protected override LocalisableString Header => GameplaySettingsStrings.HUDHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<HUDVisibilityMode>
                {
                    Caption = GameplaySettingsStrings.HUDVisibilityMode,
                    Current = config.GetBindable<HUDVisibilityMode>(OsuSetting.HUDVisibilityMode)
                }),
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = GameplaySettingsStrings.AlwaysShowKeyOverlay,
                    Current = config.GetBindable<bool>(OsuSetting.KeyOverlay),
                })
                {
                    Keywords = new[] { "counter" },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = GameplaySettingsStrings.AlwaysShowGameplayLeaderboard,
                    Current = config.GetBindable<bool>(OsuSetting.GameplayLeaderboard),
                }),
                
            };
        }
    }
}

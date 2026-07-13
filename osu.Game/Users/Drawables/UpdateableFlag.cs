// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Users.Drawables
{
    public partial class UpdateableFlag : ModelBackedDrawable<CountryCode>
    {
        private CountryCode countryCode;

        public CountryCode CountryCode
        {
            get => countryCode;
            set
            {
                countryCode = value;
                updateModel();
            }
        }

        /// <summary>
        /// Perform an action when the flag is clicked.
        /// </summary>
        public Action? Action;

        private readonly Bindable<bool> hideFlags = new BindableBool();

        public UpdateableFlag(CountryCode countryCode = CountryCode.Unknown)
        {
            CountryCode = countryCode;
            hideFlags.BindValueChanged(_ => updateModel());
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.HideCountryFlags, hideFlags);
        }

        protected override Drawable CreateDrawable(CountryCode countryCode)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new DrawableFlag(countryCode)
                    {
                        RelativeSizeAxes = Axes.Both
                    },
                    new HoverClickSounds()
                }
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke();
            return true;
        }

        private void updateModel() => Model = hideFlags.Value ? CountryCode.Unknown : countryCode;
    }
}

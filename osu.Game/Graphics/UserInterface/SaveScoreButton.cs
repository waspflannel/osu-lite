// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Game.Graphics.UserInterface
{
    public enum ScoreSaveState
    {
        NeedsSaving,
        Saving,
        Saved,
    }

    public partial class SaveScoreButton : GrayButton
    {
        [Resolved]
        private OsuColour colours { get; set; }

        public readonly Bindable<ScoreSaveState> State = new Bindable<ScoreSaveState>();

        private SpriteIcon checkmark;

        public SaveScoreButton()
            : base(FontAwesome.Solid.Save)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(checkmark = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                X = 8,
                Size = Vector2.Zero,
                Icon = FontAwesome.Solid.Check,
            });

            State.BindValueChanged(updateState, true);
        }

        private void updateState(ValueChangedEvent<ScoreSaveState> state)
        {
            switch (state.NewValue)
            {
                case ScoreSaveState.NeedsSaving:
                    Background.FadeColour(colours.Gray4, 500, Easing.InOutExpo);
                    Icon.MoveToX(0, 500, Easing.InOutExpo);
                    checkmark.ScaleTo(Vector2.Zero, 500, Easing.InOutExpo);
                    TooltipText = @"save score";
                    break;

                case ScoreSaveState.Saving:
                    Background.FadeColour(colours.Yellow, 500, Easing.InOutExpo);
                    TooltipText = @"saving score";
                    break;

                case ScoreSaveState.Saved:
                    Background.FadeColour(colours.Green, 500, Easing.InOutExpo);
                    Icon.MoveToX(-8, 500, Easing.InOutExpo);
                    checkmark.ScaleTo(new Vector2(13), 500, Easing.InOutExpo);
                    break;
            }
        }
    }
}

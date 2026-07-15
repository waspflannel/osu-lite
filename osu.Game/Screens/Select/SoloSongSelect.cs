// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Play;
using osu.Game.Utils;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Select
{
    public partial class SoloSongSelect : SongSelect
    {
        private PlayerLoader? playerLoader;
        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        private Sample? sampleConfirmSelection { get; set; }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleConfirmSelection = audio.Samples.Get(@"SongSelect/confirm-selection");
        }

        public override IEnumerable<OsuMenuItem> GetForwardActions(BeatmapInfo beatmap)
        {
            yield return new OsuMenuItem(ButtonSystemStrings.Play.ToSentence(), MenuItemType.Highlighted, () => SelectAndRun(beatmap, OnStart)) { Icon = FontAwesome.Solid.Check };

            yield return new OsuMenuItemSpacer();

            if (beatmap.LastPlayed == null)
                yield return new OsuMenuItem(SongSelectStrings.MarkAsPlayed, MenuItemType.Standard, () => beatmaps.MarkPlayed(beatmap)) { Icon = FontAwesome.Solid.TimesCircle };
            else
                yield return new OsuMenuItem(SongSelectStrings.RemoveFromPlayed, MenuItemType.Standard, () => beatmaps.MarkNotPlayed(beatmap)) { Icon = FontAwesome.Solid.TimesCircle };

            yield return new OsuMenuItem(SongSelectStrings.ClearAllLocalScores, MenuItemType.Standard, () => dialogOverlay?.Push(new BeatmapClearScoresDialog(beatmap)))
            {
                Icon = FontAwesome.Solid.Eraser
            };

            if (beatmaps.CanHide(beatmap))
                yield return new OsuMenuItem(WebCommonStrings.ButtonsHide.ToSentence(), MenuItemType.Destructive, () => beatmaps.Hide(beatmap));
        }

        protected override void OnStart()
        {
            if (playerLoader != null) return;

            bool autoplay = GetContainingInputManager()?.CurrentState?.Keyboard.ControlPressed == true;

            sampleConfirmSelection?.Play();

            this.Push(playerLoader = new PlayerLoader(() => createPlayer(autoplay)));

            Player createPlayer(bool autoplay)
            {
                if (!autoplay)
                    return new SoloPlayer();

                var replay = Ruleset.Value.CreateInstance().CreateAutoplayScore(Beatmap.Value.Beatmap);
                return replay == null ? new SoloPlayer() : new ReplayPlayer(replay, autoplayPlayback: true);
            }
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            playerLoader = null;
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            if (base.OnExiting(e))
                return true;

            playerLoader = null;
            return false;
        }

        private partial class PlayerLoader : Play.PlayerLoader
        {
            public override bool ShowFooter => !QuickRestart;

            public PlayerLoader(Func<Player> createPlayer)
                : base(createPlayer)
            {
            }
        }
    }
}

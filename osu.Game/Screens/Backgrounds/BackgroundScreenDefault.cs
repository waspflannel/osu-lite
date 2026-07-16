// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Backgrounds;

namespace osu.Game.Screens.Backgrounds
{
    public partial class BackgroundScreenDefault : BackgroundScreen
    {
        private Background background;

        private int currentDisplay;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; }

        [Resolved]
        private GameHost gameHost { get; set; }

        protected virtual bool AllowStoryboardBackground => true;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.ValueChanged += _ => Scheduler.AddOnce(next);

            Next();

            // helper function required for AddOnce usage.
            void next() => Next();
        }

        private ScheduledDelegate storyboardUnloadDelegate;

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            var backgroundScreenStack = Parent as BackgroundScreenStack;
            Debug.Assert(backgroundScreenStack != null);

            if (background is BeatmapBackgroundWithStoryboard storyboardBackground)
                storyboardUnloadDelegate = gameHost.UpdateThread.Scheduler.AddDelayed(storyboardBackground.UnloadStoryboard, TRANSITION_LENGTH);

            base.OnSuspending(e);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            if (background is BeatmapBackgroundWithStoryboard storyboardBackground)
            {
                if (storyboardUnloadDelegate?.Completed == false)
                    storyboardUnloadDelegate.Cancel();
                else
                    storyboardBackground.LoadStoryboard();

                storyboardUnloadDelegate = null;
            }

            base.OnResuming(e);
        }

        private ScheduledDelegate nextTask;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Request loading the next background.
        /// </summary>
        /// <returns>Whether a new background was queued for load. May return false if the current background is still valid.</returns>
        public virtual bool Next()
        {
            var nextBackground = createBackground();

            // in the case that the background hasn't changed, we want to avoid cancelling any tasks that could still be loading.
            if (nextBackground == background)
                return false;

            Logger.Log(@"🌅 Global background change queued");

            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            nextTask?.Cancel();
            nextTask = Scheduler.AddDelayed(() =>
            {
                Logger.Log(@"🌅 Global background loading");
                LoadComponentAsync(nextBackground, displayNext, cancellationTokenSource.Token);
            }, 500);

            return true;
        }

        private void displayNext(Background newBackground)
        {
            background?.FadeOut(800, Easing.OutQuint);
            background?.Expire();

            AddInternal(background = newBackground);
            currentDisplay++;
        }

        private Background createBackground()
        {
            Background newBackground = AllowStoryboardBackground
                ? new BeatmapBackgroundWithStoryboard(beatmap.Value, getBackgroundTextureName())
                : new BeatmapBackground(beatmap.Value, getBackgroundTextureName());

            // this method is called in many cases where the background might not necessarily need to change.
            // if an equivalent background is currently being shown, we don't want to load it again.
            if (newBackground.Equals(background))
                return background;

            newBackground.Depth = currentDisplay;

            return newBackground;
        }

        private string getBackgroundTextureName() => @"Menu/menu-background-1";
    }
}

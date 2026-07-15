// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge : VisibilityContainer
    {
        private const float corner_radius = 10;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        public float TopPadding { get; init; }

        protected override bool StartHidden => true;

        private OsuHoverContainer titleLink = null!;
        private MarqueeContainer titleLabel = null!;
        private OsuHoverContainer artistLink = null!;
        private MarqueeContainer artistLabel = null!;

        internal string DisplayedTitle { get; private set; } = string.Empty;
        internal string DisplayedArtist { get; private set; } = string.Empty;

        private Statistic lengthStatistic = null!;
        private Statistic bpmStatistic = null!;

        [Resolved]
        private ISongSelect? songSelect { get; set; }

        [Resolved]
        private LocalisationManager localisation { get; set; } = null!;

        private FillFlowContainer statisticsFlow = null!;

        public BeatmapTitleWedge()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Masking = true;
            CornerRadius = corner_radius;

            InternalChildren = new Drawable[]
            {
                new WedgeBackground(),
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding
                    {
                        Top = SongSelect.WEDGE_CONTENT_MARGIN + TopPadding,
                        Left = SongSelect.WEDGE_CONTENT_MARGIN
                    },
                    Spacing = new Vector2(0f, 4f),
                    Children = new Drawable[]
                    {
                        new ShearAligningWrapper(new Container
                        {
                            Shear = -OsuGame.SHEAR,
                            RelativeSizeAxes = Axes.X,
                            Height = OsuFont.Style.Title.Size,
                            Margin = new MarginPadding { Bottom = -4f },
                            Child = titleLink = new OsuHoverContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Child = titleLabel = new MarqueeContainer
                                {
                                    OverflowSpacing = 50,
                                }
                            }
                        }),
                        new ShearAligningWrapper(new Container
                        {
                            Shear = -OsuGame.SHEAR,
                            RelativeSizeAxes = Axes.X,
                            Height = OsuFont.Style.Heading2.Size,
                            Margin = new MarginPadding { Left = 1f },
                            Child = artistLink = new OsuHoverContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Child = artistLabel = new MarqueeContainer
                                {
                                    OverflowSpacing = 50,
                                }
                            }
                        }),
                        new ShearAligningWrapper(statisticsFlow = new FillFlowContainer
                        {
                            Shear = -OsuGame.SHEAR,
                            AutoSizeAxes = Axes.X,
                            Height = 30,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(2f, 0f),
                            Children = new Drawable[]
                            {
                                lengthStatistic = new Statistic(OsuIcon.Clock, background: true, leftPadding: SongSelect.WEDGE_CONTENT_MARGIN, minSize: 50f)
                                {
                                    Margin = new MarginPadding { Left = -SongSelect.WEDGE_CONTENT_MARGIN },
                                },
                                bpmStatistic = new Statistic(OsuIcon.Metronome)
                                {
                                    TooltipText = BeatmapsetsStrings.ShowStatsBpm,
                                    Margin = new MarginPadding { Left = 5f },
                                },
                            },
                        }),
                        new ShearAligningWrapper(new Container
                        {
                            Shear = -OsuGame.SHEAR,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Margin = new MarginPadding { Left = -SongSelect.WEDGE_CONTENT_MARGIN },
                            Padding = new MarginPadding { Right = -SongSelect.WEDGE_CONTENT_MARGIN },
                            Child = new DifficultyDisplay(),
                        }),
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            working.BindValueChanged(_ => updateDisplay());
            ruleset.BindValueChanged(_ => updateDisplay());

            updateDisplay();

            statisticsFlow.AutoSizeDuration = 100;
            statisticsFlow.AutoSizeEasing = Easing.OutQuint;
        }

        protected override void PopIn()
        {
            this.MoveToX(0, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeIn(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        protected override void PopOut()
        {
            this.MoveToX(-150, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeOut(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        private void updateDisplay()
        {
            var metadata = working.Value.Metadata;
            var beatmapInfo = working.Value.BeatmapInfo;


            var titleText = new RomanisableString(metadata.TitleUnicode, metadata.Title);
            titleLabel.CreateContent = () => new OsuSpriteText
            {
                Text = titleText,
                Shadow = true,
                Font = OsuFont.Style.Title,
            };
            titleLink.Action = () => songSelect?.Search(titleText.GetPreferred(localisation.CurrentParameters.Value.PreferOriginalScript));
            DisplayedTitle = titleText.ToString();

            var artistText = new RomanisableString(metadata.ArtistUnicode, metadata.Artist);
            artistLabel.CreateContent = () => new OsuSpriteText
            {
                Text = artistText,
                Shadow = true,
                Font = OsuFont.Style.Heading2,
            };
            artistLink.Action = () => songSelect?.Search(artistText.GetPreferred(localisation.CurrentParameters.Value.PreferOriginalScript));
            DisplayedArtist = artistText.ToString();

            updateLengthAndBpmStatistics();
        }

        private CancellationTokenSource? lengthBpmCancellationSource;

        private void updateLengthAndBpmStatistics()
        {
            lengthBpmCancellationSource?.Cancel();
            lengthBpmCancellationSource = new CancellationTokenSource();

            var token = lengthBpmCancellationSource.Token;

            Task.Run(() =>
            {
                var beatmapInfo = working.Value.BeatmapInfo;
                // This can take time as it is a synchronous task.
                var beatmap = working.Value.Beatmap;

                int bpmMax = FormatUtils.RoundBPM(beatmap.ControlPointInfo.BPMMaximum);
                int bpmMin = FormatUtils.RoundBPM(beatmap.ControlPointInfo.BPMMinimum);
                int mostCommonBPM = FormatUtils.RoundBPM(60000 / beatmap.GetMostCommonBeatLength());

                double drainLength = Math.Round(beatmap.CalculateDrainLength());
                double hitLength = Math.Round(beatmapInfo.Length);

                Schedule(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    lengthStatistic.Text = hitLength.ToFormattedDuration();
                    lengthStatistic.TooltipText = BeatmapsetsStrings.ShowStatsTotalLength(drainLength.ToFormattedDuration());

                    bpmStatistic.Text = bpmMin == bpmMax
                        ? $"{bpmMin}"
                        : LocalisableString.Interpolate($"{bpmMin}-{bpmMax} ({SongSelectStrings.MostlyBPM(mostCommonBPM)})");
                });
            }, token);
        }

    }
}

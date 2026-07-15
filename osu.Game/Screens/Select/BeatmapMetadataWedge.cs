// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Resources.Localisation.Web;
using osuTK;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapMetadataWedge : VisibilityContainer
    {
        private MetadataDisplay creator = null!;
        private MetadataDisplay source = null!;
        private MetadataDisplay mapperTags = null!;

        protected override bool StartHidden => true;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private ISongSelect? songSelect { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Padding = new MarginPadding { Top = 4f };

            Width = 0.9f;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0f, 4f),
                Shear = OsuGame.SHEAR,
                Children = new[]
                {
                    new ShearAligningWrapper(new Container
                    {
                        CornerRadius = 10,
                        Masking = true,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new WedgeBackground(),
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Shear = -OsuGame.SHEAR,
                                Padding = new MarginPadding { Left = SongSelect.WEDGE_CONTENT_MARGIN, Right = 35, Vertical = 16 },
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0f, 10f),
                                        AutoSizeDuration = (float)transition_duration / 3,
                                        AutoSizeEasing = Easing.OutQuint,
                                        Children = new Drawable[]
                                        {
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[]
                                                {
                                                    new Dimension(),
                                                    new Dimension(),
                                                },
                                                Content = new[]
                                                {
                                                    new[]
                                                    {
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(0f, 10f),
                                                            Children = new[]
                                                            {
                                                                creator = new MetadataDisplay(EditorSetupStrings.Creator),
                                                            },
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(0f, 10f),
                                                            Children = new[]
                                                            {
                                                                source = new MetadataDisplay(BeatmapsetsStrings.ShowInfoSource),
                                                            },
                                                        },
                                                    },
                                                },
                                            },
                                            mapperTags = new MetadataDisplay(BeatmapsetsStrings.ShowInfoMapperTags),
                                        },
                                    },
                                },
                            },
                        },
                    }),
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            beatmap.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay), true);
        }

        private const double transition_duration = 300;

        protected override void PopIn()
        {
            this.FadeIn(transition_duration, Easing.OutQuint)
                .MoveToX(0, transition_duration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(transition_duration, Easing.OutQuint)
                .MoveToX(-100, transition_duration, Easing.OutQuint);
        }

        private void updateDisplay()
        {
            var metadata = beatmap.Value.Metadata;

            // osu! lite is offline: mapper identity is plain local metadata, not a clickable profile link.
            creator.Data = (metadata.Author.Username, null);

            if (!string.IsNullOrEmpty(metadata.Source))
                source.Data = (metadata.Source, () => songSelect?.Search(metadata.Source));
            else
                source.Data = ("-", null);

            if (!string.IsNullOrEmpty(metadata.Tags))
                mapperTags.Tags = (metadata.Tags.Split(' '), t => songSelect?.Search(t));
            else
                mapperTags.Tags = (Array.Empty<string>(), _ => { });

        }

    }
}

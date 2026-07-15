// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Carousel;
using osu.Game.Utils;

namespace osu.Game.Screens.Select
{
    public class BeatmapCarouselFilterMatching : ICarouselFilter
    {
        private readonly Func<FilterCriteria> getCriteria;

        public int BeatmapItemsCount { get; private set; }

        public BeatmapCarouselFilterMatching(Func<FilterCriteria> getCriteria)
        {
            this.getCriteria = getCriteria;
        }

        public async Task<List<CarouselItem>> Run(IEnumerable<CarouselItem> items, CancellationToken cancellationToken) => await Task.Run(() =>
        {
            var criteria = getCriteria();

            return matchItems(items, criteria).ToList();
        }, cancellationToken).ConfigureAwait(false);

        private IEnumerable<CarouselItem> matchItems(IEnumerable<CarouselItem> items, FilterCriteria criteria)
        {
            int countMatching = 0;

            foreach (var item in items)
            {
                var beatmap = (BeatmapInfo)item.Model;

                if (beatmap.Hidden)
                    continue;

                if (!CheckCriteriaMatch(beatmap, criteria))
                    continue;

                countMatching++;
                yield return item;
            }

            BeatmapItemsCount = countMatching;
        }

        public static bool CheckCriteriaMatch(BeatmapInfo beatmap, FilterCriteria criteria)
        {
            bool match = criteria.Ruleset == null || beatmap.AllowGameplayWithRuleset(criteria.Ruleset!, criteria.AllowConvertedBeatmaps);

            if (criteria.SelectedBeatmapSet != null)
            {
                // only check ruleset equality or convertability for selected beatmap
                return beatmap.BeatmapSet?.Equals(criteria.SelectedBeatmapSet) == true && match;
            }

            if (!match) return false;

            if (criteria.SearchTerms.Length > 0)
            {
                match = beatmap.Match(criteria.SearchTerms);

            }

            if (!match) return false;

            match &= !criteria.StarDifficulty.HasFilter || criteria.StarDifficulty.IsInRange(beatmap.StarRating.FloorToDecimalDigits(2));
            match &= !criteria.ApproachRate.HasFilter || criteria.ApproachRate.IsInRange(beatmap.Difficulty.ApproachRate);
            match &= !criteria.DrainRate.HasFilter || criteria.DrainRate.IsInRange(beatmap.Difficulty.DrainRate);
            match &= !criteria.CircleSize.HasFilter || criteria.CircleSize.IsInRange(beatmap.Difficulty.CircleSize);
            match &= !criteria.OverallDifficulty.HasFilter || criteria.OverallDifficulty.IsInRange(beatmap.Difficulty.OverallDifficulty);
            match &= !criteria.Length.HasFilter || criteria.Length.IsInRange(beatmap.Length);
            match &= !criteria.LastPlayed.HasFilter || criteria.LastPlayed.IsInRange(beatmap.LastPlayed ?? DateTimeOffset.MinValue);
            match &= !criteria.BPM.HasFilter || criteria.BPM.IsInRange(beatmap.BPM);

            match &= !criteria.BeatDivisor.HasFilter || criteria.BeatDivisor.IsInRange(beatmap.BeatDivisor);
            if (!match) return false;

            match &= !criteria.Creator.HasFilter || criteria.Creator.Matches(beatmap.Metadata.Creator);

            if (criteria.Artist.HasFilter)
            {
                if (criteria.Artist.ExcludeTerm)
                    match &= criteria.Artist.Matches(beatmap.Metadata.Artist) && criteria.Artist.Matches(beatmap.Metadata.ArtistUnicode);
                else
                    match &= criteria.Artist.Matches(beatmap.Metadata.Artist) || criteria.Artist.Matches(beatmap.Metadata.ArtistUnicode);
            }

            if (criteria.Title.HasFilter)
            {
                if (criteria.Title.ExcludeTerm)
                    match &= criteria.Title.Matches(beatmap.Metadata.Title) && criteria.Title.Matches(beatmap.Metadata.TitleUnicode);
                else
                    match &= criteria.Title.Matches(beatmap.Metadata.Title) || criteria.Title.Matches(beatmap.Metadata.TitleUnicode);
            }

            match &= !criteria.DifficultyName.HasFilter || criteria.DifficultyName.Matches(beatmap.DifficultyName);
            match &= !criteria.Source.HasFilter || criteria.Source.Matches(beatmap.Metadata.Source);

            match &= !criteria.UserStarDifficulty.HasFilter || criteria.UserStarDifficulty.IsInRange(beatmap.StarRating);

            if (!match) return false;

            if (match && criteria.RulesetCriteria != null)
                match &= criteria.RulesetCriteria.Matches(beatmap, criteria);

            return match;
        }
    }
}

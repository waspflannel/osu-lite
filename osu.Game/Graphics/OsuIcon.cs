// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Text;

namespace osu.Game.Graphics
{
    public static class OsuIcon
    {
        public const string FONT_NAME = @"Icons";

        // ruleset icons
        public static IconUsage RulesetOsu => get(OsuIconMapping.RulesetOsu);
        public static IconUsage RulesetMania => get(OsuIconMapping.RulesetMania);
        public static IconUsage RulesetCatch => get(OsuIconMapping.RulesetCatch);
        public static IconUsage RulesetTaiko => get(OsuIconMapping.RulesetTaiko);

        public static IconUsage Logo => get(OsuIconMapping.Logo);
        public static IconUsage EditCircle => get(OsuIconMapping.EditCircle);
        public static IconUsage LeftCircle => get(OsuIconMapping.LeftCircle);
        public static IconUsage RightCircle => get(OsuIconMapping.RightCircle);
        public static IconUsage Undo => get(OsuIconMapping.Undo);

        public static IconUsage Audio => get(OsuIconMapping.Audio);
        public static IconUsage Beatmap => get(OsuIconMapping.Beatmap);
        public static IconUsage Calendar => get(OsuIconMapping.Calendar);
        public static IconUsage ChangelogA => get(OsuIconMapping.ChangelogA);
        public static IconUsage ChangelogB => get(OsuIconMapping.ChangelogB);
        public static IconUsage Chat => get(OsuIconMapping.Chat);
        public static IconUsage CheckCircle => get(OsuIconMapping.CheckCircle);
        public static IconUsage Clock => get(OsuIconMapping.Clock);
        public static IconUsage CollapseA => get(OsuIconMapping.CollapseA);
        public static IconUsage Collections => get(OsuIconMapping.Collections);
        public static IconUsage Cross => get(OsuIconMapping.Cross);
        public static IconUsage CrossCircle => get(OsuIconMapping.CrossCircle);
        public static IconUsage Crown => get(OsuIconMapping.Crown);
        public static IconUsage DailyChallenge => get(OsuIconMapping.DailyChallenge);
        public static IconUsage Debug => get(OsuIconMapping.Debug);
        public static IconUsage Delete => get(OsuIconMapping.Delete);
        public static IconUsage Details => get(OsuIconMapping.Details);
        public static IconUsage Discord => get(OsuIconMapping.Discord);
        public static IconUsage EllipsisHorizontal => get(OsuIconMapping.EllipsisHorizontal);
        public static IconUsage EllipsisVertical => get(OsuIconMapping.EllipsisVertical);
        public static IconUsage ExpandA => get(OsuIconMapping.ExpandA);
        public static IconUsage ExpandB => get(OsuIconMapping.ExpandB);
        public static IconUsage FeaturedArtist => get(OsuIconMapping.FeaturedArtist);
        public static IconUsage FeaturedArtistCircle => get(OsuIconMapping.FeaturedArtistCircle);
        public static IconUsage GameplayA => get(OsuIconMapping.GameplayA);
        public static IconUsage GameplayB => get(OsuIconMapping.GameplayB);
        public static IconUsage GameplayC => get(OsuIconMapping.GameplayC);
        public static IconUsage Global => get(OsuIconMapping.Global);
        public static IconUsage Graphics => get(OsuIconMapping.Graphics);
        public static IconUsage Heart => get(OsuIconMapping.Heart);
        public static IconUsage Home => get(OsuIconMapping.Home);
        public static IconUsage Input => get(OsuIconMapping.Input);
        public static IconUsage Maintenance => get(OsuIconMapping.Maintenance);
        public static IconUsage Megaphone => get(OsuIconMapping.Megaphone);
        public static IconUsage Metronome => get(OsuIconMapping.Metronome);
        public static IconUsage Music => get(OsuIconMapping.Music);
        public static IconUsage News => get(OsuIconMapping.News);
        public static IconUsage Next => get(OsuIconMapping.Next);
        public static IconUsage NextCircle => get(OsuIconMapping.NextCircle);
        public static IconUsage Notification => get(OsuIconMapping.Notification);
        public static IconUsage Online => get(OsuIconMapping.Online);
        public static IconUsage Play => get(OsuIconMapping.Play);
        public static IconUsage Player => get(OsuIconMapping.Player);
        public static IconUsage PlayerFollow => get(OsuIconMapping.PlayerFollow);
        public static IconUsage Prev => get(OsuIconMapping.Prev);
        public static IconUsage PrevCircle => get(OsuIconMapping.PrevCircle);
        public static IconUsage Ranking => get(OsuIconMapping.Ranking);
        public static IconUsage Rulesets => get(OsuIconMapping.Rulesets);
        public static IconUsage Search => get(OsuIconMapping.Search);
        public static IconUsage Settings => get(OsuIconMapping.Settings);
        public static IconUsage SkinA => get(OsuIconMapping.SkinA);
        public static IconUsage SkinB => get(OsuIconMapping.SkinB);
        public static IconUsage Star => get(OsuIconMapping.Star);
        public static IconUsage Storyboard => get(OsuIconMapping.Storyboard);
        public static IconUsage Team => get(OsuIconMapping.Team);
        public static IconUsage ThumbsUp => get(OsuIconMapping.ThumbsUp);
        public static IconUsage Tournament => get(OsuIconMapping.Tournament);
        public static IconUsage Twitter => get(OsuIconMapping.Twitter);
        public static IconUsage UserInterface => get(OsuIconMapping.UserInterface);
        public static IconUsage Wiki => get(OsuIconMapping.Wiki);
        public static IconUsage EditorHitCircle => get(OsuIconMapping.EditorHitCircle);
        public static IconUsage EditorSlider => get(OsuIconMapping.EditorSlider);
        public static IconUsage EditorSpinner => get(OsuIconMapping.EditorSpinner);
        public static IconUsage EditorHit => get(OsuIconMapping.EditorHit);
        public static IconUsage EditorDrumRoll => get(OsuIconMapping.EditorDrumRoll);
        public static IconUsage EditorSwell => get(OsuIconMapping.EditorSwell);
        public static IconUsage EditorFruit => get(OsuIconMapping.EditorFruit);
        public static IconUsage EditorJuiceStream => get(OsuIconMapping.EditorJuiceStream);
        public static IconUsage EditorNote => get(OsuIconMapping.EditorNote);
        public static IconUsage EditorHoldNote => get(OsuIconMapping.EditorHoldNote);
        public static IconUsage EditorBananaShower => get(OsuIconMapping.EditorBananaShower);
        public static IconUsage EditorGrid => get(OsuIconMapping.EditorGrid);
        public static IconUsage EditorAddControlPoint => get(OsuIconMapping.EditorAddControlPoint);
        public static IconUsage EditorConvertToStream => get(OsuIconMapping.EditorConvertToStream);
        public static IconUsage EditorDistanceSnap => get(OsuIconMapping.EditorDistanceSnap);
        public static IconUsage EditorFinish => get(OsuIconMapping.EditorFinish);
        public static IconUsage EditorGridSnap => get(OsuIconMapping.EditorGridSnap);
        public static IconUsage EditorNewComboSparkles => get(OsuIconMapping.EditorNewComboSparkles);
        public static IconUsage EditorSelect => get(OsuIconMapping.EditorSelect);
        public static IconUsage EditorSound => get(OsuIconMapping.EditorSound);
        public static IconUsage EditorWhistle => get(OsuIconMapping.EditorWhistle);
        public static IconUsage EditorClap => get(OsuIconMapping.EditorClap);
        public static IconUsage EditorBankAuto => get(OsuIconMapping.EditorBankAuto);
        public static IconUsage EditorBankAutoCompact => get(OsuIconMapping.EditorBankAutoCompact);
        public static IconUsage EditorBankNormal => get(OsuIconMapping.EditorBankNormal);
        public static IconUsage EditorBankNormalCompact => get(OsuIconMapping.EditorBankNormalCompact);
        public static IconUsage EditorBankSoft => get(OsuIconMapping.EditorBankSoft);
        public static IconUsage EditorBankSoftCompact => get(OsuIconMapping.EditorBankSoftCompact);
        public static IconUsage EditorBankDrum => get(OsuIconMapping.EditorBankDrum);
        public static IconUsage EditorBankDrumCompact => get(OsuIconMapping.EditorBankDrumCompact);
        public static IconUsage Tortoise => get(OsuIconMapping.Tortoise);
        public static IconUsage Hare => get(OsuIconMapping.Hare);

        private static IconUsage get(OsuIconMapping glyph) => new IconUsage((char)glyph, FONT_NAME);

        private enum OsuIconMapping
        {
            [Description(@"Logo")]
            Logo,

            [Description(@"RulesetOsu")]
            RulesetOsu,

            [Description(@"RulesetMania")]
            RulesetMania,

            [Description(@"RulesetCatch")]
            RulesetCatch,

            [Description(@"RulesetTaiko")]
            RulesetTaiko,

            [Description(@"EditCircle")]
            EditCircle,

            [Description(@"LeftCircle")]
            LeftCircle,

            [Description(@"RightCircle")]
            RightCircle,

            [Description(@"audio")]
            Audio,

            [Description(@"beatmap")]
            Beatmap,

            [Description(@"calendar")]
            Calendar,

            [Description(@"changelog-a")]
            ChangelogA,

            [Description(@"changelog-b")]
            ChangelogB,

            [Description(@"chat")]
            Chat,

            [Description(@"check-circle")]
            CheckCircle,

            [Description(@"clock")]
            Clock,

            [Description(@"collapse-a")]
            CollapseA,

            [Description(@"collections")]
            Collections,

            [Description(@"cross")]
            Cross,

            [Description(@"cross-circle")]
            CrossCircle,

            [Description(@"crown")]
            Crown,

            [Description(@"daily-challenge")]
            DailyChallenge,

            [Description(@"debug")]
            Debug,

            [Description(@"delete")]
            Delete,

            [Description(@"details")]
            Details,

            [Description(@"discord")]
            Discord,

            [Description(@"ellipsis-horizontal")]
            EllipsisHorizontal,

            [Description(@"ellipsis-vertical")]
            EllipsisVertical,

            [Description(@"expand-a")]
            ExpandA,

            [Description(@"expand-b")]
            ExpandB,

            [Description(@"featured-artist")]
            FeaturedArtist,

            [Description(@"featured-artist-circle")]
            FeaturedArtistCircle,

            [Description(@"gameplay-a")]
            GameplayA,

            [Description(@"gameplay-b")]
            GameplayB,

            [Description(@"gameplay-c")]
            GameplayC,

            [Description(@"global")]
            Global,

            [Description(@"graphics")]
            Graphics,

            [Description(@"heart")]
            Heart,

            [Description(@"home")]
            Home,

            [Description(@"input")]
            Input,

            [Description(@"maintenance")]
            Maintenance,

            [Description(@"megaphone")]
            Megaphone,

            [Description(@"metronome")]
            Metronome,

            [Description(@"music")]
            Music,

            [Description(@"news")]
            News,

            [Description(@"next")]
            Next,

            [Description(@"next-circle")]
            NextCircle,

            [Description(@"notification")]
            Notification,

            [Description(@"online")]
            Online,

            [Description(@"play")]
            Play,

            [Description(@"player")]
            Player,

            [Description(@"player-follow")]
            PlayerFollow,

            [Description(@"prev")]
            Prev,

            [Description(@"prev-circle")]
            PrevCircle,

            [Description(@"ranking")]
            Ranking,

            [Description(@"rulesets")]
            Rulesets,

            [Description(@"search")]
            Search,

            [Description(@"settings")]
            Settings,

            [Description(@"skin-a")]
            SkinA,

            [Description(@"skin-b")]
            SkinB,

            [Description(@"star")]
            Star,

            [Description(@"storyboard")]
            Storyboard,

            [Description(@"team")]
            Team,

            [Description(@"thumbs-up")]
            ThumbsUp,

            [Description(@"tournament")]
            Tournament,

            [Description(@"twitter")]
            Twitter,

            [Description(@"undo")]
            Undo,

            [Description(@"user-interface")]
            UserInterface,

            [Description(@"wiki")]
            Wiki,

            [Description(@"Editor/hitcircle")]
            EditorHitCircle,

            [Description(@"Editor/slider")]
            EditorSlider,

            [Description(@"Editor/spinner")]
            EditorSpinner,

            [Description(@"Editor/hit")]
            EditorHit,

            [Description(@"Editor/drum-roll")]
            EditorDrumRoll,

            [Description(@"Editor/swell")]
            EditorSwell,

            [Description(@"Editor/fruit")]
            EditorFruit,

            [Description(@"Editor/juice-stream")]
            EditorJuiceStream,

            [Description(@"Editor/banana-shower")]
            EditorBananaShower,

            [Description(@"Editor/note")]
            EditorNote,

            [Description(@"Editor/hold-note")]
            EditorHoldNote,

            [Description(@"Editor/grid")]
            EditorGrid,

            [Description(@"Editor/add-control-point")]
            EditorAddControlPoint = 1000,

            [Description(@"Editor/convert-to-stream")]
            EditorConvertToStream,

            [Description(@"Editor/distance-snap")]
            EditorDistanceSnap,

            [Description(@"Editor/finish")]
            EditorFinish,

            [Description(@"Editor/grid-snap")]
            EditorGridSnap,

            [Description(@"Editor/new-combo-sparkles")]
            EditorNewComboSparkles,

            [Description(@"Editor/select")]
            EditorSelect,

            [Description(@"Editor/sound")]
            EditorSound,

            [Description(@"Editor/whistle")]
            EditorWhistle,

            [Description(@"Editor/clap")]
            EditorClap,

            [Description(@"Editor/bank-auto")]
            EditorBankAuto,

            [Description(@"Editor/bank-auto-compact")]
            EditorBankAutoCompact,

            [Description(@"Editor/bank-normal")]
            EditorBankNormal,

            [Description(@"Editor/bank-normal-compact")]
            EditorBankNormalCompact,

            [Description(@"Editor/bank-soft")]
            EditorBankSoft,

            [Description(@"Editor/bank-soft-compact")]
            EditorBankSoftCompact,

            [Description(@"Editor/bank-drum")]
            EditorBankDrum,

            [Description(@"Editor/bank-drum-compact")]
            EditorBankDrumCompact,

            [Description(@"tortoise")]
            Tortoise,

            [Description(@"hare")]
            Hare,
        }

        public class OsuIconStore : ITextureStore, ITexturedGlyphLookupStore
        {
            private readonly TextureStore textures;

            public OsuIconStore(TextureStore textures)
            {
                this.textures = textures;
            }

            public ITexturedCharacterGlyph? Get(string? fontName, char character)
            {
                if (fontName == FONT_NAME)
                    return new Glyph(textures.Get($@"{fontName}/{((OsuIconMapping)character).GetDescription()}"));

                return null;
            }

            public Task<ITexturedCharacterGlyph?> GetAsync(string fontName, char character) => Task.Run(() => Get(fontName, character));

            public Texture? Get(string name, WrapMode wrapModeS, WrapMode wrapModeT) => null;

            public Texture Get(string name) => throw new NotImplementedException();

            public Task<Texture> GetAsync(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Stream GetStream(string name) => throw new NotImplementedException();

            public IEnumerable<string> GetAvailableResources() => throw new NotImplementedException();

            public Task<Texture?> GetAsync(string name, WrapMode wrapModeS, WrapMode wrapModeT, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public class Glyph : ITexturedCharacterGlyph
            {
                public float XOffset => 0;
                public float YOffset => 0;
                public float XAdvance => 0;
                public float Baseline => 0;
                public char Character => '\0';

                public float GetKerning<T>(T lastGlyph) where T : ICharacterGlyph => throw new NotImplementedException();

                public Texture Texture { get; }
                public float Width => Texture.Width;
                public float Height => Texture.Height;

                public Glyph(Texture texture)
                {
                    Texture = texture;
                }
            }

            public void Dispose()
            {
                textures.Dispose();
            }
        }
    }
}

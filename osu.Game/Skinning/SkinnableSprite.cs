// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Configuration;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Skinning
{
    /// <summary>
    /// A skinnable element which uses a single texture backing.
    /// </summary>
    public partial class SkinnableSprite : SkinnableDrawable
    {
        protected override bool ApplySizeRestrictionsToDefault => true;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public Bindable<string> SpriteName { get; } = new Bindable<string>(string.Empty);

        public SkinnableSprite(string textureName, Vector2? maxSize = null, ConfineMode confineMode = ConfineMode.NoScaling)
            : base(new SpriteComponentLookup(textureName, maxSize), confineMode)
        {
            SpriteName.Value = textureName;
        }

        public SkinnableSprite()
            : base(new SpriteComponentLookup(string.Empty), ConfineMode.NoScaling)
        {
            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.Both;

            SpriteName.BindValueChanged(name =>
            {
                ((SpriteComponentLookup)ComponentLookup).LookupName = name.NewValue ?? string.Empty;
                if (IsLoaded)
                    SkinChanged(CurrentSkin);
            });
        }

        protected override Drawable CreateDefault(ISkinComponentLookup lookup)
        {
            var spriteLookup = (SpriteComponentLookup)lookup;
            var texture = textures.Get(spriteLookup.LookupName);

            if (texture == null)
                return new SpriteNotFound(spriteLookup.LookupName);

            if (spriteLookup.MaxSize != null)
                texture = texture.WithMaximumSize(spriteLookup.MaxSize.Value);

            return new Sprite { Texture = texture };
        }

        public bool UsesFixedAnchor { get; set; }

        internal class SpriteComponentLookup : ISkinComponentLookup
        {
            public string LookupName { get; set; }
            public Vector2? MaxSize { get; set; }

            public SpriteComponentLookup(string textureName, Vector2? maxSize = null)
            {
                LookupName = textureName;
                MaxSize = maxSize;
            }
        }

        public partial class SpriteNotFound : CompositeDrawable
        {
            public SpriteNotFound(string lookup)
            {
                AutoSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Size = new Vector2(50),
                        Icon = FontAwesome.Solid.QuestionCircle
                    },
                    new OsuSpriteText
                    {
                        Position = new Vector2(25, 50),
                        Text = $"missing: {lookup}",
                        Origin = Anchor.TopCentre,
                    }
                };
            }
        }
    }
}

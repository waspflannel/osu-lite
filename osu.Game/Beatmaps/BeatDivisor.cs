// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Graphics;
using osuTK.Graphics;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Shared beat-divisor constants and helpers used when parsing, snapping, and colouring beatmap timing.
    /// </summary>
    public static class BeatDivisor
    {
        public static readonly int[] PREDEFINED_DIVISORS = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };

        public const int MINIMUM_DIVISOR = 1;

        public const int MAXIMUM_DIVISOR = 64;

        /// <summary>
        /// Retrieves the colour for a specified beat divisor.
        /// </summary>
        public static Color4 GetColourFor(int beatDivisor, OsuColour colours)
        {
            switch (beatDivisor)
            {
                case 1:
                    return Color4.White;

                case 2:
                    return colours.Red;

                case 4:
                    return colours.Blue;

                case 8:
                    return colours.Yellow;

                case 16:
                    return colours.PurpleDark;

                case 3:
                    return colours.Purple;

                case 6:
                    return colours.YellowDark;

                case 12:
                    return colours.YellowDarker;

                case 5:
                case 7:
                case 9:
                    return colours.GreenLight;

                default:
                    return Color4.Red;
            }
        }
    }
}

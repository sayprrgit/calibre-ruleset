// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Palette du ruleset. Toutes les couleurs sont définies ici : aucun skin ni texture externe n'est utilisé.
    /// </summary>
    public static class CalibreColours
    {
        public static readonly Colour4 ACCENT = Colour4.FromHex("#ffb347");

        public static readonly Colour4 PLAYFIELD_BORDER = Colour4.White.Opacity(0.08f);

        public static readonly Colour4 GREAT = Colour4.FromHex("#66ccff");
        public static readonly Colour4 OK = Colour4.FromHex("#88dd66");
        public static readonly Colour4 MEH = Colour4.FromHex("#ffcc33");
        public static readonly Colour4 MISS = Colour4.FromHex("#ff5566");

        private static readonly Colour4[] combo_colours =
        {
            Colour4.FromHex("#4fa3ff"),
            Colour4.FromHex("#ff7a59"),
            Colour4.FromHex("#3ddc97"),
            Colour4.FromHex("#c77dff"),
        };

        public static Colour4 ForCombo(int comboIndex) => combo_colours[(comboIndex % combo_colours.Length + combo_colours.Length) % combo_colours.Length];

        /// <summary>
        /// Dégradé vert → jaune → rouge pour une erreur ramenée entre 0 (aucune) et 1 (la pire), assombri pour garder le texte lisible.
        /// </summary>
        public static Colour4 ForError(double ratio)
        {
            float t = (float)Math.Clamp(ratio, 0, 1);

            var good = Colour4.FromHex("#3ddc97");
            var medium = Colour4.FromHex("#ffcc33");
            var bad = Colour4.FromHex("#ff5566");

            var colour = t < 0.5f ? interpolate(good, medium, t * 2) : interpolate(medium, bad, (t - 0.5f) * 2);
            return colour.Darken(0.6f);
        }

        private static Colour4 interpolate(Colour4 from, Colour4 to, float t)
            => new Colour4(from.R + (to.R - from.R) * t, from.G + (to.G - from.G) * t, from.B + (to.B - from.B) * t, 1);

        /// <summary>
        /// Couleur propre à chaque entrée dans les graphiques du diagnostic.
        /// </summary>
        public static Colour4 ForAction(CalibreAction action)
        {
            switch (action)
            {
                case CalibreAction.Key1:
                    return ACCENT;

                case CalibreAction.Key2:
                    return GREAT;

                case CalibreAction.Mouse1:
                    return OK;

                default:
                    return combo_colours[3];
            }
        }

        public static Colour4 ForResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                    return GREAT;

                case HitResult.Ok:
                    return OK;

                case HitResult.Meh:
                    return MEH;

                default:
                    return MISS;
            }
        }
    }
}

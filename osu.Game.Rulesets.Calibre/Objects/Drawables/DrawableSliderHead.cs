// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Tête de slider : un cercle à part entière (anneau d'approche, note lock, fenêtres, position et touche enregistrées).
    /// </summary>
    public partial class DrawableSliderHead : DrawableHitCircle
    {
        public DrawableSliderHead()
            : this(null)
        {
        }

        public DrawableSliderHead(SliderHeadCircle? headCircle)
            : base(headCircle)
        {
        }
    }
}

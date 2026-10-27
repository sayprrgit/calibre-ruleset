// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables.Pieces
{
    /// <summary>
    /// Anneau qui se resserre de 4x à 1x pendant le préempt : il atteint le bord du cercle à l'instant de frappe.
    /// </summary>
    public partial class ApproachRing : CircularContainer
    {
        public ApproachRing()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Masking = true;
            BorderThickness = 4;
            BorderColour = Colour4.White;
            Alpha = 0;

            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                AlwaysPresent = true,
            };
        }
    }
}

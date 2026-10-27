// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Lines;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables.Pieces
{
    /// <summary>
    /// Corps du slider, tracé en code : bordure blanche puis remplissage aux couleurs du combo, plus opaque vers les bords.
    /// </summary>
    public partial class SliderBodyPath : SmoothPath
    {
        /// <summary>
        /// Part du rayon occupée par la bordure (0 = bord extérieur, 1 = axe du chemin).
        /// </summary>
        private const float border_portion = 0.12f;

        private const float opacity_at_edge = 0.75f;
        private const float opacity_at_centre = 0.35f;

        private Color4 accentColour = Color4.White;

        public Color4 AccentColour
        {
            get => accentColour;
            set
            {
                if (accentColour == value)
                    return;

                accentColour = value;
                InvalidateTexture();
            }
        }

        /// <summary>
        /// Remplace le tracé et place l'origine du drawable sur le premier point du chemin (la tête du slider).
        /// </summary>
        public void SetPath(IReadOnlyList<Vector2> vertices)
        {
            Vertices = vertices;
            refreshOrigin();
        }

        public override float PathRadius
        {
            get => base.PathRadius;
            set
            {
                base.PathRadius = value;
                refreshOrigin();
            }
        }

        // La boîte englobante dépend du rayon : l'origine est recalculée à chaque changement de tracé ou d'épaisseur.
        private void refreshOrigin()
        {
            if (Vertices.Count > 0)
                OriginPosition = PositionInBoundingBox(Vector2.Zero);
        }

        protected override Color4 ColourAt(float position)
        {
            if (position <= border_portion)
                return Color4.White;

            float t = (position - border_portion) / (1 - border_portion);
            return new Color4(accentColour.R, accentColour.G, accentColour.B, opacity_at_edge + (opacity_at_centre - opacity_at_edge) * t);
        }
    }
}

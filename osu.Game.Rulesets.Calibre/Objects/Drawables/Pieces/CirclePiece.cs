// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables.Pieces
{
    /// <summary>
    /// Corps d'un cercle : disque coloré selon le combo, contour blanc et numéro dans le combo.
    /// </summary>
    public partial class CirclePiece : CompositeDrawable
    {
        private readonly Circle fill;
        private readonly OsuSpriteText number;

        private int comboNumber = -1;

        public CirclePiece()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 10,
                    BorderColour = Colour4.White,
                    Child = fill = new Circle
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                },
                number = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.Default.With(size: 48, weight: FontWeight.Bold),
                },
            };
        }

        public Colour4 AccentColour
        {
            set => fill.Colour = value;
        }

        public int ComboNumber
        {
            set
            {
                // Évite de réassigner le texte (et de relancer sa mise en page) quand la valeur ne change pas.
                if (comboNumber == value)
                    return;

                comboNumber = value;
                number.Text = value.ToString();
            }
        }
    }
}

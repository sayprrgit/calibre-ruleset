// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI.Cursor
{
    public partial class CalibreCursorContainer : GameplayCursorContainer
    {
        protected override Drawable CreateCursor() => new CalibreCursor();

        /// <summary>
        /// Curseur de gameplay : anneau avec point central, pour lire précisément la position de visée.
        /// </summary>
        public partial class CalibreCursor : CompositeDrawable
        {
            public CalibreCursor()
            {
                Origin = Anchor.Centre;
                Size = new Vector2(32);

                InternalChildren = new Drawable[]
                {
                    new CircularContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderThickness = 3,
                        BorderColour = CalibreColours.ACCENT,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = CalibreColours.ACCENT,
                            Alpha = 0.15f,
                        },
                    },
                    new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(5),
                        Colour = Colour4.White,
                    },
                };
            }
        }
    }
}

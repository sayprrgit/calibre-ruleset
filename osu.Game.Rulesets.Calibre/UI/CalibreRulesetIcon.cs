// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Icône du ruleset, entièrement dessinée en code : une mire (anneau + réticule + point central).
    /// </summary>
    public partial class CalibreRulesetIcon : CompositeDrawable
    {
        public CalibreRulesetIcon()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new Container
            {
                Size = new Vector2(20),
                Children = new Drawable[]
                {
                    new CircularContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderThickness = 2.5f,
                        BorderColour = Colour4.White,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            AlwaysPresent = true,
                        },
                    },
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Width = 1.3f,
                        Height = 1.5f,
                    },
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Y,
                        Height = 1.3f,
                        Width = 1.5f,
                    },
                    new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(6),
                        Colour = CalibreColours.ACCENT,
                    },
                },
            };
        }
    }
}

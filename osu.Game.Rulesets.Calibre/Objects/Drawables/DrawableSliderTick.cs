// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    public partial class DrawableSliderTick : DrawableSliderNested
    {
        public DrawableSliderTick()
            : this(null)
        {
        }

        public DrawableSliderTick(SliderTick? tick)
            : base(tick)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Content.Child = new Circle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(18),
                Colour = Colour4.White,
            };
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Queue du slider : jugée à la fin du slider, d'après le suivi relevé 36 ms plus tôt (tolérance d'osu!).
    /// </summary>
    public partial class DrawableSliderTail : DrawableSliderNested
    {
        public DrawableSliderTail()
            : this(null)
        {
        }

        public DrawableSliderTail(SliderTailCircle? tail)
            : base(tail)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Content.Child = new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.9f),
                Masking = true,
                BorderThickness = 6,
                BorderColour = Colour4.White.Opacity(0.6f),
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true,
                },
            };
        }

        protected override bool IsTracked() => Slider?.TrackedAtTailLeniency == true;
    }
}

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
    /// Répétition : cercle cerclé de blanc portant une flèche (chevron dessiné en code) orientée dans le sens du retour.
    /// </summary>
    public partial class DrawableSliderRepeat : DrawableSliderNested
    {
        public new SliderRepeat HitObject => (SliderRepeat)base.HitObject;

        private Container arrow = null!;

        public DrawableSliderRepeat()
            : this(null)
        {
        }

        public DrawableSliderRepeat(SliderRepeat? repeat)
            : base(repeat)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Content.Children = new Drawable[]
            {
                new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.9f),
                    Masking = true,
                    BorderThickness = 6,
                    BorderColour = Colour4.White,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true,
                    },
                },
                arrow = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(40),
                    Children = new Drawable[]
                    {
                        // Deux branches à ±45° forment un chevron « > » pointant vers +X avant rotation.
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.CentreRight,
                            Size = new Vector2(26, 7),
                            X = 8,
                            Rotation = 45,
                            Colour = Colour4.White,
                        },
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.CentreRight,
                            Size = new Vector2(26, 7),
                            X = 8,
                            Rotation = -45,
                            Colour = Colour4.White,
                        },
                    },
                },
            };
        }

        protected override void OnApply()
        {
            base.OnApply();

            arrow.Rotation = HitObject.ArrowRotation;
        }
    }
}

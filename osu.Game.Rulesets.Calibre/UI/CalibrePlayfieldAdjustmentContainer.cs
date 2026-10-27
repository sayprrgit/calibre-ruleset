// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Place l'aire de jeu 512x384 au centre de l'écran, en 4:3, sur 80 % de la hauteur disponible
    /// (même cadrage qu'osu!standard, pour que les distances de visée soient identiques à l'écran).
    /// </summary>
    public partial class CalibrePlayfieldAdjustmentContainer : PlayfieldAdjustmentContainer
    {
        private const float playfield_size_adjust = 0.8f;

        protected override Container<Drawable> Content => content;

        private readonly ScalingContainer content;

        public CalibrePlayfieldAdjustmentContainer()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new Vector2(playfield_size_adjust);

            InternalChild = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                FillAspectRatio = 4f / 3,
                Child = content = new ScalingContainer { RelativeSizeAxes = Axes.Both },
            };
        }

        /// <summary>
        /// Met à l'échelle le contenu pour que ses coordonnées internes restent en pixels de l'aire de jeu (512 de large),
        /// quelle que soit la résolution.
        /// </summary>
        private partial class ScalingContainer : Container
        {
            protected override void Update()
            {
                base.Update();

                float scale = Parent!.ChildSize.X / CalibrePlayfield.BASE_SIZE.X;

                Scale = new Vector2(scale);
                Size = new Vector2(1 / scale);
            }
        }
    }
}

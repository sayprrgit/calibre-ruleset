// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Libellé de jugement affiché brièvement à l'emplacement de l'objet. Poolé : aucune allocation par jugement.
    /// </summary>
    public partial class DrawableCalibreJudgement : PoolableDrawable
    {
        private const double display_duration = 500;

        private readonly OsuSpriteText text;

        public DrawableCalibreJudgement()
        {
            AutoSizeAxes = Axes.Both;
            Origin = Anchor.Centre;

            InternalChild = text = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Font = OsuFont.Default.With(size: 20, weight: FontWeight.Bold),
            };
        }

        public void Apply(HitResult result, Vector2 position)
        {
            Position = position;
            text.Text = labelFor(result);
            text.Colour = CalibreColours.ForResult(result);
        }

        protected override void PrepareForUse()
        {
            base.PrepareForUse();

            ClearTransforms(true);

            this.FadeInFromZero(60)
                .Then().Delay(display_duration - 160).FadeOut(100);
            this.MoveToOffset(new Vector2(0, -20), display_duration, Easing.OutQuint);
            this.Delay(display_duration).Expire();
        }

        private static string labelFor(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                    return "Great";

                case HitResult.Ok:
                    return "Ok";

                case HitResult.Meh:
                    return "Meh";

                default:
                    return "Miss";
            }
        }
    }
}

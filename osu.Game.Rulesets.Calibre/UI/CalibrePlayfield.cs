// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Objects.Drawables;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.Calibre.UI.Cursor;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    [Cached]
    public partial class CalibrePlayfield : Playfield
    {
        /// <summary>
        /// Taille de l'aire de jeu en pixels logiques : celle des beatmaps osu!standard.
        /// </summary>
        public static readonly Vector2 BASE_SIZE = new Vector2(512, 384);

        private readonly Container<DrawableCalibreJudgement> judgementLayer;
        private readonly DrawablePool<DrawableCalibreJudgement> judgementPool;
        private readonly StartTimeOrderedHitPolicy hitPolicy;

        public CalibrePlayfield()
        {
            // La taille reste relative : CalibrePlayfieldAdjustmentContainer fournit un espace de BASE_SIZE pixels logiques.
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 2,
                    BorderColour = CalibreColours.PLAYFIELD_BORDER,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true,
                    },
                },
                judgementLayer = new Container<DrawableCalibreJudgement> { RelativeSizeAxes = Axes.Both },
                HitObjectContainer,
                judgementPool = new DrawablePool<DrawableCalibreJudgement>(20, 100),
            };

            hitPolicy = new StartTimeOrderedHitPolicy { HitObjectContainer = HitObjectContainer };

            NewResult += onNewResult;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RegisterPool<HitCircle, DrawableHitCircle>(20, 100);
        }

        protected override GameplayCursorContainer CreateCursor() => new CalibreCursorContainer();

        protected override HitObjectLifetimeEntry CreateLifetimeEntry(HitObject hitObject) => new CalibreHitObjectLifetimeEntry(hitObject);

        protected override void OnNewDrawableHitObject(DrawableHitObject drawableHitObject)
        {
            base.OnNewDrawableHitObject(drawableHitObject);

            // Appelé une fois par instance poolée : le délégué est réutilisé à chaque réutilisation de l'objet.
            ((DrawableCalibreHitObject)drawableHitObject).CheckHittable = hitPolicy.CheckHittable;
        }

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
        {
            hitPolicy.HandleHit(judgedObject);

            if (!judgedObject.DisplayResult || !DisplayJudgements.Value)
                return;

            var judgement = judgementPool.Get();
            judgement.Apply(result.Type, ((CalibreHitObject)judgedObject.HitObject).Position);
            judgementLayer.Add(judgement);
        }

        // Les objets proches des bords débordent de l'aire de jeu : on accepte l'entrée partout où un objet peut la recevoir.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => HitObjectContainer.ReceivePositionalInputAt(screenSpacePos);

        /// <summary>
        /// Rend l'objet vivant (et donc instancie son drawable poolé) seulement au début de son préempt,
        /// au lieu des 10 s par défaut.
        /// </summary>
        private class CalibreHitObjectLifetimeEntry : HitObjectLifetimeEntry
        {
            public CalibreHitObjectLifetimeEntry(HitObject hitObject)
                : base(hitObject)
            {
            }

            protected override double InitialLifetimeOffset => ((CalibreHitObject)HitObject).TimePreempt;
        }
    }
}

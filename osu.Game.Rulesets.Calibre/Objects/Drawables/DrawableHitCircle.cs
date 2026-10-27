// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Calibre.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    public partial class DrawableHitCircle : DrawableCalibreHitObject
    {
        public HitReceptor HitArea { get; private set; } = null!;
        public CirclePiece CirclePiece { get; private set; } = null!;
        public ApproachRing ApproachRing { get; private set; } = null!;

        private Container scaleContainer = null!;
        private ShakeContainer shakeContainer = null!;

        public DrawableHitCircle()
            : this(null)
        {
        }

        public DrawableHitCircle(HitCircle? hitCircle)
            : base(hitCircle)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            Size = CalibreHitObject.OBJECT_DIMENSIONS;

            AddInternal(scaleContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    HitArea = new HitReceptor
                    {
                        CanBeHit = () => !AllJudged,
                        Hit = () => UpdateResult(true),
                    },
                    shakeContainer = new ShakeContainer
                    {
                        ShakeDuration = 30,
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            CirclePiece = new CirclePiece(),
                            ApproachRing = new ApproachRing(),
                        },
                    },
                },
            });

            // Ces abonnements sont créés une seule fois par instance poolée, pas à chaque réutilisation.
            PositionBindable.BindValueChanged(p => Position = p.NewValue);
            ScaleBindable.BindValueChanged(s => scaleContainer.Scale = new Vector2(s.NewValue));
            IndexInCurrentComboBindable.BindValueChanged(i => CirclePiece.ComboNumber = i.NewValue + 1);
            ComboIndexBindable.BindValueChanged(c => CirclePiece.AccentColour = CalibreColours.ForCombo(c.NewValue));
        }

        public override void Shake() => shakeContainer.Shake();

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            Debug.Assert(HitObject.HitWindows != null);

            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                {
                    // Lambdas statiques + état passé en paramètre : aucune fermeture allouée pendant le jeu.
                    ApplyResult(static (r, position) =>
                    {
                        var result = (CalibreHitCircleJudgementResult)r;
                        result.Type = r.Judgement.MinResult;
                        result.CursorPositionAtHit = position;
                    }, computeHitPosition());
                }

                return;
            }

            var hitResult = HitObject.HitWindows.ResultFor(timeOffset);
            var clickAction = CheckHittable?.Invoke(this, Time.Current, hitResult) ?? ClickAction.Hit;

            if (clickAction == ClickAction.Shake)
                Shake();

            if (hitResult == HitResult.None || clickAction != ClickAction.Hit)
                return;

            ApplyResult(static (r, state) =>
            {
                var result = (CalibreHitCircleJudgementResult)r;
                result.Type = state.hitResult;
                result.CursorPositionAtHit = state.position;
                result.HitAction = state.action;
            }, (hitResult, position: computeHitPosition(), action: HitArea.LastPressAction));
        }

        /// <summary>
        /// Position de la frappe la plus proche du centre, ramenée dans l'espace de l'aire de jeu.
        /// </summary>
        private Vector2? computeHitPosition()
        {
            if (HitArea.ClosestPressPosition is Vector2 screenSpacePosition)
                return HitObject.Position + (ToLocalSpace(screenSpacePosition) - DrawSize / 2);

            return null;
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            CirclePiece.FadeInFromZero(HitObject.TimeFadeIn);

            ApproachRing.FadeOut().ScaleTo(4);
            ApproachRing.FadeTo(0.9f, Math.Min(HitObject.TimeFadeIn * 2, HitObject.TimePreempt));
            ApproachRing.ScaleTo(1, HitObject.TimePreempt);
        }

        protected override void UpdateStartTimeStateTransforms()
        {
            base.UpdateStartTimeStateTransforms();

            // L'anneau d'approche disparaît toujours à l'instant exact de frappe, comme dans osu!.
            ApproachRing.FadeOut(50);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                // Pas d'Expire() ici : l'objet doit rester vivant après son instant de frappe
                // pour accepter une frappe tardive puis être jugé Miss à la fin de la fenêtre.
                case ArmedState.Idle:
                    HitArea.Reset();
                    break;

                case ArmedState.Hit:
                    ApproachRing.FadeOut();
                    CirclePiece.ScaleTo(1.4f, 240, Easing.OutQuint);
                    this.FadeOut(240, Easing.OutQuint);
                    Expire();
                    break;

                case ArmedState.Miss:
                    ApproachRing.FadeOut();
                    this.FadeColour(CalibreColours.MISS, 100);
                    this.FadeOut(160, Easing.In);
                    Expire();
                    break;
            }
        }

        protected override void OnFree()
        {
            base.OnFree();

            CirclePiece.Scale = Vector2.One;
            Colour = Colour4.White;
        }

        protected override JudgementResult CreateResult(Judgement judgement) => new CalibreHitCircleJudgementResult(HitObject, judgement);

        /// <summary>
        /// Zone cliquable circulaire. Mémorise la frappe la plus proche du centre et l'entrée utilisée.
        /// </summary>
        public partial class HitReceptor : CompositeDrawable, IKeyBindingHandler<CalibreAction>
        {
            public override bool HandlePositionalInput => true;

            public required Func<bool> CanBeHit { get; init; }

            public required Action Hit { get; init; }

            public Vector2? ClosestPressPosition { get; private set; }

            public CalibreAction? LastPressAction { get; private set; }

            public HitReceptor()
            {
                Size = CalibreHitObject.OBJECT_DIMENSIONS;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                // Arrondi complet : IsHovered ne vaut vrai qu'à l'intérieur du cercle et non du carré englobant.
                CornerRadius = CalibreHitObject.OBJECT_RADIUS;
                CornerExponent = 2;
            }

            public bool OnPressed(KeyBindingPressEvent<CalibreAction> e)
            {
                if (!e.Action.IsHitAction() || !CanBeHit())
                    return false;

                var centre = ScreenSpaceDrawQuad.Centre;

                if (ClosestPressPosition is not Vector2 closest
                    || Vector2.DistanceSquared(e.ScreenSpaceMousePosition, centre) < Vector2.DistanceSquared(closest, centre))
                    ClosestPressPosition = e.ScreenSpaceMousePosition;

                if (!IsHovered)
                    return false;

                LastPressAction = e.Action;
                Hit();
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<CalibreAction> e)
            {
            }

            public void Reset()
            {
                ClosestPressPosition = null;
                LastPressAction = null;
            }
        }
    }
}

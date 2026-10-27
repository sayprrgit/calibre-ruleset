// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Rulesets.Calibre.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Slider : couvre toute l'aire de jeu pour que ses éléments imbriqués gardent les coordonnées 512x384 de leurs objets.
    /// Il calcule à chaque frame l'état de suivi que ses ticks, répétitions et queue consultent pour se juger.
    /// </summary>
    public partial class DrawableSlider : DrawableCalibreHitObject
    {
        public new Slider HitObject => (Slider)base.HitObject;

        /// <summary>
        /// Vrai si une entrée de frappe est maintenue et que le curseur est dans la zone de suivi de la bille.
        /// </summary>
        public bool Tracking { get; private set; }

        /// <summary>
        /// Suivi relevé à l'instant de tolérance de la queue (36 ms avant la fin, comme osu!) : c'est lui qui juge la queue.
        /// </summary>
        public bool TrackedAtTailLeniency { get; private set; }

        public DrawableSliderHead? HeadCircle { get; private set; }

        public DrawableSliderTail? TailCircle { get; private set; }

        public override bool DisplayResult => false;

        private SliderBodyPath body = null!;
        private Container<DrawableSliderTail> tailContainer = null!;
        private Container<DrawableSliderTick> tickContainer = null!;
        private Container<DrawableSliderRepeat> repeatContainer = null!;
        private Container<DrawableSliderHead> headContainer = null!;

        private Container ball = null!;
        private CircularContainer followArea = null!;

        private readonly List<Vector2> pathVertices = new List<Vector2>();

        private CalibreInputManager? inputManager;
        private bool tailTrackingRecorded;

        public DrawableSlider()
            : this(null)
        {
        }

        public DrawableSlider(Slider? slider)
            : base(slider)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                body = new SliderBodyPath(),
                tailContainer = new Container<DrawableSliderTail> { RelativeSizeAxes = Axes.Both },
                tickContainer = new Container<DrawableSliderTick> { RelativeSizeAxes = Axes.Both },
                repeatContainer = new Container<DrawableSliderRepeat> { RelativeSizeAxes = Axes.Both },
                ball = new Container
                {
                    Origin = Anchor.Centre,
                    Size = CalibreHitObject.OBJECT_DIMENSIONS,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        followArea = new CircularContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(Slider.FOLLOW_AREA),
                            Masking = true,
                            BorderThickness = 5,
                            BorderColour = CalibreColours.ACCENT,
                            Alpha = 0,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = CalibreColours.ACCENT,
                                Alpha = 0.1f,
                            },
                        },
                        new Circle
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.7f),
                            Colour = Colour4.White,
                        },
                    },
                },
                headContainer = new Container<DrawableSliderHead> { RelativeSizeAxes = Axes.Both },
            });

            ScaleBindable.BindValueChanged(s =>
            {
                body.PathRadius = CalibreHitObject.OBJECT_RADIUS * s.NewValue;
                ball.Scale = new Vector2(s.NewValue);
            });

            ComboIndexBindable.BindValueChanged(c => body.AccentColour = CalibreColours.ForCombo(c.NewValue));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Hors d'un vrai ruleset (scènes de test isolées), il n'y a pas de gestionnaire d'actions : aucun suivi possible.
            inputManager = GetContainingInputManager() as CalibreInputManager;
        }

        protected override void OnApply()
        {
            base.OnApply();

            pathVertices.Clear();
            HitObject.Path.GetPathToProgress(pathVertices, 0, 1);

            body.SetPath(pathVertices);
            body.Position = HitObject.Position;

            Tracking = false;
            TrackedAtTailLeniency = false;
            tailTrackingRecorded = false;
        }

        protected override void AddNestedHitObject(DrawableHitObject hitObject)
        {
            base.AddNestedHitObject(hitObject);

            switch (hitObject)
            {
                case DrawableSliderHead head:
                    headContainer.Add(HeadCircle = head);
                    break;

                case DrawableSliderTail tail:
                    tailContainer.Add(TailCircle = tail);
                    break;

                case DrawableSliderTick tick:
                    tickContainer.Add(tick);
                    break;

                case DrawableSliderRepeat repeat:
                    repeatContainer.Add(repeat);
                    break;
            }
        }

        protected override void ClearNestedHitObjects()
        {
            base.ClearNestedHitObjects();

            // Les éléments imbriqués retournent dans leurs pools : on les détache sans les libérer.
            headContainer.Clear(false);
            tailContainer.Clear(false);
            tickContainer.Clear(false);
            repeatContainer.Clear(false);

            HeadCircle = null;
            TailCircle = null;
        }

        protected override void Update()
        {
            base.Update();

            var slider = HitObject;
            double time = Time.Current;

            if (time < slider.StartTime || time > slider.EndTime)
                Tracking = false;
            else
            {
                double progress = slider.Duration > 0 ? Math.Clamp((time - slider.StartTime) / slider.Duration, 0, 1) : 1;
                Vector2 ballPosition = slider.Position + slider.CurvePositionAt(progress);

                ball.Position = ballPosition;
                Tracking = isTracking(ballPosition);
            }

            // Si le temps recule (retour en arrière d'un replay), le relevé de la queue est refait au prochain passage.
            double tailLeniencyTime = Math.Max(slider.StartTime + slider.Duration / 2, slider.EndTime + SliderEventGenerator.TAIL_LENIENCY);

            if (time < tailLeniencyTime)
            {
                tailTrackingRecorded = false;
                TrackedAtTailLeniency = false;
            }
            else if (!tailTrackingRecorded)
            {
                tailTrackingRecorded = true;
                TrackedAtTailLeniency = Tracking;
            }

            followArea.Alpha = (float)Interpolation.DampContinuously(followArea.Alpha, Tracking ? 1 : 0, 20, Math.Abs(Time.Elapsed));
        }

        private bool isTracking(Vector2 ballPosition)
        {
            if (inputManager == null || !inputManager.IsAnyHitActionPressed())
                return false;

            // Comme dans osu!, la zone s'élargit une fois le suivi acquis : on peut « décrocher » moins facilement que l'on accroche.
            float radius = HitObject.Radius * (Tracking ? Slider.FOLLOW_AREA : 1);
            Vector2 cursor = ToLocalSpace(inputManager.CurrentState.Mouse.Position);

            return Vector2.DistanceSquared(cursor, ballPosition) <= radius * radius;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (userTriggered || TailCircle?.Judged != true || Time.Current < HitObject.EndTime)
                return;

            // Le slider ne rapporte rien en lui-même : il est « touché » si au moins un de ses éléments l'a été.
            ApplyResult(anyNestedHit() ? HitResult.IgnoreHit : HitResult.IgnoreMiss);
        }

        private bool anyNestedHit()
        {
            foreach (var nested in NestedHitObjects)
            {
                if (nested.IsHit)
                    return true;
            }

            return false;
        }

        // Les sons sont portés par la tête, les répétitions et la queue, joués à leur propre jugement.
        public override void PlaySamples()
        {
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            body.FadeInFromZero(HitObject.TimeFadeIn);
            ball.FadeOut();
        }

        protected override void UpdateStartTimeStateTransforms()
        {
            base.UpdateStartTimeStateTransforms();

            ball.FadeIn();
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:
                case ArmedState.Miss:
                    this.FadeOut(240, Easing.OutQuint).Expire();
                    break;
            }
        }
    }
}

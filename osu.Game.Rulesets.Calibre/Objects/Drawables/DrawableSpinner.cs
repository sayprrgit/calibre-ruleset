// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Spinner dessiné en code : un disque dont le remplissage suit la progression et une aiguille qui suit la rotation.
    /// </summary>
    public partial class DrawableSpinner : DrawableCalibreHitObject
    {
        /// <summary>
        /// Diamètre du spinner en pixels de l'aire de jeu.
        /// </summary>
        public const float SIZE = 300;

        public new Spinner HitObject => (Spinner)base.HitObject;

        public readonly SpinnerRotationTracker RotationTracker = new SpinnerRotationTracker();

        /// <summary>
        /// Part des tours requis déjà effectués, entre 0 et 1. Un spinner trop court pour exiger un tour est acquis d'emblée.
        /// </summary>
        public double Progress => HitObject.SpinsRequired == 0 ? 1 : Math.Clamp(Math.Floor(RotationTracker.CompletedSpins) / HitObject.SpinsRequired, 0, 1);

        private Container content = null!;
        private Circle progressFill = null!;
        private Container needle = null!;

        private CalibreInputManager? inputManager;

        public DrawableSpinner()
            : this(null)
        {
        }

        public DrawableSpinner(Spinner? spinner)
            : base(spinner)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            Size = new Vector2(SIZE);

            AddInternal(content = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new CircularContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderThickness = 6,
                        BorderColour = Colour4.White,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.Black,
                            Alpha = 0.3f,
                        },
                    },
                    progressFill = new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = CalibreColours.ACCENT,
                        Alpha = 0.35f,
                        Scale = Vector2.Zero,
                    },
                    needle = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Child = new Box
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Size = new Vector2(6, SIZE / 2),
                            Colour = Colour4.White,
                        },
                    },
                    new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(16),
                        Colour = Colour4.White,
                    },
                },
            });

            PositionBindable.BindValueChanged(p => Position = p.NewValue);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager() as CalibreInputManager;
        }

        protected override void OnApply()
        {
            base.OnApply();

            RotationTracker.Reset();
        }

        protected override void Update()
        {
            base.Update();

            double time = Time.Current;

            if (time < HitObject.StartTime)
                RotationTracker.Reset();
            else if (!Judged && time <= HitObject.EndTime && inputManager != null)
            {
                Vector2 fromCentre = ToLocalSpace(inputManager.CurrentState.Mouse.Position) - DrawSize / 2;
                RotationTracker.AddSample(Math.Atan2(fromCentre.Y, fromCentre.X), Time.Elapsed, inputManager.IsAnyHitActionPressed());
            }

            needle.Rotation = (float)(RotationTracker.SignedRotation * 180 / Math.PI);
            progressFill.Scale = new Vector2((float)Progress);
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (userTriggered || Time.Current < HitObject.EndTime)
                return;

            // Seuils d'osu! : tous les tours requis pour un Great, plus de 90 % pour un Ok, plus de 75 % pour un Meh.
            double progress = Progress;

            HitResult result;

            if (progress >= 1)
                result = HitResult.Great;
            else if (progress > 0.9)
                result = HitResult.Ok;
            else if (progress > 0.75)
                result = HitResult.Meh;
            else
                result = HitResult.Miss;

            ApplyResult(result);
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            content.FadeInFromZero(HitObject.TimeFadeIn);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:
                    content.ScaleTo(1.1f, 240, Easing.OutQuint);
                    this.FadeOut(240, Easing.OutQuint).Expire();
                    break;

                case ArmedState.Miss:
                    this.FadeColour(CalibreColours.MISS, 100);
                    this.FadeOut(240, Easing.In).Expire();
                    break;
            }
        }

        protected override void OnFree()
        {
            base.OnFree();

            content.Scale = Vector2.One;
            Colour = Colour4.White;
        }
    }
}

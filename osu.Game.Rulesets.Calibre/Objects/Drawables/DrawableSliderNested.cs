// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Base des éléments d'un slider jugés sur le suivi (tick, répétition, queue). Ils n'affichent pas de libellé de jugement.
    /// </summary>
    public abstract partial class DrawableSliderNested : DrawableCalibreHitObject
    {
        protected Container Content { get; private set; } = null!;

        public override bool DisplayResult => false;

        protected DrawableSlider? Slider => ParentHitObject as DrawableSlider;

        protected DrawableSliderNested(SliderNestedObject? hitObject)
            : base(hitObject)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            Size = CalibreHitObject.OBJECT_DIMENSIONS;

            AddInternal(Content = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            PositionBindable.BindValueChanged(p => Position = p.NewValue);
            ScaleBindable.BindValueChanged(s => Content.Scale = new Vector2(s.NewValue));
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (userTriggered || timeOffset < 0)
                return;

            ApplyResult(IsTracked() ? Result.Judgement.MaxResult : Result.Judgement.MinResult);
        }

        /// <summary>
        /// État de suivi qui décide du jugement, relevé sur le slider parent à l'instant de l'élément.
        /// </summary>
        protected virtual bool IsTracked() => Slider?.Tracking == true;

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            Content.FadeInFromZero(HitObject.TimeFadeIn);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            switch (state)
            {
                case ArmedState.Hit:
                    this.ScaleTo(1.5f, 150, Easing.OutQuint);
                    this.FadeOut(150, Easing.OutQuint).Expire();
                    break;

                case ArmedState.Miss:
                    this.FadeColour(CalibreColours.MISS, 60);
                    this.FadeOut(150, Easing.In).Expire();
                    break;
            }
        }

        protected override void OnFree()
        {
            base.OnFree();

            Scale = Vector2.One;
            Colour = Colour4.White;
        }
    }
}

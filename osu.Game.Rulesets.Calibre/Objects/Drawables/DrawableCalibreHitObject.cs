// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Base des représentations visuelles. Les instances sont poolées par le playfield :
    /// les bindables sont liés à l'objet courant dans <see cref="OnApply"/> et déliés dans <see cref="OnFree"/>.
    /// </summary>
    public abstract partial class DrawableCalibreHitObject : DrawableHitObject<CalibreHitObject>
    {
        public readonly IBindable<Vector2> PositionBindable = new Bindable<Vector2>();
        public readonly IBindable<float> ScaleBindable = new BindableFloat();
        public readonly IBindable<int> IndexInCurrentComboBindable = new Bindable<int>();
        public readonly IBindable<int> ComboIndexBindable = new Bindable<int>();

        // Nécessaire pour que IsHovered soit mis à jour et serve au test de visée.
        public override bool HandlePositionalInput => true;

        /// <summary>
        /// Politique de « note lock » fournie par le playfield : décide si une frappe est acceptée.
        /// </summary>
        public Func<DrawableHitObject, double, HitResult, ClickAction>? CheckHittable;

        protected DrawableCalibreHitObject(CalibreHitObject? hitObject)
            : base(hitObject!)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Alpha = 0;
        }

        protected override void OnApply()
        {
            base.OnApply();

            PositionBindable.BindTo(HitObject.PositionBindable);
            ScaleBindable.BindTo(HitObject.ScaleBindable);
            IndexInCurrentComboBindable.BindTo(HitObject.IndexInCurrentComboBindable);
            ComboIndexBindable.BindTo(HitObject.ComboIndexBindable);
        }

        protected override void OnFree()
        {
            base.OnFree();

            PositionBindable.UnbindFrom(HitObject.PositionBindable);
            ScaleBindable.UnbindFrom(HitObject.ScaleBindable);
            IndexInCurrentComboBindable.UnbindFrom(HitObject.IndexInCurrentComboBindable);
            ComboIndexBindable.UnbindFrom(HitObject.ComboIndexBindable);
        }

        protected sealed override double InitialLifetimeOffset => HitObject.TimePreempt;

        public virtual void Shake()
        {
        }

        public void MissForcefully() => ApplyMinResult();
    }
}

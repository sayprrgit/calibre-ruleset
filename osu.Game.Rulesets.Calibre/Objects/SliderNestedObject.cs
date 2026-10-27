// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Objects
{
    /// <summary>
    /// Élément d'un slider jugé sur le suivi du curseur (tick, répétition, queue), sans fenêtre de timing.
    /// </summary>
    public abstract class SliderNestedObject : CalibreHitObject
    {
        /// <summary>
        /// Indice du parcours (aller ou retour) auquel appartient l'élément.
        /// </summary>
        public int SpanIndex { get; set; }

        public double SliderStartTime { get; set; }

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            // L'élément apparaît en même temps que son slider, et non un préempt avant son propre instant.
            TimePreempt += StartTime - SliderStartTime;
        }

        protected override HitWindows CreateHitWindows() => HitWindows.Empty;
    }
}

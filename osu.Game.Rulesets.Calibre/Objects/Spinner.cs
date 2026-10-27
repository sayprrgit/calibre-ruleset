// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Objects
{
    /// <summary>
    /// Spinner : le joueur doit effectuer un nombre minimal de tours, touche enfoncée, avant la fin.
    /// </summary>
    public class Spinner : CalibreHitObject, IHasDuration
    {
        /// <summary>
        /// Vitesse minimale exigée (tours par seconde) selon l'OD, avant le facteur de correspondance avec osu!stable.
        /// </summary>
        public static readonly DifficultyRange MINIMUM_ROTATIONS_PER_SECOND = new DifficultyRange(3, 5, 7.5);

        private const double stable_matching_fudge = 0.6;

        public double EndTime
        {
            get => StartTime + Duration;
            set => Duration = value - StartTime;
        }

        public double Duration { get; set; }

        /// <summary>
        /// Nombre de tours complets requis pour un jugement Great.
        /// </summary>
        public int SpinsRequired { get; private set; } = 1;

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            double minimumRotationsPerSecond = stable_matching_fudge * IBeatmapDifficultyInfo.DifficultyRange(difficulty.OverallDifficulty, MINIMUM_ROTATIONS_PER_SECOND);

            SpinsRequired = (int)(minimumRotationsPerSecond * Math.Max(0, Duration) / 1000);
        }

        public override Judgement CreateJudgement() => new CalibreJudgement();

        // Pas de fenêtre de timing : un spinner est jugé sur sa rotation, et ne doit pas entrer dans l'UR.
        protected override HitWindows CreateHitWindows() => HitWindows.Empty;
    }
}

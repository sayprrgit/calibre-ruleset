// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Calibre.Scoring;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects
{
    /// <summary>
    /// Base de tous les objets Calibre. Les grandeurs (rayon, préempt, fenêtres) reprennent celles d'osu!standard
    /// pour que les statistiques mesurées ici soient directement comparables à celles du jeu.
    /// </summary>
    public abstract class CalibreHitObject : HitObject, IHasPosition, IHasComboInformation, IHasTimePreempt
    {
        /// <summary>
        /// Rayon d'un objet à CS 0 avant mise à l'échelle, en pixels de l'aire de jeu (512x384).
        /// </summary>
        public const float OBJECT_RADIUS = 64;

        public static readonly Vector2 OBJECT_DIMENSIONS = new Vector2(OBJECT_RADIUS * 2);

        public const double PREEMPT_MIN = 450;
        public const double PREEMPT_MID = 1200;
        public const double PREEMPT_MAX = 1800;

        /// <summary>
        /// Temps d'apparition avant l'instant de frappe, interpolé selon l'AR : 1800 ms à AR 0, 1200 ms à AR 5, 450 ms à AR 10.
        /// </summary>
        public static readonly DifficultyRange PREEMPT_RANGE = new DifficultyRange(PREEMPT_MAX, PREEMPT_MID, PREEMPT_MIN);

        public double TimePreempt { get; set; } = 600;

        public double TimeFadeIn { get; set; } = 400;

        private HitObjectProperty<Vector2> position;

        public Bindable<Vector2> PositionBindable => position.Bindable;

        public Vector2 Position
        {
            get => position.Value;
            set => position.Value = value;
        }

        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(Position.X, value);
        }

        private HitObjectProperty<float> scale = new HitObjectProperty<float>(1);

        public Bindable<float> ScaleBindable => scale.Bindable;

        public float Scale
        {
            get => scale.Value;
            set => scale.Value = value;
        }

        public float Radius => OBJECT_RADIUS * Scale;

        #region Combo

        public bool NewCombo { get; set; }

        private HitObjectProperty<int> comboOffset;

        public Bindable<int> ComboOffsetBindable => comboOffset.Bindable;

        public int ComboOffset
        {
            get => comboOffset.Value;
            set => comboOffset.Value = value;
        }

        private HitObjectProperty<int> indexInCurrentCombo;

        public Bindable<int> IndexInCurrentComboBindable => indexInCurrentCombo.Bindable;

        public int IndexInCurrentCombo
        {
            get => indexInCurrentCombo.Value;
            set => indexInCurrentCombo.Value = value;
        }

        private HitObjectProperty<int> comboIndex;

        public Bindable<int> ComboIndexBindable => comboIndex.Bindable;

        public int ComboIndex
        {
            get => comboIndex.Value;
            set => comboIndex.Value = value;
        }

        private HitObjectProperty<int> comboIndexWithOffsets;

        public Bindable<int> ComboIndexWithOffsetsBindable => comboIndexWithOffsets.Bindable;

        public int ComboIndexWithOffsets
        {
            get => comboIndexWithOffsets.Value;
            set => comboIndexWithOffsets.Value = value;
        }

        private HitObjectProperty<bool> lastInCombo;

        public Bindable<bool> LastInComboBindable => lastInCombo.Bindable;

        public bool LastInCombo
        {
            get => lastInCombo.Value;
            set => lastInCombo.Value = value;
        }

        #endregion

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            TimePreempt = IBeatmapDifficultyInfo.DifficultyRangeInt(difficulty.ApproachRate, PREEMPT_RANGE);

            // Au-delà d'AR 10 (via mods), le préempt passe sous 450 ms : on raccourcit le fondu
            // proportionnellement pour que l'objet soit entièrement visible avant d'être frappable.
            TimeFadeIn = 400 * Math.Min(1, TimePreempt / PREEMPT_MIN);

            Scale = LegacyRulesetExtensions.CalculateScaleFromCircleSize(difficulty.CircleSize, true);
        }

        protected override HitWindows CreateHitWindows() => new CalibreHitWindows();
    }
}

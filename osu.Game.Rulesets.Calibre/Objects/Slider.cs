// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
// Calcul de la vitesse et des objets imbriqués adapté du Slider d'osu! (https://github.com/ppy/osu), © ppy Pty Ltd, licence MIT.

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Objects
{
    /// <summary>
    /// Slider : une tête jugée comme un cercle, puis des ticks, des répétitions et une queue jugés sur le suivi du curseur.
    /// Le slider lui-même ne rapporte rien : il sert de conteneur et ne compte que pour la durée de la partie.
    /// </summary>
    public class Slider : CalibreHitObject, IHasPathWithRepeats, IHasSliderVelocity, IHasGenerateTicks
    {
        /// <summary>
        /// Distance parcourue en un temps à multiplicateur de vitesse 1 (constante historique d'osu!).
        /// </summary>
        public const double BASE_SCORING_DISTANCE = 100;

        /// <summary>
        /// Rayon de la zone de suivi, en multiple du rayon de l'objet, une fois le suivi acquis.
        /// </summary>
        public const float FOLLOW_AREA = 2.4f;

        public double EndTime => StartTime + this.SpanCount() * Path.Distance / Velocity;

        public double Duration
        {
            get => EndTime - StartTime;
            set => throw new NotSupportedException($"La durée d'un slider découle de son chemin : modifier {nameof(RepeatCount)} ou {nameof(Path)}.");
        }

        public SliderPath Path { get; set; } = new SliderPath();

        public double Distance => Path.Distance;

        public int RepeatCount { get; set; }

        public IList<IList<HitSampleInfo>> NodeSamples { get; set; } = new List<IList<HitSampleInfo>>();

        public BindableNumber<double> SliderVelocityMultiplierBindable { get; } = new BindableDouble(1)
        {
            Precision = 0.01,
            MinValue = 0.1,
            MaxValue = 10,
        };

        public double SliderVelocityMultiplier
        {
            get => SliderVelocityMultiplierBindable.Value;
            set => SliderVelocityMultiplierBindable.Value = value;
        }

        public bool GenerateTicks { get; set; } = true;

        /// <summary>
        /// Correctif des beatmaps antérieures au format v8, où la vitesse ne modifiait pas l'espacement des ticks.
        /// </summary>
        public double TickDistanceMultiplier { get; set; } = 1;

        /// <summary>
        /// Vitesse en pixels de l'aire de jeu par milliseconde.
        /// </summary>
        public double Velocity { get; private set; } = 1;

        public double TickDistance { get; private set; }

        public double SpanDuration => Duration / this.SpanCount();

        public SliderHeadCircle? HeadCircle { get; private set; }

        public SliderTailCircle? TailCircle { get; private set; }

        public Vector2 EndPosition => Position + this.CurvePositionAt(1);

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            var timingPoint = controlPointInfo.TimingPointAt(StartTime);

            // Les beatmaps converties viennent d'osu!standard : on applique ses bornes de vitesse (ruleset « osu »).
            Velocity = BASE_SCORING_DISTANCE * difficulty.SliderMultiplier / LegacyRulesetExtensions.GetPrecisionAdjustedBeatLength(this, timingPoint, "osu");

            // Volontairement recalculé depuis la vitesse plutôt que BASE_SCORING_DISTANCE * SliderMultiplier :
            // osu! fait de même pour reproduire les erreurs d'arrondi d'osu!stable sur l'espacement des ticks.
            double scoringDistance = Velocity * timingPoint.BeatLength;

            TickDistance = GenerateTicks ? scoringDistance / difficulty.SliderTickRate * TickDistanceMultiplier : double.PositiveInfinity;
        }

        protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
        {
            base.CreateNestedHitObjects(cancellationToken);

            HeadCircle = null;
            TailCircle = null;

            foreach (var e in SliderEventGenerator.Generate(StartTime, SpanDuration, Velocity, TickDistance, Path.Distance, this.SpanCount(), cancellationToken))
            {
                switch (e.Type)
                {
                    case SliderEventType.Head:
                        // Numéro et couleur de combo : HitObject.ApplyDefaults lie ceux des objets imbriqués à ceux du slider.
                        AddNested(HeadCircle = new SliderHeadCircle
                        {
                            StartTime = e.Time,
                            Position = Position,
                        });
                        break;

                    case SliderEventType.Tick:
                        AddNested(new SliderTick
                        {
                            SpanIndex = e.SpanIndex,
                            SliderStartTime = StartTime,
                            StartTime = e.Time,
                            Position = Position + Path.PositionAt(e.PathProgress),
                        });
                        break;

                    case SliderEventType.Repeat:
                        AddNested(new SliderRepeat
                        {
                            SpanIndex = e.SpanIndex,
                            SliderStartTime = StartTime,
                            StartTime = StartTime + (e.SpanIndex + 1) * SpanDuration,
                            Position = Position + Path.PositionAt(e.PathProgress),
                            ArrowRotation = arrowRotationAt(e.PathProgress),
                        });
                        break;

                    case SliderEventType.Tail:
                        AddNested(TailCircle = new SliderTailCircle
                        {
                            SpanIndex = e.SpanIndex,
                            SliderStartTime = StartTime,
                            StartTime = e.Time,
                            Position = EndPosition,
                        });
                        break;

                    // LegacyLastTick n'est pas un objet : la tolérance de 36 ms sur la queue est appliquée par DrawableSlider.
                }
            }

            updateNestedSamples();
        }

        /// <summary>
        /// Orientation (en degrés) de la flèche d'une répétition : elle pointe vers l'intérieur du chemin, dans le sens du retour.
        /// </summary>
        private float arrowRotationAt(double pathProgress)
        {
            const double step = 0.01;

            Vector2 at = Path.PositionAt(pathProgress);
            Vector2 inward = Path.PositionAt(pathProgress >= 0.5 ? pathProgress - step : pathProgress + step) - at;

            return MathF.Atan2(inward.Y, inward.X) * 180 / MathF.PI;
        }

        private void updateNestedSamples()
        {
            // Les extrémités (tête, répétitions, queue) portent les sons de leur nœud ; ceux du slider servent de repli.
            foreach (var nested in NestedHitObjects)
            {
                switch (nested)
                {
                    case SliderHeadCircle head:
                        head.Samples = this.GetNodeSamples(0);
                        break;

                    case SliderRepeat repeat:
                        repeat.Samples = this.GetNodeSamples(repeat.SpanIndex + 1);
                        break;

                    case SliderTailCircle tail:
                        tail.Samples = this.GetNodeSamples(this.SpanCount());
                        break;
                }
            }
        }

        public override Judgement CreateJudgement() => new CalibreIgnoreJudgement();

        // Le slider n'a pas de fenêtre propre : seule sa tête est jugée en timing (et entre dans l'UR).
        protected override HitWindows CreateHitWindows() => HitWindows.Empty;
    }
}

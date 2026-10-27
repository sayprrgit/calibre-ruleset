// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Réglages d'un exercice. L'intensité multiplie la distance des sauts, la longueur des streams et celle des sliders.
    /// </summary>
    public record ExerciseSettings(double Bpm, double DurationSeconds, double Intensity);

    /// <summary>
    /// Pattern ciblé par un exercice, avec le niveau atteint par le joueur sur ce pattern.
    /// </summary>
    public readonly record struct PatternTarget(PatternType Type, double Level);

    /// <summary>
    /// Génère procéduralement une beatmap d'exercice : des blocs de chaque pattern ciblé, en alternance, jusqu'à la durée voulue.
    /// </summary>
    /// <remarks>
    /// Déterministe à graine égale (un seul <see cref="Random"/> initialisé par la graine), donc testable et rejouable.
    /// Les subdivisions, espacements et angles reprennent les seuils de <see cref="PatternClassifier"/> : l'analyse
    /// d'un exercice retrouve les patterns qu'il entraîne.
    /// </remarks>
    public static class ExerciseGenerator
    {
        /// <summary>
        /// À 120 BPM, une double croche dure 125 ms : le plus lent pour qu'un stream reste un stream pour l'analyse.
        /// </summary>
        public const double MINIMUM_BPM = 120;

        public const double MAXIMUM_BPM = 240;

        /// <summary>
        /// Premier objet à 2 s : de quoi jouer quatre temps de décompte au métronome, même à 120 BPM.
        /// </summary>
        public const double START_TIME = 2000;

        public const float CIRCLE_SIZE = 4;

        private const int jumps_per_block = 8;
        private const int sliders_per_block = 4;
        private const int base_stream_length = 8;

        public static readonly float RADIUS = CalibreHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(CIRCLE_SIZE, true);

        private static float diameter => 2 * RADIUS;

        public static Beatmap<CalibreHitObject> Generate(ExerciseSettings settings, IReadOnlyList<PatternTarget> targets, int seed)
        {
            if (targets.Count == 0)
                throw new ArgumentException("Un exercice cible au moins un pattern.", nameof(targets));
            if (settings.Bpm < MINIMUM_BPM || settings.Bpm > MAXIMUM_BPM)
                throw new ArgumentOutOfRangeException(nameof(settings), settings.Bpm, $"Le BPM doit être entre {MINIMUM_BPM} et {MAXIMUM_BPM}.");

            foreach (var target in targets)
            {
                if (target.Type == PatternType.Isolated)
                    throw new ArgumentException("Les objets isolés ne s'entraînent pas.", nameof(targets));
            }

            double beat = 60000 / settings.Bpm;

            var beatmap = new Beatmap<CalibreHitObject>
            {
                Difficulty = new BeatmapDifficulty
                {
                    CircleSize = CIRCLE_SIZE,
                    ApproachRate = 9,
                    OverallDifficulty = 8,
                    DrainRate = 3,
                    SliderMultiplier = 1.4,
                    SliderTickRate = 1,
                },
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beat });

            var state = new GenerationState(new Random(seed), beat, beatmap);
            double end = START_TIME + settings.DurationSeconds * 1000;

            for (int block = 0; state.Time < end; block++)
            {
                var target = targets[block % targets.Count];
                double scale = settings.Intensity * target.Level;
                var band = TurnBand.For(target.Type);

                float distance = moveDistance(target.Type, scale);

                // Le bloc commence à côté du dernier objet, pas empilé dessus, et dans une direction d'où il peut se dérouler.
                if (block > 0)
                    state.TurnAndMoveInside(state.NextDouble(-90, 90), TurnBand.ANY, 1.5f * diameter, band, distance, GenerationState.MAXIMUM_LOOKAHEAD);

                switch (target.Type)
                {
                    case PatternType.Stream:
                        addStream(state, (int)Math.Clamp(Math.Round(base_stream_length * scale), 4, 32), distance);
                        break;

                    case PatternType.Slider:
                        addSliders(state, Math.Clamp(100 * scale, 60, 220), distance);
                        break;

                    default:
                        addJumps(state, band, target.Type == PatternType.LongWideJump ? 4 : jumps_per_block, distance);
                        break;
                }

                // Une noire de repos entre deux blocs.
                state.Time += beat;
            }

            return beatmap;
        }

        /// <summary>
        /// Distance entre deux objets d'un bloc. Sauts courts entre 1,7 et 2,9 diamètres, longs entre 3,2 et 4,5
        /// (selon l'intensité et le niveau) ; stream à 0,6 diamètre ; sliders séparés d'1,5 diamètre.
        /// </summary>
        /// <remarks>
        /// Des sauts longs qui tournent chacun de moins de 90° ne peuvent pas boucler dans l'aire de jeu : leurs blocs
        /// comptent 4 objets (trois sauts en équerre), à 3,8 diamètres au plus pour tenir dans sa hauteur.
        /// </remarks>
        private static float moveDistance(PatternType type, double scale)
        {
            switch (type)
            {
                case PatternType.Stream:
                    return 0.6f * diameter;

                case PatternType.Slider:
                    return 1.5f * diameter;

                case PatternType.LongWideJump:
                    return diameter * (float)Math.Clamp(3.4 * scale, 3.2, 3.8);

                case PatternType.LongSharpJump:
                    return diameter * (float)Math.Clamp(3.6 * scale, 3.2, 4.5);

                default:
                    return diameter * (float)Math.Clamp(2.2 * scale, 1.7, 2.9);
            }
        }

        /// <summary>
        /// Stream en double croches qui tourne doucement.
        /// </summary>
        private static void addStream(GenerationState state, int length, float spacing)
        {
            for (int i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    state.Turn(state.NextDouble(-20, 20));
                    state.Move(spacing);
                }

                state.AddCircle();
                state.Time += state.Beat / 4;
            }
        }

        /// <summary>
        /// Sauts en croches. Angle : aigu = presque demi-tour, ouvert = presque tout droit, en restant dans la plage de la catégorie.
        /// </summary>
        private static void addJumps(GenerationState state, TurnBand band, int count, float distance)
        {
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    double preferred = band.Centre + state.NextDouble(-35, 35);

                    // Le tout premier saut de l'exercice n'a pas d'objet avant-avant : son angle n'est pas mesuré, sa direction est libre.
                    state.TurnAndMoveInside(preferred, state.IsFirstMove ? TurnBand.ANY : band, distance, band, distance, count - 1 - i);
                }

                state.AddCircle();
                state.Time += state.Beat / 2;
            }
        }

        /// <summary>
        /// Sliders droits, séparés d'une croche et d'un saut.
        /// </summary>
        private static void addSliders(GenerationState state, double length, float gap)
        {
            for (int i = 0; i < sliders_per_block; i++)
            {
                if (i > 0)
                    state.TurnAndMoveInside(state.NextDouble(-60, 60), TurnBand.ANY, gap, TurnBand.ANY, gap, 1);

                state.AddSlider((float)length);
                state.Time += state.Beat / 2;
            }
        }

        /// <summary>
        /// Plage de rotations autorisées entre deux sauts, en degrés : centre ± demi-largeur.
        /// </summary>
        /// <remarks>
        /// L'angle au sommet vaut 180° - |rotation| : une rotation de plus de 90° donne un saut aigu (sommet sous 90°),
        /// de moins de 90° un saut ouvert. Les plages s'arrêtent à 5° de cette limite.
        /// </remarks>
        private readonly record struct TurnBand(double Centre, double HalfWidth)
        {
            public static readonly TurnBand ANY = new TurnBand(0, 180);
            public static readonly TurnBand WIDE = new TurnBand(0, 85);
            public static readonly TurnBand SHARP = new TurnBand(180, 85);

            public double this[int step, int steps] => Centre - HalfWidth + 2 * HalfWidth * step / (steps - 1);

            public static TurnBand For(PatternType type)
            {
                switch (type)
                {
                    case PatternType.ShortSharpJump:
                    case PatternType.LongSharpJump:
                        return SHARP;

                    case PatternType.Slider:
                        return ANY;

                    default:
                        return WIDE;
                }
            }
        }

        /// <summary>
        /// Position, direction et instant courants de la génération.
        /// </summary>
        private class GenerationState
        {
            /// <summary>
            /// Anticipation maximale, en sauts : assez pour finir un bloc de sauts longs ouverts (trois sauts).
            /// </summary>
            public const int MAXIMUM_LOOKAHEAD = 3;

            private const int band_steps = 25;

            /// <summary>
            /// Balayage plus grossier pour l'anticipation : 13 rotations par saut, 13³ chemins au plus.
            /// </summary>
            private const int lookahead_steps = 13;

            private static readonly Vector2 minimum = new Vector2(RADIUS);
            private static readonly Vector2 maximum = CalibrePlayfield.BASE_SIZE - new Vector2(RADIUS);

            /// <summary>
            /// Vitesse des sliders en px/ms, comme <see cref="Slider"/> la calculera : 100 px × multiplicateur par temps.
            /// </summary>
            private double sliderVelocity => Slider.BASE_SCORING_DISTANCE * 1.4 / Beat;

            public readonly double Beat;
            public double Time = START_TIME;

            public bool IsFirstMove => beatmap.HitObjects.Count == 1;

            private readonly Random random;
            private readonly Beatmap<CalibreHitObject> beatmap;

            private Vector2 position = CalibrePlayfield.BASE_SIZE / 2;
            private float direction;

            public GenerationState(Random random, double beat, Beatmap<CalibreHitObject> beatmap)
            {
                this.random = random;
                this.beatmap = beatmap;
                Beat = beat;
                direction = (float)(random.NextDouble() * 2 * Math.PI);
            }

            public double NextDouble(double minimumValue, double maximumValue) => minimumValue + random.NextDouble() * (maximumValue - minimumValue);

            public void Turn(double degrees) => direction += radians(degrees);

            /// <summary>
            /// Avance dans la direction courante ; au bord de l'aire de jeu, la direction rebondit comme sur un mur.
            /// </summary>
            public void Move(float distance)
            {
                var next = position + distance * heading(direction);

                if (next.X < minimum.X || next.X > maximum.X)
                    direction = MathF.PI - direction;
                if (next.Y < minimum.Y || next.Y > maximum.Y)
                    direction = -direction;

                position = Vector2.Clamp(position + distance * heading(direction), minimum, maximum);
            }

            /// <summary>
            /// Tourne d'une rotation de <paramref name="band"/> puis saute de <paramref name="distance"/>.
            /// </summary>
            /// <remarks>
            /// La rotation tirée (<paramref name="preferred"/>) est gardée si le chemin peut ensuite enchaîner
            /// <paramref name="lookahead"/> sauts (plage <paramref name="nextBand"/>, distance <paramref name="nextDistance"/>)
            /// en gardant de la marge. Sinon, on balaie toute la plage et on garde la rotation qui laisse le plus de marge :
            /// un chemin qui fonce dans un coin ne pourrait en repartir qu'en faisant demi-tour, ce qui changerait un saut
            /// ouvert en saut aigu.
            /// </remarks>
            public void TurnAndMoveInside(double preferred, TurnBand band, float distance, TurnBand nextBand, float nextDistance, int lookahead)
            {
                float bestScore = float.NegativeInfinity;
                float bestAngle = direction;
                int depth = Math.Min(lookahead, MAXIMUM_LOOKAHEAD);

                for (int step = -1; step < band_steps; step++)
                {
                    float angle = direction + radians(step < 0 ? preferred : band[step, band_steps]);
                    var next = position + distance * heading(angle);

                    if (roomAt(next) < 0)
                        continue;

                    float score = MathF.Min(roomAt(next), pathRoom(next, angle, nextBand, nextDistance, depth));

                    if (step < 0 && score >= nextDistance / 4)
                    {
                        bestAngle = angle;
                        bestScore = score;
                        break;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAngle = angle;
                    }
                }

                if (bestScore > float.NegativeInfinity)
                {
                    direction = bestAngle;
                    position += distance * heading(bestAngle);
                    return;
                }

                // shortcut: aucune rotation de la plage ne tient dans l'aire de jeu, on rogne le saut contre le bord ; à revoir si les tests de classement le détectent.
                Turn(preferred);
                Move(distance);
            }

            /// <summary>
            /// Meilleure marge minimale le long d'un chemin de <paramref name="depth"/> sauts depuis <paramref name="from"/>,
            /// chacun de la plage <paramref name="band"/> et de longueur <paramref name="distance"/>.
            /// </summary>
            private static float pathRoom(Vector2 from, float angle, TurnBand band, float distance, int depth)
            {
                if (depth == 0)
                    return float.PositiveInfinity;

                float best = float.NegativeInfinity;

                for (int step = 0; step < lookahead_steps; step++)
                {
                    float next = angle + radians(band[step, lookahead_steps]);
                    var point = from + distance * heading(next);
                    float room = roomAt(point);

                    best = MathF.Max(best, room < 0 ? room : MathF.Min(room, pathRoom(point, next, band, distance, depth - 1)));
                }

                return best;
            }

            /// <summary>
            /// Distance au bord le plus proche de la zone utile ; négative hors de l'aire de jeu.
            /// </summary>
            private static float roomAt(Vector2 point) => MathF.Min(MathF.Min(point.X - minimum.X, maximum.X - point.X), MathF.Min(point.Y - minimum.Y, maximum.Y - point.Y));

            private static Vector2 heading(float angle) => new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            private static float radians(double degrees) => (float)(degrees * Math.PI / 180);

            public void AddCircle()
                => beatmap.HitObjects.Add(new HitCircle { StartTime = Time, Position = position, NewCombo = beatmap.HitObjects.Count % 8 == 0 });

            public void AddSlider(float length)
            {
                var path = heading(direction) * length;

                // Le slider garde la direction courante si elle tient dans l'aire de jeu, sinon il part dans l'autre sens ;
                // si aucun des deux ne tient, il part à l'horizontale vers le centre (la largeur utile dépasse toujours sa longueur).
                if (roomAt(position + path) < 0)
                    path = -path;
                if (roomAt(position + path) < 0)
                    path = new Vector2(position.X < CalibrePlayfield.BASE_SIZE.X / 2 ? length : -length, 0);

                beatmap.HitObjects.Add(new Slider
                {
                    StartTime = Time,
                    Position = position,
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, path }),
                    NewCombo = true,
                });

                position = Vector2.Clamp(position + path, minimum, maximum);
                direction = MathF.Atan2(path.Y, path.X);
                Time += length / sliderVelocity;
            }
        }
    }
}

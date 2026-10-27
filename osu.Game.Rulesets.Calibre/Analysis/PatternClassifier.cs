// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Analysis
{
    public enum PatternType
    {
        Stream,
        ShortSharpJump,
        ShortWideJump,
        LongSharpJump,
        LongWideJump,
        Slider,
        Isolated,
    }

    /// <summary>
    /// Bilan d'un type de pattern : précision sur tous ses objets, timing sur ses frappes réussies.
    /// </summary>
    public class PatternStats
    {
        public PatternType Type { get; }

        public int Count { get; }

        /// <summary>
        /// Précision au sens de lazer : Great = 1, Ok = 1/3, Meh = 1/6, Miss = 0 (rapports 300/100/50).
        /// </summary>
        public double Accuracy { get; }

        public OffsetStatistics Timing { get; }

        public PatternStats(PatternType type, IReadOnlyList<TimedHit> hits)
        {
            Type = type;
            Count = hits.Count;
            Accuracy = hits.Count == 0 ? 0 : hits.Average(h => accuracyOf(h.Result));
            Timing = OffsetStatistics.From(hits.Where(h => h.IsHit).Select(h => h.Offset).ToArray());
        }

        private static double accuracyOf(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                    return 1;

                case HitResult.Ok:
                    return 1 / 3.0;

                case HitResult.Meh:
                    return 1 / 6.0;

                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// Classe chaque frappe selon le mouvement qui y mène depuis l'objet précédent.
    /// </summary>
    /// <remarks>
    /// Ordre de décision : tête de slider → slider ; plus de <see cref="ISOLATED_GAP"/> ms depuis la fin de l'objet précédent → isolé ;
    /// moins de <see cref="STREAM_GAP"/> ms et un espacement d'au plus <see cref="STREAM_MAX_SPACING"/> diamètres → stream ;
    /// sinon saut, court ou long selon <see cref="LONG_JUMP_SPACING"/>, aigu ou ouvert selon <see cref="SHARP_ANGLE"/>.
    /// </remarks>
    public static class PatternClassifier
    {
        /// <summary>
        /// Au-delà d'une noire à 120 BPM, l'objet ne s'enchaîne plus avec le précédent.
        /// </summary>
        public const double ISOLATED_GAP = 500;

        /// <summary>
        /// Une double croche à 120 BPM : au-dessous, les frappes s'enchaînent en rafale.
        /// </summary>
        public const double STREAM_GAP = 125;

        public const double STREAM_MAX_SPACING = 1.5;

        public const double LONG_JUMP_SPACING = 3;

        /// <summary>
        /// Angle au sommet (objet précédent) entre l'arrivée et le départ : 180° = ligne droite, 0° = aller-retour.
        /// </summary>
        public const double SHARP_ANGLE = 90;

        public static PatternType Classify(IReadOnlyList<TimedHit> hits, int index)
        {
            var hit = hits[index];

            if (hit.IsSlider)
                return PatternType.Slider;

            if (index == 0 || hit.Time - hits[index - 1].EndTime > ISOLATED_GAP)
                return PatternType.Isolated;

            var previous = hits[index - 1];
            double gap = hit.Time - previous.EndTime;
            double spacing = Vector2.Distance(previous.EndPosition, hit.Target) / (2 * hit.Radius);

            if (gap <= STREAM_GAP && spacing <= STREAM_MAX_SPACING)
                return PatternType.Stream;

            bool sharp = index >= 2 && angleAt(hits[index - 2].EndPosition, previous, hit.Target) < SHARP_ANGLE;

            if (spacing >= LONG_JUMP_SPACING)
                return sharp ? PatternType.LongSharpJump : PatternType.LongWideJump;

            return sharp ? PatternType.ShortSharpJump : PatternType.ShortWideJump;
        }

        /// <summary>
        /// Un bilan par type de pattern présent, dans l'ordre de <see cref="PatternType"/>.
        /// </summary>
        public static IReadOnlyList<PatternStats> Summarise(IReadOnlyList<TimedHit> hits)
        {
            var types = new PatternType[hits.Count];

            for (int i = 0; i < hits.Count; i++)
                types[i] = Classify(hits, i);

            return Enum.GetValues<PatternType>()
                       .Select(type => new PatternStats(type, hits.Where((_, i) => types[i] == type).ToArray()))
                       .Where(stats => stats.Count > 0)
                       .ToArray();
        }

        public static string DisplayName(PatternType type)
        {
            switch (type)
            {
                case PatternType.Stream:
                    return "Streams";

                case PatternType.ShortSharpJump:
                    return "Sauts courts, angle aigu";

                case PatternType.ShortWideJump:
                    return "Sauts courts, angle ouvert";

                case PatternType.LongSharpJump:
                    return "Sauts longs, angle aigu";

                case PatternType.LongWideJump:
                    return "Sauts longs, angle ouvert";

                case PatternType.Slider:
                    return "Sliders";

                default:
                    return "Objets isolés";
            }
        }

        /// <summary>
        /// Angle en degrés au sommet <paramref name="vertex"/>, entre l'arrivée depuis <paramref name="from"/> et le départ vers
        /// <paramref name="to"/>. Un objet empilé (vecteur nul) compte comme une ligne droite.
        /// </summary>
        private static double angleAt(Vector2 from, TimedHit vertex, Vector2 to)
        {
            var incoming = from - vertex.Target;
            var outgoing = to - vertex.EndPosition;

            if (incoming.LengthSquared < 1 || outgoing.LengthSquared < 1)
                return 180;

            double cosine = Vector2.Dot(incoming, outgoing) / (incoming.Length * outgoing.Length);
            return Math.Acos(Math.Clamp(cosine, -1, 1)) * 180 / Math.PI;
        }
    }
}

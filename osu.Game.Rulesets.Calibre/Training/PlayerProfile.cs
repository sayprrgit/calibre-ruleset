// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using osu.Game.Rulesets.Calibre.Analysis;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Cumul d'un type de pattern sur toutes les parties analysées, et niveau de difficulté de ses exercices.
    /// </summary>
    /// <remarks>
    /// On garde des sommes plutôt que des moyennes : l'UR de toutes les parties se recalcule exactement
    /// (variance = moyenne des carrés - carré de la moyenne), quel que soit le nombre de frappes de chacune.
    /// </remarks>
    public class PatternRecord
    {
        public int Objects { get; set; }

        /// <summary>
        /// Somme des précisions pondérées par le nombre d'objets de chaque partie.
        /// </summary>
        public double AccuracySum { get; set; }

        public int TimedHits { get; set; }

        public double OffsetSum { get; set; }

        public double OffsetSquareSum { get; set; }

        /// <summary>
        /// Multiplicateur de difficulté des exercices de ce pattern (voir <see cref="DifficultyController"/>).
        /// </summary>
        public double Level { get; set; } = DifficultyController.INITIAL_LEVEL;

        [JsonIgnore]
        public double Accuracy => Objects == 0 ? 0 : AccuracySum / Objects;

        [JsonIgnore]
        public double? UnstableRate
        {
            get
            {
                if (TimedHits == 0)
                    return null;

                double mean = OffsetSum / TimedHits;
                return 10 * Math.Sqrt(Math.Max(0, OffsetSquareSum / TimedHits - mean * mean));
            }
        }
    }

    /// <summary>
    /// Profil local du joueur : statistiques par pattern cumulées après chaque partie, et niveau de chaque exercice.
    /// </summary>
    public class PlayerProfile
    {
        public const int CURRENT_SCHEMA_VERSION = 1;

        /// <summary>
        /// En dessous, un pattern n'a pas été assez joué pour être jugé faible.
        /// </summary>
        public const int MINIMUM_OBJECTS = 20;

        /// <summary>
        /// En dessous, une partie ne compte pas assez d'objets d'un pattern pour faire évoluer son niveau.
        /// </summary>
        public const int MINIMUM_OBJECTS_FOR_LEVEL = 10;

        /// <summary>
        /// Patterns qu'un exercice peut cibler : un objet isolé n'est pas un enchaînement à travailler.
        /// </summary>
        public static readonly PatternType[] TRAINABLE = Enum.GetValues<PatternType>().Where(t => t != PatternType.Isolated).ToArray();

        public int SchemaVersion { get; set; } = CURRENT_SCHEMA_VERSION;

        public int GamesAnalysed { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public Dictionary<PatternType, PatternRecord> Patterns { get; set; } = new Dictionary<PatternType, PatternRecord>();

        /// <summary>
        /// Ajoute le bilan d'une partie : cumuls par pattern, puis ajustement du niveau des patterns assez représentés.
        /// </summary>
        public void Apply(IEnumerable<PatternStats> game, DateTimeOffset now)
        {
            foreach (var stats in game)
            {
                if (!Patterns.TryGetValue(stats.Type, out var record))
                    Patterns[stats.Type] = record = new PatternRecord();

                record.Objects += stats.Count;
                record.AccuracySum += stats.Accuracy * stats.Count;

                if (stats.Timing.Mean is double mean && stats.Timing.UnstableRate is double unstableRate)
                {
                    // L'UR est 10 fois l'écart-type de population : on en déduit la somme des carrés de la partie.
                    int n = stats.Timing.Count;
                    double variance = unstableRate / 10 * (unstableRate / 10);

                    record.TimedHits += n;
                    record.OffsetSum += mean * n;
                    record.OffsetSquareSum += (variance + mean * mean) * n;
                }

                if (stats.Count >= MINIMUM_OBJECTS_FOR_LEVEL)
                    record.Level = DifficultyController.Next(record.Level, stats.Accuracy);
            }

            GamesAnalysed++;
            LastUpdated = now;
        }

        /// <summary>
        /// Les patterns entraînables les plus faibles : précision la plus basse d'abord, puis UR le plus haut.
        /// </summary>
        public IReadOnlyList<PatternType> WeakestPatterns(int count = 3)
            => TRAINABLE.Where(t => Patterns.TryGetValue(t, out var r) && r.Objects >= MINIMUM_OBJECTS)
                        .OrderBy(t => Patterns[t].Accuracy)
                        .ThenByDescending(t => Patterns[t].UnstableRate ?? 0)
                        .Take(count)
                        .ToArray();

        public double LevelOf(PatternType type) => Patterns.TryGetValue(type, out var record) ? record.Level : DifficultyController.INITIAL_LEVEL;
    }

    /// <summary>
    /// Difficulté progressive : le niveau d'un pattern monte après une bonne partie, descend après une mauvaise.
    /// </summary>
    public static class DifficultyController
    {
        public const double INITIAL_LEVEL = 1;
        public const double MINIMUM_LEVEL = 0.5;
        public const double MAXIMUM_LEVEL = 2;
        public const double STEP = 0.1;

        /// <summary>
        /// Au-dessus de 95 % de précision, l'exercice devient un peu plus dur.
        /// </summary>
        public const double RAISE_THRESHOLD = 0.95;

        /// <summary>
        /// Sous 80 %, il devient un peu plus facile. Entre les deux, le niveau ne bouge pas.
        /// </summary>
        public const double LOWER_THRESHOLD = 0.80;

        public static double Next(double level, double accuracy)
        {
            if (accuracy >= RAISE_THRESHOLD)
                level += STEP;
            else if (accuracy < LOWER_THRESHOLD)
                level -= STEP;

            // Arrondi au centième : des pas répétés de 0,1 ne doivent pas accumuler d'erreur flottante.
            return Math.Round(Math.Clamp(level, MINIMUM_LEVEL, MAXIMUM_LEVEL), 2);
        }
    }
}

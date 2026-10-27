// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Analysis
{
    /// <summary>
    /// Passage de la map où l'UR local dépasse nettement l'UR de toute la partie.
    /// </summary>
    public readonly record struct UnstableSection(double StartTime, double EndTime, double UnstableRate);

    public static class UnstableSectionDetector
    {
        /// <summary>
        /// Taille de la fenêtre glissante, en frappes réussies consécutives.
        /// </summary>
        public const int WINDOW = 16;

        /// <summary>
        /// Une fenêtre est instable si son UR atteint 1,25 fois l'UR global...
        /// </summary>
        public const double FACTOR = 1.25;

        /// <summary>
        /// ... et le dépasse d'au moins 10 points, pour ne pas signaler les écarts infimes d'une partie très régulière.
        /// </summary>
        public const double MINIMUM_INCREASE = 10;

        /// <summary>
        /// Fusionne les fenêtres instables qui se chevauchent en sections. Il faut au moins deux fenêtres de frappes
        /// pour qu'une section puisse se distinguer du reste de la partie.
        /// </summary>
        public static IReadOnlyList<UnstableSection> Detect(IReadOnlyList<TimedHit> hits)
        {
            var timed = hits.Where(h => h.IsHit).ToArray();

            if (timed.Length < 2 * WINDOW)
                return Array.Empty<UnstableSection>();

            double global = unstableRate(timed, 0, timed.Length);
            double threshold = Math.Max(global * FACTOR, global + MINIMUM_INCREASE);

            var sections = new List<UnstableSection>();
            int? start = null;
            int end = 0;

            for (int i = 0; i + WINDOW <= timed.Length; i++)
            {
                if (unstableRate(timed, i, WINDOW) >= threshold)
                {
                    start ??= i;
                    end = i + WINDOW - 1;
                }
                else if (start is int s && i > end)
                {
                    sections.Add(section(timed, s, end));
                    start = null;
                }
            }

            if (start is int last)
                sections.Add(section(timed, last, end));

            return sections;
        }

        private static UnstableSection section(TimedHit[] timed, int first, int last)
            => new UnstableSection(timed[first].Time, timed[last].Time, unstableRate(timed, first, last - first + 1));

        private static double unstableRate(TimedHit[] timed, int start, int count)
            => OffsetStatistics.From(timed.Skip(start).Take(count).Select(h => h.Offset).ToArray()).UnstableRate ?? 0;
    }
}

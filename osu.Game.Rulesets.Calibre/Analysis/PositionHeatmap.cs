// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Analysis
{
    /// <summary>
    /// Erreurs de position (curseur - centre de la cible) agrégées sur une grille de l'aire de jeu, selon la position des cibles.
    /// </summary>
    public class PositionHeatmap
    {
        public const int COLUMNS = 16;
        public const int ROWS = 12;

        /// <summary>
        /// Frappes par case.
        /// </summary>
        public readonly int[,] Counts = new int[COLUMNS, ROWS];

        private readonly double[,] errorSums = new double[COLUMNS, ROWS];

        /// <summary>
        /// Frappes positionnées (les misses sans frappe n'ont pas de position).
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Écart moyen en pixels de l'aire de jeu.
        /// </summary>
        public double? MeanError { get; private set; }

        /// <summary>
        /// Décalage moyen du curseur par rapport aux cibles : un biais systématique de visée (ou de calibration).
        /// </summary>
        public Vector2 MeanOffset { get; private set; }

        /// <summary>
        /// Écart moyen dans une case, exprimé en rayons de cible (1 = au bord de la cible). Nul si la case est vide.
        /// </summary>
        public double? MeanErrorAt(int column, int row) => Counts[column, row] == 0 ? null : errorSums[column, row] / Counts[column, row];

        public static PositionHeatmap From(IEnumerable<TimedHit> hits)
        {
            var heatmap = new PositionHeatmap();
            var offsetSum = Vector2.Zero;
            double errorSum = 0;

            foreach (var hit in hits)
            {
                if (hit.Cursor is not Vector2 cursor)
                    continue;

                var offset = cursor - hit.Target;
                int column = Math.Clamp((int)(hit.Target.X / CalibrePlayfield.BASE_SIZE.X * COLUMNS), 0, COLUMNS - 1);
                int row = Math.Clamp((int)(hit.Target.Y / CalibrePlayfield.BASE_SIZE.Y * ROWS), 0, ROWS - 1);

                heatmap.Counts[column, row]++;
                heatmap.errorSums[column, row] += offset.Length / hit.Radius;
                heatmap.Count++;

                offsetSum += offset;
                errorSum += offset.Length;
            }

            if (heatmap.Count > 0)
            {
                heatmap.MeanError = errorSum / heatmap.Count;
                heatmap.MeanOffset = offsetSum / heatmap.Count;
            }

            return heatmap;
        }
    }
}

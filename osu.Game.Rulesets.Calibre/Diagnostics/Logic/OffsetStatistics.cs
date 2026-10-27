// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Statistiques descriptives d'une série d'écarts de timing (en ms).
    /// </summary>
    public class OffsetStatistics
    {
        public int Count { get; }

        /// <summary>
        /// Moyenne des écarts : le biais. Négatif = en avance. Nulle s'il n'y a aucune valeur.
        /// </summary>
        public double? Mean { get; }

        /// <summary>
        /// Variance d'échantillon (dénominateur n - 1), utilisée par les tests statistiques. Nulle sous deux valeurs.
        /// </summary>
        public double? Variance { get; }

        public double? StandardDeviation => Variance is double variance ? Math.Sqrt(variance) : null;

        /// <summary>
        /// Unstable rate : 10 fois l'écart-type de population (dénominateur n), la définition de lazer.
        /// </summary>
        public double? UnstableRate { get; }

        private OffsetStatistics(int count, double? mean, double? variance, double? unstableRate)
        {
            Count = count;
            Mean = mean;
            Variance = variance;
            UnstableRate = unstableRate;
        }

        public static OffsetStatistics From(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
                return new OffsetStatistics(0, null, null, null);

            // Algorithme de Welford : moyenne et somme des carrés des écarts en une passe, numériquement stable.
            double mean = 0;
            double sumOfSquares = 0;

            for (int i = 0; i < values.Count; i++)
            {
                double delta = values[i] - mean;
                mean += delta / (i + 1);
                sumOfSquares += delta * (values[i] - mean);
            }

            int n = values.Count;

            return new OffsetStatistics(
                n,
                mean,
                n >= 2 ? sumOfSquares / (n - 1) : null,
                10 * Math.Sqrt(sumOfSquares / n));
        }
    }

    public static class LinearRegression
    {
        /// <summary>
        /// Pente de la droite des moindres carrés de <paramref name="y"/> en fonction de <paramref name="x"/>.
        /// Nulle s'il y a moins de deux points ou si tous les x sont égaux.
        /// </summary>
        public static double? Slope(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            if (x.Count != y.Count)
                throw new ArgumentException("Les deux séries doivent avoir la même longueur.");

            int n = x.Count;

            if (n < 2)
                return null;

            double meanX = 0;
            double meanY = 0;

            for (int i = 0; i < n; i++)
            {
                meanX += x[i];
                meanY += y[i];
            }

            meanX /= n;
            meanY /= n;

            double covariance = 0;
            double varianceX = 0;

            for (int i = 0; i < n; i++)
            {
                covariance += (x[i] - meanX) * (y[i] - meanY);
                varianceX += (x[i] - meanX) * (x[i] - meanX);
            }

            return varianceX > 0 ? covariance / varianceX : null;
        }
    }
}

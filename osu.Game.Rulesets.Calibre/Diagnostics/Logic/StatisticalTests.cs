// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Résultat d'un test d'hypothèse : statistique, degrés de liberté et p-valeur bilatérale.
    /// </summary>
    public readonly record struct TestResult(double Statistic, double DegreesOfFreedom, double PValue);

    /// <summary>
    /// Tests de comparaison de deux séries d'écarts, sans dépendance externe.
    /// </summary>
    public static class StatisticalTests
    {
        /// <summary>
        /// Test t de Welch : les moyennes des deux séries diffèrent-elles ? Ne suppose pas des variances égales,
        /// ce qui est le cas général entre deux touches. Exige au moins deux valeurs par série.
        /// </summary>
        public static TestResult WelchTTest(OffsetStatistics first, OffsetStatistics second)
        {
            requireVariance(first);
            requireVariance(second);

            double a = first.Variance!.Value / first.Count;
            double b = second.Variance!.Value / second.Count;
            double meanDifference = second.Mean!.Value - first.Mean!.Value;

            // Variances toutes deux nulles : la différence est certaine dès que les moyennes diffèrent.
            if (a + b == 0)
                return new TestResult(meanDifference == 0 ? 0 : Math.Sign(meanDifference) * double.PositiveInfinity, first.Count + second.Count - 2, meanDifference == 0 ? 1 : 0);

            double t = meanDifference / Math.Sqrt(a + b);

            // Degrés de liberté de Welch-Satterthwaite.
            double df = (a + b) * (a + b) / (a * a / (first.Count - 1) + b * b / (second.Count - 1));

            return new TestResult(t, df, StudentTwoSidedPValue(t, df));
        }

        /// <summary>
        /// Test F bilatéral : les variances des deux séries diffèrent-elles ? La statistique est le rapport
        /// variance de la seconde / variance de la première.
        /// </summary>
        public static TestResult FTest(OffsetStatistics first, OffsetStatistics second)
        {
            requireVariance(first);
            requireVariance(second);

            double d1 = second.Count - 1;
            double d2 = first.Count - 1;

            if (first.Variance == 0 || second.Variance == 0)
                return new TestResult(double.NaN, d1, first.Variance == second.Variance ? 1 : 0);

            double f = second.Variance!.Value / first.Variance!.Value;

            return new TestResult(f, d1, FTwoSidedPValue(f, d1, d2));
        }

        /// <summary>
        /// p-valeur bilatérale de la loi de Student : P(|T| ≥ |t|) = I_{df / (df + t²)}(df / 2, 1 / 2).
        /// </summary>
        public static double StudentTwoSidedPValue(double t, double degreesOfFreedom)
        {
            if (double.IsInfinity(t))
                return 0;

            return RegularizedIncompleteBeta(degreesOfFreedom / (degreesOfFreedom + t * t), degreesOfFreedom / 2, 0.5);
        }

        /// <summary>
        /// p-valeur bilatérale de la loi de Fisher : deux fois la plus petite des deux queues,
        /// avec P(F ≤ f) = I_{d1·f / (d1·f + d2)}(d1 / 2, d2 / 2).
        /// </summary>
        public static double FTwoSidedPValue(double f, double d1, double d2)
        {
            double cdf = RegularizedIncompleteBeta(d1 * f / (d1 * f + d2), d1 / 2, d2 / 2);
            return Math.Min(1, 2 * Math.Min(cdf, 1 - cdf));
        }

        /// <summary>
        /// Fonction bêta incomplète régularisée I_x(a, b), évaluée par fraction continue (méthode de Lentz).
        /// Sur la moitié haute de l'intervalle, on utilise la symétrie I_x(a, b) = 1 - I_{1-x}(b, a), où la fraction converge vite.
        /// </summary>
        public static double RegularizedIncompleteBeta(double x, double a, double b)
        {
            if (x <= 0)
                return 0;
            if (x >= 1)
                return 1;

            double logFront = LogGamma(a + b) - LogGamma(a) - LogGamma(b) + a * Math.Log(x) + b * Math.Log(1 - x);
            double front = Math.Exp(logFront);

            if (x < (a + 1) / (a + b + 2))
                return front * betaContinuedFraction(x, a, b) / a;

            return 1 - front * betaContinuedFraction(1 - x, b, a) / b;
        }

        private static double betaContinuedFraction(double x, double a, double b)
        {
            const int max_iterations = 500;
            const double epsilon = 1e-15;

            double qab = a + b;
            double qap = a + 1;
            double qam = a - 1;

            double c = 1;
            double d = 1 / nonZero(1 - qab * x / qap);
            double h = d;

            for (int m = 1; m <= max_iterations; m++)
            {
                int m2 = 2 * m;

                // Terme pair de la fraction.
                double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1 / nonZero(1 + aa * d);
                c = nonZero(1 + aa / c);
                h *= d * c;

                // Terme impair.
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1 / nonZero(1 + aa * d);
                c = nonZero(1 + aa / c);

                double delta = d * c;
                h *= delta;

                if (Math.Abs(delta - 1) < epsilon)
                    break;
            }

            return h;
        }

        // Évite une division par zéro dans la méthode de Lentz.
        private static double nonZero(double value) => Math.Abs(value) < 1e-300 ? 1e-300 : value;

        private static readonly double[] lanczos_coefficients =
        {
            0.99999999999980993,
            676.5203681218851,
            -1259.1392167224028,
            771.32342877765313,
            -176.61502916214059,
            12.507343278686905,
            -0.13857109526572012,
            9.9843695780195716e-6,
            1.5056327351493116e-7,
        };

        /// <summary>
        /// ln Γ(x) par l'approximation de Lanczos (g = 7), précise à ~15 chiffres pour x > 0.
        /// </summary>
        public static double LogGamma(double x)
        {
            if (x < 0.5)
            {
                // Formule de réflexion : Γ(x) Γ(1 - x) = π / sin(πx).
                return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1 - x);
            }

            x -= 1;

            double sum = lanczos_coefficients[0];
            double t = x + 7.5;

            for (int i = 1; i < lanczos_coefficients.Length; i++)
                sum += lanczos_coefficients[i] / (x + i);

            return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(sum);
        }

        private static void requireVariance(OffsetStatistics statistics)
        {
            if (statistics.Count < 2)
                throw new ArgumentException("Il faut au moins deux valeurs par série pour comparer des variances.", nameof(statistics));
        }
    }
}

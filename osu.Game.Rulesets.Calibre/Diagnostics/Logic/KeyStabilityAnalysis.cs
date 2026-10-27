// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Stabilité d'une entrée sur le métronome : UR, biais et dérive au fil du test.
    /// </summary>
    public class KeyStats
    {
        public CalibreAction Action { get; }

        public IReadOnlyList<MatchedTap> Taps { get; }

        public OffsetStatistics Statistics { get; }

        /// <summary>
        /// Pente de l'écart en fonction du temps, en ms par minute. Positive = le joueur prend du retard au fil du test.
        /// </summary>
        public double? DriftPerMinute { get; }

        public KeyStats(CalibreAction action, IReadOnlyList<MatchedTap> taps)
        {
            Action = action;
            Taps = taps;
            Statistics = OffsetStatistics.From(taps.Select(t => t.Offset).ToArray());

            double? slope = LinearRegression.Slope(taps.Select(t => t.BeatTime).ToArray(), taps.Select(t => t.Offset).ToArray());
            DriftPerMinute = slope * 60000;
        }
    }

    /// <summary>
    /// Comparaison de deux entrées jumelles (K1 et K2, ou M1 et M2).
    /// </summary>
    public class KeyComparison
    {
        public KeyStats First { get; }

        public KeyStats Second { get; }

        /// <summary>
        /// Test de Welch sur les moyennes : un écart constant entre les deux touches.
        /// </summary>
        public TestResult MeanTest { get; }

        /// <summary>
        /// Test F sur les variances : une touche nettement moins régulière que l'autre.
        /// </summary>
        public TestResult VarianceTest { get; }

        /// <summary>
        /// Écart moyen de la seconde touche par rapport à la première, en ms.
        /// </summary>
        public double MeanDifference => Second.Statistics.Mean!.Value - First.Statistics.Mean!.Value;

        /// <summary>
        /// Rapport des UR (seconde / première).
        /// </summary>
        public double UnstableRateRatio => Second.Statistics.UnstableRate!.Value / First.Statistics.UnstableRate!.Value;

        /// <summary>
        /// Touche « traînante » : frappée significativement plus tard que sa jumelle, d'au moins
        /// <see cref="KeyStabilityResult.MINIMUM_MEAN_DIFFERENCE"/> ms. Nulle si la différence n'est pas significative.
        /// </summary>
        public CalibreAction? LaggingKey { get; }

        /// <summary>
        /// Touche significativement moins régulière que sa jumelle, d'au moins <see cref="KeyStabilityResult.MINIMUM_UR_RATIO"/> fois en UR.
        /// </summary>
        public CalibreAction? LessStableKey { get; }

        public KeyComparison(KeyStats first, KeyStats second)
        {
            First = first;
            Second = second;

            MeanTest = StatisticalTests.WelchTTest(first.Statistics, second.Statistics);
            VarianceTest = StatisticalTests.FTest(first.Statistics, second.Statistics);

            // Significativité ET taille d'effet : avec beaucoup de frappes, une différence d'une milliseconde
            // devient « significative » sans rien révéler d'utile sur le matériel.
            if (MeanTest.PValue < KeyStabilityResult.SIGNIFICANCE_LEVEL && Math.Abs(MeanDifference) >= KeyStabilityResult.MINIMUM_MEAN_DIFFERENCE)
                LaggingKey = MeanDifference > 0 ? second.Action : first.Action;

            double ratio = UnstableRateRatio;

            if (VarianceTest.PValue < KeyStabilityResult.SIGNIFICANCE_LEVEL && Math.Max(ratio, 1 / ratio) >= KeyStabilityResult.MINIMUM_UR_RATIO)
                LessStableKey = ratio > 1 ? second.Action : first.Action;
        }
    }

    public class KeyStabilityResult
    {
        public const double SIGNIFICANCE_LEVEL = 0.05;

        public const double MINIMUM_MEAN_DIFFERENCE = 3;

        public const double MINIMUM_UR_RATIO = 1.2;

        /// <summary>
        /// Frappes minimales par touche pour la comparer à sa jumelle.
        /// </summary>
        public const int MINIMUM_TAPS_PER_KEY = 10;

        public MetronomeSchedule Schedule { get; }

        public MatchedTaps Matched { get; }

        /// <summary>
        /// Une entrée par touche utilisée, dans l'ordre K1, K2, M1, M2.
        /// </summary>
        public IReadOnlyList<KeyStats> Keys { get; }

        public KeyComparison? Keyboard { get; }

        public KeyComparison? Mouse { get; }

        public KeyStabilityResult(MetronomeSchedule schedule, IReadOnlyList<TapSample> taps)
        {
            Schedule = schedule;
            Matched = MetronomeAnalysis.Match(schedule, taps);

            Keys = Enum.GetValues<CalibreAction>()
                       .Select(action => new KeyStats(action, Matched.Taps.Where(t => t.Action == action).ToArray()))
                       .Where(k => k.Statistics.Count > 0)
                       .ToArray();

            Keyboard = compare(CalibreAction.Key1, CalibreAction.Key2);
            Mouse = compare(CalibreAction.Mouse1, CalibreAction.Mouse2);
        }

        public KeyStats? For(CalibreAction action) => Keys.FirstOrDefault(k => k.Action == action);

        private KeyComparison? compare(CalibreAction first, CalibreAction second)
        {
            var a = For(first);
            var b = For(second);

            if (a == null || b == null || a.Statistics.Count < MINIMUM_TAPS_PER_KEY || b.Statistics.Count < MINIMUM_TAPS_PER_KEY)
                return null;

            return new KeyComparison(a, b);
        }
    }
}

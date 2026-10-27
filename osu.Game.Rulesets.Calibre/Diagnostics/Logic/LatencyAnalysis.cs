// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Résultat d'une phase du test de latence (métronome audio ou visuel).
    /// </summary>
    public class LatencyPhaseResult
    {
        public MetronomeSchedule Schedule { get; }

        public MatchedTaps Matched { get; }

        public OffsetStatistics Statistics { get; }

        public LatencyPhaseResult(MetronomeSchedule schedule, IReadOnlyList<TapSample> taps)
        {
            Schedule = schedule;
            Matched = MetronomeAnalysis.Match(schedule, taps);
            Statistics = OffsetStatistics.From(Matched.Taps.Select(t => t.Offset).ToArray());
        }

        /// <summary>
        /// Vrai si assez de temps ont été frappés pour que la moyenne ait un sens.
        /// </summary>
        public bool IsReliable => Statistics.Count >= LatencyResult.MINIMUM_TAPS;
    }

    /// <summary>
    /// Latence perçue : écart moyen des frappes à un métronome audio, puis à un métronome visuel seul.
    /// </summary>
    /// <remarks>
    /// L'écart mesuré additionne la latence du périphérique de sortie (son ou affichage), celle de l'entrée
    /// et l'anticipation propre au joueur. C'est précisément ce que l'offset de lazer doit compenser.
    /// </remarks>
    public class LatencyResult
    {
        /// <summary>
        /// En dessous, la moyenne est trop instable pour recommander un offset.
        /// </summary>
        public const int MINIMUM_TAPS = 8;

        public LatencyPhaseResult Audio { get; }

        public LatencyPhaseResult Visual { get; }

        public LatencyResult(LatencyPhaseResult audio, LatencyPhaseResult visual)
        {
            Audio = audio;
            Visual = visual;
        }

        /// <summary>
        /// Offset audio à saisir dans lazer : l'opposé de l'écart moyen, arrondi à la milliseconde.
        /// Un joueur en retard de 20 ms en moyenne doit régler -20 ms, comme le suggère lazer après une partie.
        /// </summary>
        public double? RecommendedAudioOffset => recommendedOffset(Audio);

        /// <summary>
        /// Équivalent pour un joueur qui se fie à l'image plutôt qu'au son (jeu sans hitsounds).
        /// </summary>
        public double? RecommendedVisualOffset => recommendedOffset(Visual);

        /// <summary>
        /// Retard de la réponse à l'image par rapport à la réponse au son. Positif = l'image est perçue plus tard.
        /// </summary>
        public double? AudioVisualGap => Audio.IsReliable && Visual.IsReliable ? Visual.Statistics.Mean - Audio.Statistics.Mean : null;

        private static double? recommendedOffset(LatencyPhaseResult phase)
        {
            if (!phase.IsReliable)
                return null;

            double offset = -Math.Round(phase.Statistics.Mean!.Value, MidpointRounding.AwayFromZero);

            // Évite d'afficher « -0 ms ».
            return offset == 0 ? 0 : offset;
        }
    }
}

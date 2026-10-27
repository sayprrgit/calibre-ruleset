// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Test de latence perçue en deux phases : métronome audio, puis métronome visuel seul (son coupé).
    /// </summary>
    public partial class LatencyTestRunner : MetronomeTestRunner
    {
        /// <summary>
        /// 100 BPM : assez lent pour frapper chaque temps sans effort, avec 4 temps d'annonce et 24 temps mesurés (~20 s).
        /// </summary>
        public static MetronomeSchedule DefaultSchedule => new MetronomeSchedule(600, 4, 24);

        public event Action<LatencyResult>? Completed;

        private readonly MetronomeSchedule audioSchedule;
        private readonly MetronomeSchedule visualSchedule;

        private LatencyPhaseResult? audio;

        public LatencyTestRunner(MetronomeSchedule? audioSchedule = null, MetronomeSchedule? visualSchedule = null)
            : base("Latence perçue")
        {
            this.audioSchedule = audioSchedule ?? DefaultSchedule;
            this.visualSchedule = visualSchedule ?? DefaultSchedule;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            showAudioPhase();
        }

        private void showAudioPhase()
        {
            Instructions.Text = $"Phase 1 sur 2 : métronome audio. Après {audioSchedule.LeadInBeats} clics aigus d'annonce, "
                                + "frappe (K1, K2, M1 ou M2) en même temps que chaque clic. Ne regarde pas l'écran : seul le son compte.";

            PrepareStart("Démarrer la phase audio", () => RunPhase(audioSchedule, true, false, taps =>
            {
                audio = new LatencyPhaseResult(audioSchedule, taps);
                showVisualPhase();
            }));
        }

        private void showVisualPhase()
        {
            var audioResult = audio!;

            Status.Text = $"Phase audio terminée : {audioResult.Statistics.Count} temps frappés.";

            Instructions.Text = $"Phase 2 sur 2 : métronome visuel, son coupé. Après {visualSchedule.LeadInBeats} éclairs gris d'annonce, "
                                + "frappe au moment où le disque s'allume.";

            PrepareStart("Démarrer la phase visuelle", () => RunPhase(visualSchedule, false, true, taps =>
                Completed?.Invoke(new LatencyResult(audioResult, new LatencyPhaseResult(visualSchedule, taps)))));
        }
    }
}

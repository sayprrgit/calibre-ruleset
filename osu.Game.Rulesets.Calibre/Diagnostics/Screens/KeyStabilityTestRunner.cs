// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Test de stabilité par touche : frappe en alternance sur un métronome audio rapide, comme dans un stream.
    /// </summary>
    public partial class KeyStabilityTestRunner : MetronomeTestRunner
    {
        /// <summary>
        /// Un temps toutes les 300 ms (doubles croches à 100 BPM) : 8 temps d'annonce et 64 mesurés, soit 32 frappes par touche.
        /// </summary>
        public static MetronomeSchedule DefaultSchedule => new MetronomeSchedule(300, 8, 64);

        public event Action<KeyStabilityResult>? Completed;

        private readonly MetronomeSchedule schedule;

        public KeyStabilityTestRunner(MetronomeSchedule? schedule = null)
            : base("Stabilité par touche")
        {
            this.schedule = schedule ?? DefaultSchedule;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Instructions.Text = $"Après {schedule.LeadInBeats} clics aigus d'annonce, frappe chaque clic en alternant K1 et K2, comme dans un stream. "
                                + "Pour tester la souris, refais le test en alternant M1 et M2. "
                                + "Chaque touche est mesurée à part : UR, biais (avance ou retard) et dérive au fil du test.";

            PrepareStart("Démarrer", () => RunPhase(schedule, true, false, taps => Completed?.Invoke(new KeyStabilityResult(schedule, taps))));
        }
    }
}

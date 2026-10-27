// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Timing;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics
{
    /// <summary>
    /// Métronome de test : l'horloge est avancée à la main, ce qui rend la datation des frappes exacte et reproductible.
    /// </summary>
    public class ManualMetronomeSource : IMetronomeSource
    {
        public readonly ManualClock Clock = new ManualClock { IsRunning = true };

        /// <summary>
        /// Pour chaque démarrage, vrai si le métronome devait être audible.
        /// </summary>
        public readonly List<bool> Starts = new List<bool>();

        public bool IsPlaying { get; private set; }

        public MetronomeSchedule? Schedule { get; private set; }

        public IClock Start(MetronomeSchedule schedule, bool audible)
        {
            Schedule = schedule;
            Starts.Add(audible);
            Clock.CurrentTime = 0;
            IsPlaying = true;

            return Clock;
        }

        public void Stop() => IsPlaying = false;
    }
}

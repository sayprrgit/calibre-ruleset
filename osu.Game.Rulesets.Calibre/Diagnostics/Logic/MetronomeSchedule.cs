// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Calendrier d'un métronome : des temps d'annonce (non mesurés) puis des temps mesurés, à intervalle fixe.
    /// Les instants sont exprimés dans le temps de la piste du métronome, en millisecondes.
    /// </summary>
    public class MetronomeSchedule
    {
        /// <summary>
        /// Silence en fin de piste, pour que la fenêtre du dernier temps se ferme avant la fin de la lecture.
        /// </summary>
        public const double TAIL = 500;

        public double FirstBeatTime { get; }

        public double Interval { get; }

        public int LeadInBeats { get; }

        public int MeasuredBeats { get; }

        public int TotalBeats => LeadInBeats + MeasuredBeats;

        /// <summary>
        /// Une frappe est rattachée au temps le plus proche : chaque temps mesuré accepte une demi-période de part et d'autre.
        /// </summary>
        public double MeasureStartTime => BeatTime(LeadInBeats) - Interval / 2;

        public double EndTime => BeatTime(TotalBeats - 1) + Interval / 2;

        public double TrackLength => EndTime + TAIL;

        public MetronomeSchedule(double interval, int leadInBeats, int measuredBeats, double firstBeatTime = 1000)
        {
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "L'intervalle doit être positif.");
            if (leadInBeats < 0)
                throw new ArgumentOutOfRangeException(nameof(leadInBeats), leadInBeats, "Le nombre de temps d'annonce ne peut pas être négatif.");
            if (measuredBeats < 1)
                throw new ArgumentOutOfRangeException(nameof(measuredBeats), measuredBeats, "Il faut au moins un temps mesuré.");

            Interval = interval;
            LeadInBeats = leadInBeats;
            MeasuredBeats = measuredBeats;
            FirstBeatTime = firstBeatTime;
        }

        public double BeatTime(int index) => FirstBeatTime + index * Interval;

        public bool IsLeadIn(int index) => index < LeadInBeats;

        public double BeatsPerMinute => 60000 / Interval;
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Frappe rattachée à un temps mesuré du métronome. Écart négatif = en avance, positif = en retard.
    /// </summary>
    public readonly record struct MatchedTap(int BeatIndex, double BeatTime, double Offset, CalibreAction Action)
    {
        public double Time => BeatTime + Offset;
    }

    public class MatchedTaps
    {
        public IReadOnlyList<MatchedTap> Taps { get; }

        /// <summary>
        /// Frappes écartées : pendant l'annonce, hors de la mesure, ou en double sur un même temps.
        /// </summary>
        public int IgnoredTaps { get; }

        public MatchedTaps(IReadOnlyList<MatchedTap> taps, int ignoredTaps)
        {
            Taps = taps;
            IgnoredTaps = ignoredTaps;
        }
    }

    public static class MetronomeAnalysis
    {
        /// <summary>
        /// Rattache chaque frappe au temps le plus proche et ne garde, pour chaque temps mesuré, que la frappe la plus proche.
        /// </summary>
        /// <remarks>
        /// Une seule frappe par temps : une frappe parasite (rebond de touche, double frappe) ne doit pas compter
        /// comme un écart d'une demi-période.
        /// </remarks>
        public static MatchedTaps Match(MetronomeSchedule schedule, IReadOnlyList<TapSample> taps)
        {
            var best = new TapSample?[schedule.TotalBeats];
            int ignored = 0;

            foreach (var tap in taps)
            {
                int beat = (int)Math.Round((tap.Time - schedule.FirstBeatTime) / schedule.Interval, MidpointRounding.AwayFromZero);

                if (beat < schedule.LeadInBeats || beat >= schedule.TotalBeats)
                {
                    ignored++;
                    continue;
                }

                double beatTime = schedule.BeatTime(beat);

                if (best[beat] is TapSample current)
                {
                    ignored++;

                    if (Math.Abs(tap.Time - beatTime) >= Math.Abs(current.Time - beatTime))
                        continue;
                }

                best[beat] = tap;
            }

            var matched = new List<MatchedTap>(schedule.MeasuredBeats);

            for (int beat = schedule.LeadInBeats; beat < schedule.TotalBeats; beat++)
            {
                if (best[beat] is TapSample tap)
                {
                    double beatTime = schedule.BeatTime(beat);
                    matched.Add(new MatchedTap(beat, beatTime, tap.Time - beatTime, tap.Action));
                }
            }

            return new MatchedTaps(matched, ignored);
        }
    }
}

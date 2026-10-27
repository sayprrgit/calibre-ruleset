// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Frappe horodatée à sa réception, dans le temps de l'horloge de référence du test.
    /// </summary>
    public readonly record struct TapSample(double Time, CalibreAction Action);

    /// <summary>
    /// Enregistre les frappes pendant un test. La capacité est réservée à l'avance : enregistrer n'alloue rien.
    /// </summary>
    public class TapRecorder
    {
        private readonly TapSample[] taps;

        public int Count { get; private set; }

        public TapRecorder(int capacity)
        {
            taps = new TapSample[capacity];
        }

        /// <param name="action">Entrée frappée.</param>
        /// <param name="time">Instant de réception, dans le temps de l'horloge de référence.</param>
        /// <returns>Faux si la capacité est atteinte : la frappe est alors ignorée.</returns>
        public bool Record(CalibreAction action, double time)
        {
            if (Count == taps.Length)
                return false;

            taps[Count++] = new TapSample(time, action);
            return true;
        }

        public void Clear() => Count = 0;

        /// <summary>
        /// Copie des frappes enregistrées, pour l'analyse après le test.
        /// </summary>
        public IReadOnlyList<TapSample> ToList() => new ArraySegment<TapSample>(taps, 0, Count).ToArray();
    }
}

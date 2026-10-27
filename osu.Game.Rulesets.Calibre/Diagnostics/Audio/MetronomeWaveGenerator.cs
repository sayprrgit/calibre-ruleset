// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Audio
{
    /// <summary>
    /// Génère en mémoire la piste WAV d'un métronome (PCM 16 bits mono), sans aucun fichier audio externe.
    /// </summary>
    /// <remarks>
    /// Chaque clic est écrit à l'échantillon exact de son temps : la position de lecture de la piste sert d'horloge
    /// de référence, donc un clic sonne au moment où l'horloge atteint son temps (au délai de sortie près, que l'on mesure).
    /// </remarks>
    public static class MetronomeWaveGenerator
    {
        public const int SAMPLE_RATE = 44100;

        /// <summary>
        /// Fréquence des clics d'annonce, plus aigus pour être distingués des temps mesurés.
        /// </summary>
        public const double LEAD_IN_FREQUENCY = 1760;

        public const double BEAT_FREQUENCY = 1320;

        private const double click_duration = 30;

        /// <summary>
        /// Constante de temps de la décroissance du clic, en ms : attaque franche, extinction rapide.
        /// </summary>
        private const double decay_time_constant = 6;

        private const double amplitude = 0.6;

        public static byte[] Generate(MetronomeSchedule schedule)
        {
            int sampleCount = (int)Math.Ceiling(schedule.TrackLength / 1000 * SAMPLE_RATE);
            short[] samples = new short[sampleCount];

            int clickLength = (int)(click_duration / 1000 * SAMPLE_RATE);

            for (int beat = 0; beat < schedule.TotalBeats; beat++)
            {
                int start = SampleIndexAt(schedule.BeatTime(beat));
                double frequency = schedule.IsLeadIn(beat) ? LEAD_IN_FREQUENCY : BEAT_FREQUENCY;

                for (int i = 0; i < clickLength && start + i < sampleCount; i++)
                {
                    double t = (double)i / SAMPLE_RATE;
                    double envelope = Math.Exp(-t * 1000 / decay_time_constant);
                    samples[start + i] = (short)(short.MaxValue * amplitude * envelope * Math.Sin(2 * Math.PI * frequency * t));
                }
            }

            return encode(samples);
        }

        public static int SampleIndexAt(double time) => (int)Math.Round(time / 1000 * SAMPLE_RATE);

        private static byte[] encode(short[] samples)
        {
            const short channels = 1;
            const short bits_per_sample = 16;
            const short block_align = channels * bits_per_sample / 8;

            int dataSize = samples.Length * block_align;

            using var stream = new MemoryStream(44 + dataSize);
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write(channels);
                writer.Write(SAMPLE_RATE);
                writer.Write(SAMPLE_RATE * block_align);
                writer.Write(block_align);
                writer.Write(bits_per_sample);

                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                foreach (short sample in samples)
                    writer.Write(sample);
            }

            return stream.ToArray();
        }
    }
}

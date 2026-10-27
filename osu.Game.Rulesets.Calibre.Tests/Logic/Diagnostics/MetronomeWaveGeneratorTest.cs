// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Diagnostics
{
    [TestFixture]
    public class MetronomeWaveGeneratorTest
    {
        private const int header_size = 44;

        // Temps à 100 ms (annonce), 600 ms et 1100 ms : fin de la mesure à 1350 ms, piste de 1850 ms.
        private readonly MetronomeSchedule schedule = new MetronomeSchedule(500, 1, 2, 100);

        private byte[] wave = null!;

        [SetUp]
        public void SetUp()
        {
            wave = MetronomeWaveGenerator.Generate(schedule);
        }

        [Test]
        public void TestHeaderDescribesMonoPcm()
        {
            Assert.That(Encoding.ASCII.GetString(wave, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(Encoding.ASCII.GetString(wave, 8, 4), Is.EqualTo("WAVE"));
            Assert.That(BitConverter.ToInt16(wave, 20), Is.EqualTo(1), "PCM");
            Assert.That(BitConverter.ToInt16(wave, 22), Is.EqualTo(1), "mono");
            Assert.That(BitConverter.ToInt32(wave, 24), Is.EqualTo(MetronomeWaveGenerator.SAMPLE_RATE));
            Assert.That(BitConverter.ToInt16(wave, 34), Is.EqualTo(16), "bits par échantillon");
            Assert.That(BitConverter.ToInt32(wave, 4), Is.EqualTo(wave.Length - 8));
            Assert.That(BitConverter.ToInt32(wave, 40), Is.EqualTo(wave.Length - header_size));
        }

        [Test]
        public void TestLengthCoversTheWholeSchedule()
        {
            int samples = (wave.Length - header_size) / 2;

            Assert.That(samples, Is.EqualTo((int)Math.Ceiling(schedule.TrackLength / 1000 * MetronomeWaveGenerator.SAMPLE_RATE)));
        }

        [Test]
        public void TestClicksStartExactlyOnTheBeats()
        {
            for (int beat = 0; beat < schedule.TotalBeats; beat++)
            {
                int start = MetronomeWaveGenerator.SampleIndexAt(schedule.BeatTime(beat));

                Assert.That(sampleAt(start - 1), Is.Zero, $"silence juste avant le temps {beat}");
                Assert.That(peakAround(start, 20), Is.GreaterThan(5000), $"clic au temps {beat}");
            }

            // Entre deux temps, le clic s'est éteint.
            Assert.That(peakAround(MetronomeWaveGenerator.SampleIndexAt(350), 100), Is.Zero);
        }

        private short sampleAt(int index) => BitConverter.ToInt16(wave, header_size + index * 2);

        private int peakAround(int start, int length)
        {
            int peak = 0;

            for (int i = start; i < start + length; i++)
                peak = Math.Max(peak, Math.Abs((int)sampleAt(i)));

            return peak;
        }
    }
}

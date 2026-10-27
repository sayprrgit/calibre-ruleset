// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Diagnostics
{
    [TestFixture]
    public class MetronomeTestsAnalysisTest
    {
        [Test]
        public void TestTapsAreMatchedToTheNearestMeasuredBeat()
        {
            // Temps à 1000 et 1500 (annonce), puis 2000, 2500, 3000 et 3500 (mesurés).
            var schedule = new MetronomeSchedule(500, 2, 4);

            var matched = MetronomeAnalysis.Match(schedule, new[]
            {
                new TapSample(1010, CalibreAction.Key1), // annonce : ignorée
                new TapSample(1990, CalibreAction.Key1),
                new TapSample(2520, CalibreAction.Key2),
                new TapSample(2560, CalibreAction.Key2), // second tap sur le même temps, plus loin : ignoré
                new TapSample(3000, CalibreAction.Key1),
                new TapSample(3240, CalibreAction.Key2), // rattaché à 3000, déjà mieux frappé : ignoré
                new TapSample(4100, CalibreAction.Key1), // après la mesure : ignorée
            });

            Assert.That(matched.Taps.Select(t => t.BeatIndex), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(matched.Taps.Select(t => t.Offset), Is.EqualTo(new[] { -10d, 20, 0 }));
            Assert.That(matched.Taps.Select(t => t.Action), Is.EqualTo(new[] { CalibreAction.Key1, CalibreAction.Key2, CalibreAction.Key1 }));
            Assert.That(matched.IgnoredTaps, Is.EqualTo(4));
        }

        [Test]
        public void TestCloserDuplicateReplacesTheFirstTap()
        {
            var schedule = new MetronomeSchedule(500, 0, 1);

            var matched = MetronomeAnalysis.Match(schedule, new[] { new TapSample(1040, CalibreAction.Key1), new TapSample(1005, CalibreAction.Key2) });

            Assert.That(matched.Taps.Single().Offset, Is.EqualTo(5));
            Assert.That(matched.Taps.Single().Action, Is.EqualTo(CalibreAction.Key2));
            Assert.That(matched.IgnoredTaps, Is.EqualTo(1));
        }

        [Test]
        public void TestRecommendedOffsetsAreTheOppositeOfTheMeanError()
        {
            var schedule = new MetronomeSchedule(600, 4, 12);

            var result = new LatencyResult(
                new LatencyPhaseResult(schedule, tapsAt(schedule, 23.4)),
                new LatencyPhaseResult(schedule, tapsAt(schedule, 41.6)));

            Assert.That(result.RecommendedAudioOffset, Is.EqualTo(-23));
            Assert.That(result.RecommendedVisualOffset, Is.EqualTo(-42));
            Assert.That(result.AudioVisualGap, Is.EqualTo(18.2).Within(1e-9));
        }

        [Test]
        public void TestNoRecommendationWithTooFewTaps()
        {
            var schedule = new MetronomeSchedule(600, 4, 12);
            var tooFew = tapsAt(schedule, 10).Take(LatencyResult.MINIMUM_TAPS - 1).ToArray();

            var result = new LatencyResult(new LatencyPhaseResult(schedule, tooFew), new LatencyPhaseResult(schedule, tapsAt(schedule, 10)));

            Assert.That(result.RecommendedAudioOffset, Is.Null);
            Assert.That(result.RecommendedVisualOffset, Is.EqualTo(-10));
            Assert.That(result.AudioVisualGap, Is.Null);
        }

        [Test]
        public void TestLaggingKeyIsDetected()
        {
            var schedule = new MetronomeSchedule(300, 4, 48);

            // K1 autour de 0 ms, K2 autour de +20 ms, avec la même dispersion.
            var result = new KeyStabilityResult(schedule, alternate(schedule, new[] { -6d, -2, 2, 6 }, new[] { 14d, 18, 22, 26 }));

            Assert.That(result.Keyboard, Is.Not.Null);
            Assert.That(result.Keyboard!.LaggingKey, Is.EqualTo(CalibreAction.Key2));
            Assert.That(result.Keyboard.MeanDifference, Is.EqualTo(20).Within(1e-9));
            Assert.That(result.Keyboard.MeanTest.PValue, Is.LessThan(1e-6));
            Assert.That(result.Keyboard.LessStableKey, Is.Null);
            Assert.That(result.Mouse, Is.Null, "aucune frappe à la souris");
        }

        [Test]
        public void TestLessStableKeyIsDetected()
        {
            var schedule = new MetronomeSchedule(300, 4, 48);

            var result = new KeyStabilityResult(schedule, alternate(schedule, new[] { -2d, 2 }, new[] { -10d, 10 }));

            Assert.That(result.Keyboard!.LaggingKey, Is.Null);
            Assert.That(result.Keyboard.LessStableKey, Is.EqualTo(CalibreAction.Key2));
            Assert.That(result.For(CalibreAction.Key2)!.Statistics.UnstableRate, Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void TestSmallDifferenceIsNotFlagged()
        {
            var schedule = new MetronomeSchedule(300, 4, 48);

            // 1 ms d'écart constant, sans dispersion : statistiquement certain, mais sous le seuil de 3 ms.
            var result = new KeyStabilityResult(schedule, alternate(schedule, new[] { 0d }, new[] { 1d }));

            Assert.That(result.Keyboard!.LaggingKey, Is.Null);
        }

        [Test]
        public void TestDriftFollowsTheOffsetOverTime()
        {
            var schedule = new MetronomeSchedule(300, 0, 40);

            // L'écart augmente de 1 ms par temps de 300 ms : 200 ms par minute.
            var taps = Enumerable.Range(0, 40).Select(i => new TapSample(schedule.BeatTime(i) + i, CalibreAction.Key1)).ToArray();

            var result = new KeyStabilityResult(schedule, taps);

            Assert.That(result.For(CalibreAction.Key1)!.DriftPerMinute, Is.EqualTo(200).Within(1e-6));
            Assert.That(result.Keyboard, Is.Null, "une seule touche");
        }

        private static TapSample[] tapsAt(MetronomeSchedule schedule, double offset)
            => Enumerable.Range(schedule.LeadInBeats, schedule.MeasuredBeats)
                         .Select(i => new TapSample(schedule.BeatTime(i) + offset, CalibreAction.Key1))
                         .ToArray();

        /// <summary>
        /// Alterne K1 et K2 sur chaque temps mesuré, avec des écarts pris en boucle dans chaque liste.
        /// </summary>
        private static TapSample[] alternate(MetronomeSchedule schedule, IReadOnlyList<double> key1Offsets, IReadOnlyList<double> key2Offsets)
        {
            var taps = new List<TapSample>();

            for (int i = 0; i < schedule.MeasuredBeats; i++)
            {
                int beat = schedule.LeadInBeats + i;
                bool first = i % 2 == 0;
                var offsets = first ? key1Offsets : key2Offsets;

                taps.Add(new TapSample(schedule.BeatTime(beat) + offsets[i / 2 % offsets.Count], first ? CalibreAction.Key1 : CalibreAction.Key2));
            }

            return taps.ToArray();
        }
    }
}

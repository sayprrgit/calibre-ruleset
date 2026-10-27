// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Analysis
{
    [TestFixture]
    public class AnalysisTest
    {
        [Test]
        public void TestHitEventsAreMatchedToTheRecreatedBeatmap()
        {
            // L'écran de résultats recrée la beatmap : les événements pointent vers les objets d'une autre instance.
            var played = TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS).HitObjects.Cast<CalibreHitObject>().ToArray();
            var recreated = TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS);

            var slider = (Slider)played[1];
            var events = new[]
            {
                new HitEvent(12, 1.5, HitResult.Great, played[0], null, new Vector2(103, 104)),
                new HitEvent(-6, 1.5, HitResult.Ok, slider.HeadCircle!, played[0], new Vector2(200, 150)),
                new HitEvent(0, 1.5, HitResult.Great, played[2], slider, null), // spinner : pas de timing
                new HitEvent(150, 1.5, HitResult.Miss, played[3], played[2], null),
            };

            var hits = TimedHits.From(recreated, events);
            var recreatedSlider = recreated.HitObjects.OfType<Slider>().Single();

            Assert.That(hits.Select(h => h.Result), Is.EqualTo(new[] { HitResult.Great, HitResult.Ok, HitResult.Miss }));
            Assert.That(hits.Select(h => h.IsSlider), Is.EqualTo(new[] { false, true, false }));

            // Temps et écarts ramenés en millisecondes réelles (vitesse 1,5).
            Assert.That(hits[0].Time, Is.EqualTo(1000 / 1.5).Within(1e-9));
            Assert.That(hits[0].Offset, Is.EqualTo(8).Within(1e-9));
            Assert.That(hits[1].EndTime, Is.EqualTo(recreatedSlider.EndTime / 1.5).Within(1e-9));
            Assert.That(hits[1].EndPosition, Is.EqualTo(recreatedSlider.EndPosition));
            Assert.That(hits[0].PositionError, Is.EqualTo(5).Within(1e-4));
            Assert.That(hits[2].PositionError, Is.Null);
        }

        [Test]
        public void TestUnstablePassageIsDetected()
        {
            // 64 frappes à ±3 ms, sauf les frappes 24 à 39 à ±30 ms.
            var hits = Enumerable.Range(0, 64)
                                 .Select(i => PatternClassifierTest.hit(i * 100, 100, 100, offset: (i % 2 == 0 ? 1 : -1) * (i >= 24 && i < 40 ? 30 : 3)))
                                 .ToArray();

            var sections = UnstableSectionDetector.Detect(hits);

            Assert.That(sections, Has.Count.EqualTo(1));
            Assert.That(sections[0].StartTime, Is.LessThanOrEqualTo(2400));
            Assert.That(sections[0].EndTime, Is.GreaterThanOrEqualTo(3900));
            Assert.That(sections[0].UnstableRate, Is.GreaterThan(100));
        }

        [TestCase(64, 3)]
        [TestCase(64, 0)]
        [TestCase(20, 30)]
        public void TestNoSectionWithoutAMarkedIncrease(int count, double spread)
        {
            // Partie uniforme, parfaite, ou trop courte pour comparer deux fenêtres.
            var hits = Enumerable.Range(0, count).Select(i => PatternClassifierTest.hit(i * 100, 100, 100, offset: (i % 2 == 0 ? 1 : -1) * spread)).ToArray();

            Assert.That(UnstableSectionDetector.Detect(hits), Is.Empty);
        }

        [Test]
        public void TestHeatmapCellsAndBias()
        {
            var target = new Vector2(100, 100);
            var hits = new[]
            {
                new TimedHit(0, 0, HitResult.Great, target, target + new Vector2(9, 12), 30, 0, target, false),
                new TimedHit(100, 0, HitResult.Miss, target, null, 30, 100, target, false),
            };

            var heatmap = PositionHeatmap.From(hits);

            // La cible (100, 100) tombe dans la case (3, 3) d'une grille 16x12 de 512x384.
            Assert.That(heatmap.Count, Is.EqualTo(1), "le miss sans frappe est ignoré");
            Assert.That(heatmap.Counts[3, 3], Is.EqualTo(1));
            Assert.That(heatmap.MeanErrorAt(3, 3), Is.EqualTo(0.5).Within(1e-6), "15 px pour un rayon de 30");
            Assert.That(heatmap.MeanErrorAt(0, 0), Is.Null);
            Assert.That(heatmap.MeanOffset, Is.EqualTo(new Vector2(9, 12)));
            Assert.That(heatmap.MeanError, Is.EqualTo(15).Within(1e-6));
        }
    }
}

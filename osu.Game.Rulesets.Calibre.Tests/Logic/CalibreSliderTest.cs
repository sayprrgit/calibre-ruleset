// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Scoring;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class CalibreSliderTest
    {
        private Slider slider = null!;

        [SetUp]
        public void SetUp()
        {
            slider = TestBeatmaps.DecodePlayable(TestBeatmaps.SLIDER_WITH_TICKS_AND_REPEATS).HitObjects.OfType<Slider>().Single();
        }

        [Test]
        public void TestNestedObjectsFollowTheSpans()
        {
            var nested = slider.NestedHitObjects.Cast<CalibreHitObject>().OrderBy(h => h.StartTime).ToList();

            Assert.That(nested.Select(h => h.GetType()), Is.EqualTo(new[]
            {
                typeof(SliderHeadCircle),
                typeof(SliderTick), typeof(SliderRepeat),
                typeof(SliderTick), typeof(SliderRepeat),
                typeof(SliderTick), typeof(SliderTailCircle),
            }));

            Assert.That(nested.Select(h => h.StartTime), Is.EqualTo(new[] { 1000d, 1500, 2000, 2500, 3000, 3500, 4000 }).Within(1e-6));

            // Aller vers x = 380, retour vers x = 100, puis de nouveau vers x = 380.
            Assert.That(nested.Select(h => h.X), Is.EqualTo(new[] { 100f, 240, 380, 240, 100, 240, 380 }).Within(1e-3));
        }

        [Test]
        public void TestHeadAndTailMatchTheSlider()
        {
            Assert.That(slider.HeadCircle, Is.Not.Null);
            Assert.That(slider.TailCircle, Is.Not.Null);
            Assert.That(slider.HeadCircle!.StartTime, Is.EqualTo(slider.StartTime));
            Assert.That(slider.TailCircle!.StartTime, Is.EqualTo(slider.EndTime).Within(1e-6));
            Assert.That(slider.TailCircle.Position, Is.EqualTo(slider.EndPosition));
        }

        [Test]
        public void TestOnlyTheHeadHasTimingWindows()
        {
            // Seule la tête entre dans l'UR : le reste est jugé au suivi.
            Assert.That(slider.HeadCircle!.HitWindows, Is.InstanceOf<CalibreHitWindows>());
            Assert.That(slider.HitWindows, Is.SameAs(HitWindows.Empty));
            Assert.That(slider.NestedHitObjects.OfType<SliderNestedObject>().Select(h => h.HitWindows), Has.All.SameAs(HitWindows.Empty));
        }

        [Test]
        public void TestNestedObjectsAppearWithTheSlider()
        {
            foreach (var nested in slider.NestedHitObjects.Cast<CalibreHitObject>())
                Assert.That(nested.StartTime - nested.TimePreempt, Is.EqualTo(slider.StartTime - slider.TimePreempt).Within(1e-6), nested.GetType().Name);
        }

        [Test]
        public void TestRepeatArrowsPointBackAlongThePath()
        {
            var repeats = slider.NestedHitObjects.OfType<SliderRepeat>().OrderBy(r => r.StartTime).ToList();

            // Première répétition au bout droit : la flèche pointe vers la gauche (180°). La seconde, au départ : vers la droite (0°).
            Assert.That(Math.Abs(repeats[0].ArrowRotation), Is.EqualTo(180).Within(0.5));
            Assert.That(repeats[1].ArrowRotation, Is.EqualTo(0).Within(0.5));
        }

        [Test]
        public void TestJudgementsOfEachElement()
        {
            Assert.That(slider.CreateJudgement().MaxResult, Is.EqualTo(HitResult.IgnoreHit));
            Assert.That(slider.HeadCircle!.CreateJudgement().MaxResult, Is.EqualTo(HitResult.Great));
            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().First().CreateJudgement().MaxResult, Is.EqualTo(HitResult.LargeTickHit));
            Assert.That(slider.NestedHitObjects.OfType<SliderRepeat>().First().CreateJudgement().MaxResult, Is.EqualTo(HitResult.LargeTickHit));

            var tailJudgement = slider.TailCircle!.CreateJudgement();

            // Règle de lazer : une fin de slider manquée coûte de la précision mais ne casse pas le combo.
            Assert.That(tailJudgement.MaxResult, Is.EqualTo(HitResult.SliderTailHit));
            Assert.That(tailJudgement.MinResult, Is.EqualTo(HitResult.IgnoreMiss));
        }

        [Test]
        public void TestSliderWithoutTicksStillHasHeadAndTail()
        {
            var shortSlider = TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS).HitObjects.OfType<Slider>().Single();

            Assert.That(shortSlider.NestedHitObjects.Select(h => h.GetType()), Is.EquivalentTo(new[] { typeof(SliderHeadCircle), typeof(SliderTailCircle) }));
            Assert.That(shortSlider.HeadCircle!.Position, Is.EqualTo(new Vector2(200, 150)));
        }
    }
}

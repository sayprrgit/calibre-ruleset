// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Analysis
{
    [TestFixture]
    public class PatternClassifierTest
    {
        [Test]
        public void TestFirstAndDistantObjectsAreIsolated()
        {
            assertTypes(new[] { hit(0, 100, 100), hit(1000, 300, 100) }, PatternType.Isolated, PatternType.Isolated);
        }

        [Test]
        public void TestCloseFastObjectsAreStreams()
        {
            // 100 ms d'écart et 30 px d'espacement (0,4 diamètre).
            assertTypes(new[] { hit(0, 100, 100), hit(100, 130, 100), hit(200, 160, 100) },
                PatternType.Isolated, PatternType.Stream, PatternType.Stream);
        }

        [Test]
        public void TestStraightLongJumps()
        {
            // 200 px avec un rayon de 30 : 3,3 diamètres, en ligne droite.
            assertTypes(new[] { hit(0, 50, 200, radius: 30), hit(250, 250, 200, radius: 30), hit(500, 450, 200, radius: 30) },
                PatternType.Isolated, PatternType.LongWideJump, PatternType.LongWideJump);
        }

        [Test]
        public void TestBackAndForthShortJumps()
        {
            // 120 px avec un rayon de 36 : 1,7 diamètre, puis demi-tour (angle de 0°).
            assertTypes(new[] { hit(0, 100, 200), hit(250, 220, 200), hit(500, 100, 200) },
                PatternType.Isolated, PatternType.ShortWideJump, PatternType.ShortSharpJump);
        }

        [Test]
        public void TestSliderHeadsAreSliders()
        {
            assertTypes(new[] { hit(0, 100, 100), hit(100, 110, 100) with { IsSlider = true } }, PatternType.Isolated, PatternType.Slider);
        }

        [Test]
        public void TestGapCountsFromTheEndOfASlider()
        {
            // Le slider commence à 0 et finit à 900 : le cercle à 1100 s'enchaîne à sa fin (200 ms), il n'est pas isolé.
            var slider = hit(0, 100, 100) with { IsSlider = true, EndTime = 900, EndPosition = new Vector2(300, 100) };

            assertTypes(new[] { slider, hit(1100, 300, 300) }, PatternType.Slider, PatternType.ShortWideJump);
        }

        [Test]
        public void TestSummaryAccuracyAndTiming()
        {
            var stream = new[]
            {
                hit(0, 100, 100),
                hit(100, 120, 100, HitResult.Great, 10),
                hit(200, 140, 100, HitResult.Ok, -20),
                hit(300, 160, 100, HitResult.Meh, 40),
                hit(400, 180, 100, HitResult.Miss),
            };

            var summary = PatternClassifier.Summarise(stream);

            Assert.That(summary.Select(s => s.Type), Is.EqualTo(new[] { PatternType.Stream, PatternType.Isolated }));

            var streams = summary[0];
            Assert.That(streams.Count, Is.EqualTo(4));
            Assert.That(streams.Accuracy, Is.EqualTo((1 + 1 / 3.0 + 1 / 6.0) / 4).Within(1e-12));
            Assert.That(streams.Timing.Count, Is.EqualTo(3), "le miss n'a pas d'écart de timing");
            Assert.That(streams.Timing.Mean, Is.EqualTo(10).Within(1e-12));
        }

        private static void assertTypes(TimedHit[] hits, params PatternType[] expected)
            => Assert.That(Enumerable.Range(0, hits.Length).Select(i => PatternClassifier.Classify(hits, i)), Is.EqualTo(expected));

        internal static TimedHit hit(double time, float x, float y, HitResult result = HitResult.Great, double offset = 0, float radius = 36)
        {
            var target = new Vector2(x, y);
            return new TimedHit(time, offset, result, target, result == HitResult.Miss ? null : target, radius, time, target, false);
        }
    }
}

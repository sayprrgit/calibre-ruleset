// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class CalibreHitWindowsTest
    {
        [TestCase(0, 79.5, 139.5, 199.5)]
        [TestCase(5, 49.5, 99.5, 149.5)]
        [TestCase(8, 31.5, 75.5, 119.5)]
        [TestCase(10, 19.5, 59.5, 99.5)]
        public void TestWindowsFromOverallDifficulty(double od, double great, double ok, double meh)
        {
            var windows = new CalibreHitWindows();
            windows.SetDifficulty(od);

            Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(great));
            Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(ok));
            Assert.That(windows.WindowFor(HitResult.Meh), Is.EqualTo(meh));
            Assert.That(windows.WindowFor(HitResult.Miss), Is.EqualTo(CalibreHitWindows.MISS_WINDOW));
        }

        [TestCase(0, HitResult.Great)]
        [TestCase(49, HitResult.Great)]
        [TestCase(-49, HitResult.Great)]
        [TestCase(50, HitResult.Ok)]
        [TestCase(-99, HitResult.Ok)]
        [TestCase(100, HitResult.Meh)]
        [TestCase(149, HitResult.Meh)]
        [TestCase(150, HitResult.Miss)]
        [TestCase(-399, HitResult.Miss)]
        [TestCase(-401, HitResult.None)]
        public void TestResultForOffsetAtOd5(double offset, HitResult expected)
        {
            var windows = new CalibreHitWindows();
            windows.SetDifficulty(5);

            Assert.That(windows.ResultFor(offset), Is.EqualTo(expected));
        }

        [Test]
        public void TestLateHitBeyondMehCannotBeHit()
        {
            var windows = new CalibreHitWindows();
            windows.SetDifficulty(5);

            // Après la fenêtre Meh, l'objet n'est plus frappable : il sera jugé Miss automatiquement.
            Assert.That(windows.CanBeHit(149), Is.True);
            Assert.That(windows.CanBeHit(150), Is.False);
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Objects.Drawables;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class SpinnerRotationTrackerTest
    {
        private const double step = Math.PI / 4;

        [Test]
        public void TestOneFullTurnIsOneSpin()
        {
            var tracker = new SpinnerRotationTracker();

            // 45° toutes les 20 ms, soit 6,25 tours/s : sous le plafond de 7,95 tours/s.
            for (int i = 0; i <= 8; i++)
                tracker.AddSample(normalise(i * step), 20, true);

            Assert.That(tracker.CompletedSpins, Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void TestNothingCountsWithoutAPressedInput()
        {
            var tracker = new SpinnerRotationTracker();

            for (int i = 0; i <= 8; i++)
                tracker.AddSample(normalise(i * step), 20, false);

            Assert.That(tracker.TotalRotation, Is.Zero);
        }

        [Test]
        public void TestCrossingMinusPiIsASmallStep()
        {
            var tracker = new SpinnerRotationTracker();

            tracker.AddSample(Math.PI - 0.05, 20, true);
            tracker.AddSample(-Math.PI + 0.05, 20, true);

            Assert.That(tracker.TotalRotation, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(tracker.SignedRotation, Is.EqualTo(0.1).Within(1e-9));
        }

        [Test]
        public void TestBackAndForthCountsBothWays()
        {
            var tracker = new SpinnerRotationTracker();

            tracker.AddSample(0, 20, true);
            tracker.AddSample(step, 20, true);
            tracker.AddSample(0, 20, true);

            Assert.That(tracker.TotalRotation, Is.EqualTo(2 * step).Within(1e-9));
            Assert.That(tracker.SignedRotation, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TestRotationSpeedIsCapped()
        {
            var tracker = new SpinnerRotationTracker();

            tracker.AddSample(0, 1, true);
            tracker.AddSample(step, 1, true);

            double maximumInOneMillisecond = SpinnerRotationTracker.MAX_ROTATIONS_PER_SECOND * 2 * Math.PI / 1000;
            Assert.That(tracker.TotalRotation, Is.EqualTo(maximumInOneMillisecond).Within(1e-9));
        }

        [Test]
        public void TestRewindAddsNothing()
        {
            var tracker = new SpinnerRotationTracker();

            tracker.AddSample(0, 20, true);
            tracker.AddSample(step, -20, true);
            tracker.AddSample(step, 0, true);

            Assert.That(tracker.TotalRotation, Is.Zero);
        }

        private static double normalise(double angle) => Math.Atan2(Math.Sin(angle), Math.Cos(angle));
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Diagnostics
{
    [TestFixture]
    public class AimGridTest
    {
        private const float radius = 36;

        [Test]
        public void TestZonesCoverThePlayfield()
        {
            Assert.That(AimGrid.ZoneBounds(0).TopLeft, Is.EqualTo(Vector2.Zero));
            Assert.That(AimGrid.ZoneBounds(8).BottomRight.X, Is.EqualTo(512).Within(1e-3));
            Assert.That(AimGrid.ZoneBounds(8).BottomRight.Y, Is.EqualTo(384).Within(1e-3));
            Assert.That(AimGrid.ZoneCentre(4), Is.EqualTo(new Vector2(256, 192)));
            Assert.That(AimGrid.ZoneName(6), Is.EqualTo("bas gauche"));
        }

        [Test]
        public void TestSequenceIsDeterministic()
        {
            var first = AimTargetSequence.Generate(42, 3, radius);
            var second = AimTargetSequence.Generate(42, 3, radius);
            var other = AimTargetSequence.Generate(43, 3, radius);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.EqualTo(other));
        }

        [Test]
        public void TestEachRoundVisitsEveryZoneOnce()
        {
            var targets = AimTargetSequence.Generate(7, 4, radius);

            Assert.That(targets, Has.Count.EqualTo(36));

            for (int round = 0; round < 4; round++)
                Assert.That(targets.Skip(round * 9).Take(9).Select(t => t.Zone), Is.EquivalentTo(Enumerable.Range(0, 9)), $"tour {round}");
        }

        [Test]
        public void TestTargetsStayInsideTheirZone()
        {
            foreach (var target in AimTargetSequence.Generate(1, 10, radius))
            {
                var bounds = AimGrid.ZoneBounds(target.Zone);

                // La cible entière (rayon compris) reste dans sa zone.
                Assert.That(target.Position.X - radius, Is.GreaterThanOrEqualTo(bounds.Left));
                Assert.That(target.Position.X + radius, Is.LessThanOrEqualTo(bounds.Right));
                Assert.That(target.Position.Y - radius, Is.GreaterThanOrEqualTo(bounds.Top));
                Assert.That(target.Position.Y + radius, Is.LessThanOrEqualTo(bounds.Bottom));
                Assert.That(target.Delay, Is.InRange(AimTargetSequence.MINIMUM_DELAY, AimTargetSequence.MAXIMUM_DELAY));
            }
        }

        [Test]
        public void TestZoneStatistics()
        {
            var result = new AimGridResult(1, radius, new[]
            {
                new AimAttempt(0, new Vector2(100, 100), new Vector2(103, 104), 300, true),
                new AimAttempt(0, new Vector2(100, 100), new Vector2(100, 140), 500, false),
                new AimAttempt(0, new Vector2(100, 100), null, null, false),
            });

            var zone = result.Zones[0];

            Assert.That(zone.Attempts, Is.EqualTo(3));
            Assert.That(zone.Hits, Is.EqualTo(1));
            Assert.That(zone.MissRate, Is.EqualTo(2.0 / 3).Within(1e-12));

            // Distances 5 et 40 px ; la cible expirée n'entre ni dans la distance ni dans le temps de réaction.
            Assert.That(zone.MeanDistance, Is.EqualTo(22.5).Within(1e-6));
            Assert.That(zone.MeanReactionTime, Is.EqualTo(400).Within(1e-9));

            Assert.That(result.Zones[1].Attempts, Is.Zero);
            Assert.That(result.WorstZone, Is.EqualTo(0));
            Assert.That(result.WeakZone, Is.Null, "une seule zone testée");
        }

        [Test]
        public void TestWeakZoneIsReported()
        {
            var attempts = new List<AimAttempt>();

            for (int zone = 0; zone < 9; zone++)
            {
                var centre = AimGrid.ZoneCentre(zone);

                for (int i = 0; i < 4; i++)
                {
                    // Bas droite : frappes à 45 px (hors cible). Ailleurs : frappes à 6 px.
                    var press = centre + new Vector2(zone == 8 ? 45 : 6, 0);
                    attempts.Add(new AimAttempt(zone, centre, press, 350, zone != 8));
                }
            }

            var result = new AimGridResult(1, radius, attempts);

            Assert.That(result.WorstZone, Is.EqualTo(8));
            Assert.That(result.WeakZone, Is.EqualTo(8));
            Assert.That(result.HitRate, Is.EqualTo(32.0 / 36).Within(1e-12));
        }

        [Test]
        public void TestNoWeakZoneWhenZonesAreEven()
        {
            var attempts = Enumerable.Range(0, 9)
                                     .Select(zone => new AimAttempt(zone, AimGrid.ZoneCentre(zone), AimGrid.ZoneCentre(zone) + new Vector2(10 + zone % 2, 0), 350, true))
                                     .ToArray();

            var result = new AimGridResult(1, radius, attempts);

            Assert.That(result.WorstZone, Is.Not.Null);
            Assert.That(result.WeakZone, Is.Null);
        }
    }
}

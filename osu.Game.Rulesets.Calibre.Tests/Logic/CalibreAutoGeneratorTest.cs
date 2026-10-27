// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Replays;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class CalibreAutoGeneratorTest
    {
        [Test]
        public void TestFramesAreStrictlyOrdered()
        {
            var frames = generate(0, 100, 150, 1000, 1000).Frames;

            for (int i = 1; i < frames.Count; i++)
                Assert.That(frames[i].Time, Is.GreaterThan(frames[i - 1].Time), $"frame {i}");
        }

        [Test]
        public void TestPressesExactlyOnEachCircle()
        {
            var beatmap = createBeatmap(0, 500, 1000);
            var presses = new CalibreAutoGenerator(beatmap).Generate().Frames.Cast<CalibreReplayFrame>().Where(f => f.Actions.Count > 0).ToList();

            Assert.That(presses.Select(f => f.Time), Is.EqualTo(new[] { 0d, 500d, 1000d }));
            Assert.That(presses.Select(f => f.Position), Is.EqualTo(beatmap.HitObjects.Select(h => h.Position)));
        }

        [Test]
        public void TestAlternatesKeysOnStreams()
        {
            var presses = generate(0, 100, 200, 300, 1000).Frames.Cast<CalibreReplayFrame>().Where(f => f.Actions.Count > 0).ToList();

            Assert.That(presses.Select(f => f.Actions.Single()), Is.EqualTo(new[]
            {
                CalibreAction.Key1, CalibreAction.Key2, CalibreAction.Key1, CalibreAction.Key2,
                // Intervalle de 700 ms : on repart sur K1.
                CalibreAction.Key1,
            }));
        }

        [Test]
        public void TestKeyIsReleasedBetweenCloseCircles()
        {
            var frames = generate(0, 60).Frames.Cast<CalibreReplayFrame>().ToList();
            var firstPress = frames.First(f => f.Actions.Count > 0);
            var release = frames[frames.IndexOf(firstPress) + 1];

            Assert.That(release.Actions, Is.Empty);
            Assert.That(release.Time, Is.LessThan(60));
        }

        [Test]
        public void TestSliderIsFollowedWithTheKeyHeld()
        {
            var beatmap = TestBeatmaps.DecodePlayable(TestBeatmaps.SLIDER_WITH_TICKS_AND_REPEATS);
            var slider = (Slider)beatmap.HitObjects.Single();

            var frames = new CalibreAutoGenerator(beatmap).Generate().Frames.Cast<CalibreReplayFrame>().ToList();
            var held = frames.Where(f => f.Time >= slider.StartTime && f.Time <= slider.EndTime).ToList();

            Assert.That(held, Has.All.Matches<CalibreReplayFrame>(f => f.Actions.Count == 1), "touche maintenue sur tout le slider");

            for (int i = 1; i < held.Count; i++)
                Assert.That(held[i].Time - held[i - 1].Time, Is.LessThanOrEqualTo(CalibreAutoGenerator.FRAME_INTERVAL + 1e-6), $"frame {i}");

            // Chaque frame est sur le chemin : à l'instant d'un tick, le curseur est sur le tick.
            foreach (var tick in slider.NestedHitObjects.OfType<SliderTick>())
            {
                var closest = held.OrderBy(f => Math.Abs(f.Time - tick.StartTime)).First();
                Assert.That(Vector2.Distance(closest.Position, tick.Position), Is.LessThan(slider.Radius), $"tick à {tick.StartTime}");
            }

            Assert.That(held.Last().Time, Is.EqualTo(slider.EndTime).Within(1e-6));
            Assert.That(held.Last().Position, Is.EqualTo(slider.EndPosition));
            Assert.That(frames[frames.IndexOf(held.Last()) + 1].Actions, Is.Empty, "relâchement après la fin");
        }

        [Test]
        public void TestFramesAreStrictlyOrderedWithEveryObjectType()
        {
            var frames = new CalibreAutoGenerator(TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS)).Generate().Frames;

            for (int i = 1; i < frames.Count; i++)
                Assert.That(frames[i].Time, Is.GreaterThan(frames[i - 1].Time), $"frame {i}");
        }

        private static osu.Game.Replays.Replay generate(params double[] times) => new CalibreAutoGenerator(createBeatmap(times)).Generate();

        private static Beatmap<CalibreHitObject> createBeatmap(params double[] times)
        {
            var beatmap = new Beatmap<CalibreHitObject>();

            for (int i = 0; i < times.Length; i++)
                beatmap.HitObjects.Add(new HitCircle { StartTime = times[i], Position = new Vector2(50 + 40 * i, 100) });

            return beatmap;
        }
    }
}

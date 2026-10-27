// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Training
{
    [TestFixture]
    public class ExerciseGeneratorTest
    {
        private static readonly ExerciseSettings settings = new ExerciseSettings(180, 60, 1);

        private static readonly PatternTarget[] mixed =
        {
            new PatternTarget(PatternType.Stream, 1),
            new PatternTarget(PatternType.LongWideJump, 1),
            new PatternTarget(PatternType.Slider, 1),
        };

        [Test]
        public void TestSameSeedSameExercise()
        {
            var first = ExerciseGenerator.Generate(settings, mixed, 7).HitObjects;
            var second = ExerciseGenerator.Generate(settings, mixed, 7).HitObjects;
            var other = ExerciseGenerator.Generate(settings, mixed, 8).HitObjects;

            Assert.That(second.Select(h => (h.GetType(), h.StartTime, h.Position)), Is.EqualTo(first.Select(h => (h.GetType(), h.StartTime, h.Position))));
            Assert.That(other.Select(h => h.Position), Is.Not.EqualTo(first.Select(h => h.Position)));
        }

        [Test]
        public void TestExerciseFillsTheDurationInsideThePlayfield()
        {
            var beatmap = playable(ExerciseGenerator.Generate(settings, mixed, 3));
            var objects = beatmap.HitObjects.Cast<CalibreHitObject>().ToArray();

            Assert.That(objects.First().StartTime, Is.EqualTo(ExerciseGenerator.START_TIME));
            Assert.That(objects.Last().StartTime, Is.InRange(ExerciseGenerator.START_TIME + 50000, ExerciseGenerator.START_TIME + 62000));

            foreach (var hitObject in objects)
            {
                foreach (var point in new[] { hitObject.Position, (hitObject as Slider)?.EndPosition ?? hitObject.Position })
                {
                    Assert.That(point.X, Is.InRange(ExerciseGenerator.RADIUS - 0.01f, CalibrePlayfield.BASE_SIZE.X - ExerciseGenerator.RADIUS + 0.01f));
                    Assert.That(point.Y, Is.InRange(ExerciseGenerator.RADIUS - 0.01f, CalibrePlayfield.BASE_SIZE.Y - ExerciseGenerator.RADIUS + 0.01f));
                }
            }
        }

        [Test]
        public void TestObjectsNeverOverlapInTime()
        {
            var objects = playable(ExerciseGenerator.Generate(settings, mixed, 11)).HitObjects.ToArray();

            for (int i = 1; i < objects.Length; i++)
                Assert.That(objects[i].StartTime, Is.GreaterThan(objects[i - 1].GetEndTime()), $"objet {i}");
        }

        // L'analyse du J4 doit retrouver, dans chaque exercice, le pattern qu'il entraîne.
        [TestCase(PatternType.Stream, 120)]
        [TestCase(PatternType.Stream, 240)]
        [TestCase(PatternType.ShortSharpJump, 180)]
        [TestCase(PatternType.ShortWideJump, 180)]
        [TestCase(PatternType.LongSharpJump, 180)]
        [TestCase(PatternType.LongWideJump, 120)]
        [TestCase(PatternType.LongWideJump, 240)]
        [TestCase(PatternType.Slider, 180)]
        public void TestAnalysisFindsTheTrainedPattern(PatternType type, double bpm)
        {
            for (int seed = 0; seed < 5; seed++)
            {
                var beatmap = playable(ExerciseGenerator.Generate(new ExerciseSettings(bpm, 60, 1), new[] { new PatternTarget(type, 1) }, seed));
                var hits = perfectHits(beatmap);
                double beat = 60000 / bpm;

                // Le premier objet de chaque bloc suit une noire de repos : il n'appartient à aucun enchaînement, on l'écarte.
                var types = Enumerable.Range(0, hits.Length)
                                      .Where(i => i > 0 && hits[i].Time - hits[i - 1].EndTime < 0.75 * beat)
                                      .Select(i => PatternClassifier.Classify(hits, i))
                                      .ToArray();

                Assert.That(types.Count(t => t == type) / (double)types.Length, Is.GreaterThanOrEqualTo(0.9), $"graine {seed} : {string.Join(", ", types.GroupBy(t => t).Select(g => $"{g.Key} {g.Count()}"))}");
            }
        }

        [Test]
        public void TestHigherLevelMeansLongerStreamsAndJumps()
        {
            var easyStream = ExerciseGenerator.Generate(settings, new[] { new PatternTarget(PatternType.Stream, 0.5) }, 1).HitObjects;
            var hardStream = ExerciseGenerator.Generate(settings, new[] { new PatternTarget(PatternType.Stream, 2) }, 1).HitObjects;

            // Même durée : des streams plus longs laissent moins de pauses, donc plus de notes.
            Assert.That(hardStream.Count, Is.GreaterThan(easyStream.Count));

            Assert.That(meanSpacing(new PatternTarget(PatternType.ShortWideJump, 2)), Is.GreaterThan(meanSpacing(new PatternTarget(PatternType.ShortWideJump, 0.5))));
            Assert.That(meanSpacing(new PatternTarget(PatternType.LongWideJump, 2)), Is.GreaterThan(meanSpacing(new PatternTarget(PatternType.LongWideJump, 1))));
        }

        [Test]
        public void TestInvalidRequestsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => ExerciseGenerator.Generate(settings, Array.Empty<PatternTarget>(), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExerciseGenerator.Generate(settings with { Bpm = 100 }, mixed, 1));
            Assert.Throws<ArgumentException>(() => ExerciseGenerator.Generate(settings, new[] { new PatternTarget(PatternType.Isolated, 1) }, 1));
        }

        private static double meanSpacing(PatternTarget target)
        {
            var objects = ExerciseGenerator.Generate(settings, new[] { target }, 5).HitObjects;
            return Enumerable.Range(1, objects.Count - 1).Average(i => Vector2.Distance(objects[i].Position, objects[i - 1].Position));
        }

        private static IBeatmap playable(IBeatmap beatmap)
        {
            beatmap.BeatmapInfo.Ruleset = new CalibreRuleset().RulesetInfo;
            return new FlatWorkingBeatmap(beatmap).GetPlayableBeatmap(beatmap.BeatmapInfo.Ruleset);
        }

        /// <summary>
        /// Partie parfaite simulée : chaque objet frappé pile au centre, comme le ferait l'autoplay.
        /// </summary>
        private static TimedHit[] perfectHits(IBeatmap beatmap)
            => beatmap.HitObjects.Cast<CalibreHitObject>()
                      .Select(h => new TimedHit(h.StartTime, 0, HitResult.Great, h.Position, h.Position, h.Radius, h.GetEndTime(),
                          (h as Slider)?.EndPosition ?? h.Position, h is Slider))
                      .ToArray();
    }
}

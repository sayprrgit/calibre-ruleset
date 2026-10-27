// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Tests.Logic.Analysis;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Training
{
    [TestFixture]
    public class PlayerProfileTest
    {
        private static readonly DateTimeOffset now = new DateTimeOffset(2026, 10, 27, 18, 0, 0, TimeSpan.Zero);

        [TestCase(1, 0.96, 1.1)]
        [TestCase(1, 0.95, 1.1)]
        [TestCase(1, 0.94, 1)]
        [TestCase(1, 0.80, 1)]
        [TestCase(1, 0.79, 0.9)]
        [TestCase(2, 1, 2)]
        [TestCase(0.5, 0, 0.5)]
        public void TestLevelProgression(double level, double accuracy, double expected)
        {
            Assert.That(DifficultyController.Next(level, accuracy), Is.EqualTo(expected));
        }

        [Test]
        public void TestRepeatedStepsStayRound()
        {
            double level = 1;

            for (int i = 0; i < 7; i++)
                level = DifficultyController.Next(level, 1);

            Assert.That(level, Is.EqualTo(1.7));
        }

        [Test]
        public void TestGamesAreAccumulated()
        {
            var profile = new PlayerProfile();

            // Première partie : 4 frappes à 0 et 10 ms ; seconde : 4 frappes à 20 et 30 ms, et un miss.
            profile.Apply(PatternClassifier.Summarise(stream(0, 10, 0, 10)), now);
            profile.Apply(PatternClassifier.Summarise(stream(20, 30, 20, 30, null)), now);

            var record = profile.Patterns[PatternType.Stream];

            Assert.That(profile.GamesAnalysed, Is.EqualTo(2));
            Assert.That(profile.LastUpdated, Is.EqualTo(now));
            Assert.That(record.Objects, Is.EqualTo(3 + 4), "le premier objet de chaque partie est isolé");
            Assert.That(record.TimedHits, Is.EqualTo(6), "le miss n'a pas d'écart");

            // UR des 6 frappes des deux parties réunies, recalculé exactement depuis les sommes.
            double[] offsets = { 10, 0, 10, 30, 20, 30 };
            double mean = offsets.Average();
            double expectedUr = 10 * Math.Sqrt(offsets.Average(o => (o - mean) * (o - mean)));
            Assert.That(record.UnstableRate, Is.EqualTo(expectedUr).Within(1e-9));
            Assert.That(record.Accuracy, Is.EqualTo(6 / 7.0).Within(1e-12));
        }

        [Test]
        public void TestWeakestPatterns()
        {
            var profile = new PlayerProfile
            {
                Patterns =
                {
                    [PatternType.Stream] = record(40, 0.70),
                    [PatternType.LongWideJump] = record(40, 0.90),
                    [PatternType.ShortSharpJump] = record(40, 0.80),
                    [PatternType.Slider] = record(10, 0.10), // trop peu joué pour être jugé
                    [PatternType.Isolated] = record(40, 0.05), // pas entraînable
                    [PatternType.LongSharpJump] = record(40, 0.95),
                },
            };

            Assert.That(profile.WeakestPatterns(), Is.EqualTo(new[] { PatternType.Stream, PatternType.ShortSharpJump, PatternType.LongWideJump }));
            Assert.That(new PlayerProfile().WeakestPatterns(), Is.Empty);
        }

        [Test]
        public void TestStoreRoundTripAndCorruptFile()
        {
            string directory = Path.Combine(Path.GetTempPath(), "calibre-tests", Guid.NewGuid().ToString("N"));
            var storage = new NativeStorage(directory);

            try
            {
                Assert.That(ProfileStore.Load(storage).GamesAnalysed, Is.Zero, "pas encore de profil");

                ProfileStore.Update(storage, p => p.Apply(PatternClassifier.Summarise(stream(5, 5, 5, 5)), now));
                var loaded = ProfileStore.Load(storage);

                Assert.That(loaded.GamesAnalysed, Is.EqualTo(1));
                Assert.That(loaded.Patterns[PatternType.Stream].Objects, Is.EqualTo(3));

                string file = Path.Combine(directory, ProfileStore.DIRECTORY, ProfileStore.FILE_NAME);
                File.WriteAllText(file, "{ pas du json");

                Assert.That(ProfileStore.Load(storage).GamesAnalysed, Is.Zero, "profil illisible : profil vierge");
                Assert.That(File.ReadAllText(Path.Combine(directory, ProfileStore.DIRECTORY, ProfileStore.CORRUPT_FILE_NAME)), Is.EqualTo("{ pas du json"), "copie conservée");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static PatternRecord record(int objects, double accuracy) => new PatternRecord { Objects = objects, AccuracySum = objects * accuracy };

        /// <summary>
        /// Stream de frappes à 100 ms d'écart : un écart null est un miss.
        /// </summary>
        private static TimedHit[] stream(params double?[] offsets)
            => offsets.Select((offset, i) => PatternClassifierTest.hit(i * 100, 100 + 20 * i, 100, offset == null ? HitResult.Miss : HitResult.Great, offset ?? 0)).ToArray();
    }
}

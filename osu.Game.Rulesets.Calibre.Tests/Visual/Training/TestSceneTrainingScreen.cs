// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Screens.Play;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Training
{
    public partial class TestSceneTrainingScreen : ScreenTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        private TrainingScreen screen = null!;

        [Test]
        public void TestFirstExerciseUsesDefaultTargets()
        {
            AddStep("effacer le profil", () => TestSceneProfileRecorder.DeleteProfile(storage));
            loadScreen();

            AddAssert("patterns par défaut", () => screen.Targets, () => Is.EqualTo(TrainingScreen.DEFAULT_TARGETS));
        }

        [Test]
        public void TestWeakestPatternsAreTargeted()
        {
            AddStep("profil avec des faiblesses", () =>
            {
                TestSceneProfileRecorder.DeleteProfile(storage);
                ProfileStore.Update(storage, profile =>
                {
                    profile.GamesAnalysed = 3;
                    // Sommes d'écarts choisies pour des UR de 90, 160, 130 et 110.
                    profile.Patterns[PatternType.Stream] = new PatternRecord { Objects = 60, AccuracySum = 60 * 0.98, TimedHits = 60, OffsetSum = -180, OffsetSquareSum = 5400, Level = 1.3 };
                    profile.Patterns[PatternType.LongSharpJump] = new PatternRecord { Objects = 40, AccuracySum = 40 * 0.62, TimedHits = 40, OffsetSum = 240, OffsetSquareSum = 11680 };
                    profile.Patterns[PatternType.ShortWideJump] = new PatternRecord { Objects = 40, AccuracySum = 40 * 0.75, TimedHits = 40, OffsetSum = 80, OffsetSquareSum = 6920 };
                    profile.Patterns[PatternType.Slider] = new PatternRecord { Objects = 30, AccuracySum = 30 * 0.85, TimedHits = 30, OffsetSum = -30, OffsetSquareSum = 3660 };
                });
            });

            loadScreen();

            AddAssert("trois patterns les plus faibles", () => screen.Targets,
                () => Is.EqualTo(new[] { PatternType.LongSharpJump, PatternType.ShortWideJump, PatternType.Slider }));
        }

        [Test]
        public void TestExerciseIsLaunched()
        {
            AddStep("effacer le profil", () => TestSceneProfileRecorder.DeleteProfile(storage));
            loadScreen(seed: 1234);

            AddStep("court exercice", () =>
            {
                screen.Duration.Value = 30;
                screen.Bpm.Value = 200;
            });
            AddStep("lancer", () => screen.ChildrenOfType<RoundedButton>().Single(b => b.Text.ToString() == "Lancer l'exercice").TriggerClick());

            AddUntilStep("chargement de la partie", () => Stack.CurrentScreen is PlayerLoader);
            AddAssert("graine imposée", () => screen.LastSeed, () => Is.EqualTo(1234));
            AddAssert("beatmap d'exercice courante", () => Beatmap.Value.Beatmap.HitObjects, () => Is.EqualTo(screen.LastExercise!.HitObjects));
            AddAssert("ruleset Calibre", () => Ruleset.Value.ShortName, () => Is.EqualTo("calibre"));
            AddAssert("durée respectée", () => screen.LastExercise!.HitObjects.Cast<CalibreHitObject>().Last().StartTime,
                () => Is.InRange(ExerciseGenerator.START_TIME + 25000, ExerciseGenerator.START_TIME + 32000));
            AddAssert("piste de métronome", () => Beatmap.Value.Track.Length, () => Is.GreaterThan(30000));
        }

        private void loadScreen(int? seed = null)
        {
            AddStep("ouvrir l'écran", () => LoadScreen(screen = new TrainingScreen(new CalibreRuleset(), seed)));
            AddUntilStep("écran affiché", () => screen.IsCurrentScreen());
        }
    }
}

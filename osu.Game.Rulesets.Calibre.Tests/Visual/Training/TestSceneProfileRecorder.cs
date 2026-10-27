// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Training
{
    /// <summary>
    /// Une partie jouée jusqu'au bout (ici sans aucune frappe) met à jour le profil : un stream de 12 cercles.
    /// PlayerTestScene ajoute le No Fail du ruleset tant que <c>AllowFail</c> vaut faux : la partie va donc au bout.
    /// </summary>
    public partial class TestSceneProfileRecorder : PlayerTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => CreateStream(ruleset);

        [SetUpSteps]
        public override void SetUpSteps()
        {
            AddStep("effacer le profil", () => DeleteProfile(storage));
            base.SetUpSteps();
        }

        [Test]
        public void TestCompletedPlayUpdatesTheProfile()
        {
            AddUntilStep("profil mis à jour", () => ProfileStore.Load(storage).GamesAnalysed, () => Is.EqualTo(1));
            AddAssert("11 objets de stream (le premier est isolé)", () => ProfileStore.Load(storage).Patterns[PatternType.Stream].Objects, () => Is.EqualTo(11));
            AddAssert("tout manqué", () => ProfileStore.Load(storage).Patterns[PatternType.Stream].Accuracy, () => Is.Zero);
            AddAssert("niveau abaissé après une partie ratée", () => ProfileStore.Load(storage).LevelOf(PatternType.Stream), () => Is.EqualTo(0.9));
        }

        internal static IBeatmap CreateStream(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap<CalibreHitObject>
            {
                BeatmapInfo = { Ruleset = ruleset },
                Difficulty = new BeatmapDifficulty { DrainRate = 0, OverallDifficulty = 5, CircleSize = 4 },
            };

            for (int i = 0; i < 12; i++)
                beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 + 100 * i, Position = new Vector2(100 + 20 * i, 200) });

            return beatmap;
        }

        internal static void DeleteProfile(Storage storage)
        {
            var directory = storage.GetStorageForDirectory(ProfileStore.DIRECTORY);

            if (directory.Exists(ProfileStore.FILE_NAME))
                directory.Delete(ProfileStore.FILE_NAME);
        }
    }

    /// <summary>
    /// Sans No Fail, la même partie sans frappe échoue : son bilan est tronqué et ne doit pas entrer dans le profil.
    /// </summary>
    public partial class TestSceneProfileRecorderFailed : PlayerTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        // Sans cela, PlayerTestScene ajouterait le No Fail du ruleset.
        protected override bool AllowFail => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => TestSceneProfileRecorder.CreateStream(ruleset);

        [SetUpSteps]
        public override void SetUpSteps()
        {
            AddStep("effacer le profil", () => TestSceneProfileRecorder.DeleteProfile(storage));
            base.SetUpSteps();
        }

        [Test]
        public void TestFailedPlayIsIgnored()
        {
            AddUntilStep("partie ratée", () => Player.GameplayState.HasFailed);
            AddWaitStep("laisser une éventuelle mise à jour s'écrire", 5);
            AddAssert("profil inchangé", () => ProfileStore.Load(storage).GamesAnalysed, () => Is.Zero);
        }
    }

    /// <summary>
    /// Une partie en autoplay (un replay) ne doit pas toucher au profil du joueur.
    /// </summary>
    public partial class TestSceneProfileRecorderAutoplay : PlayerTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        protected override bool Autoplay => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => TestSceneProfileRecorder.CreateStream(ruleset);

        [SetUpSteps]
        public override void SetUpSteps()
        {
            AddStep("effacer le profil", () => TestSceneProfileRecorder.DeleteProfile(storage));
            base.SetUpSteps();
        }

        [Test]
        public void TestAutoplayIsIgnored()
        {
            AddUntilStep("partie terminée", () => Player.ScoreProcessor.HasCompleted.Value);
            AddWaitStep("laisser une éventuelle mise à jour s'écrire", 5);
            AddAssert("profil inchangé", () => ProfileStore.Load(storage).GamesAnalysed, () => Is.Zero);
        }
    }
}

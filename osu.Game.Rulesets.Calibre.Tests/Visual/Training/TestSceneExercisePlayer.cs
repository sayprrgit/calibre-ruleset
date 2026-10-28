// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Training
{
    /// <summary>
    /// Joue un exercice généré en autoplay, de bout en bout dans un vrai Player : chaque objet produit doit être jouable.
    /// </summary>
    public partial class TestSceneExercisePlayer : PlayerTestScene
    {
        protected override bool Autoplay => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = ExerciseGenerator.Generate(new ExerciseSettings(180, 30, 1), new[]
            {
                new PatternTarget(PatternType.Stream, 1),
                new PatternTarget(PatternType.ShortWideJump, 1),
                new PatternTarget(PatternType.Slider, 1),
            }, 42);

            beatmap.BeatmapInfo.Ruleset = ruleset;
            return beatmap;
        }

        [Test]
        public void TestAutoplayClearsTheExercise()
        {
            AddUntilStep("tous les objets jugés", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("précision de 100 %", () => Player.ScoreProcessor.Accuracy.Value, () => Is.EqualTo(1));
            AddAssert("combo complet", () => Player.ScoreProcessor.HighestCombo.Value, () => Is.EqualTo(Player.ScoreProcessor.MaximumCombo));
        }
    }
}

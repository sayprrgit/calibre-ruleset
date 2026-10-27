// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Sans aucune entrée, chaque cercle est manqué : la vie doit baisser jusqu'à l'échec de la partie.
    /// </summary>
    [TestFixture]
    public partial class TestSceneHealth : PlayerTestScene
    {
        // Sans cela, PlayerTestScene ajouterait le No Fail du ruleset.
        protected override bool AllowFail => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap<CalibreHitObject>
            {
                BeatmapInfo = { Ruleset = ruleset },
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8, ApproachRate = 9, CircleSize = 4, DrainRate = 5 },
            };

            for (int i = 0; i < 60; i++)
                beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 + 250 * i, Position = new Vector2(64 + 48 * (i % 9), 96 + 96 * (i % 3)) });

            return beatmap;
        }

        [Test]
        public void TestMissingEverythingFails()
        {
            double initialHealth = 0;

            AddStep("relever la vie", () => initialHealth = Player.HealthProcessor.Health.Value);
            AddUntilStep("premiers misses", () => Player.Results.Count, () => Is.GreaterThanOrEqualTo(3));
            AddAssert("la vie baisse", () => Player.HealthProcessor.Health.Value, () => Is.LessThan(initialHealth));
            AddUntilStep("échec de la partie", () => Player.GameplayState.HasFailed);
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Joue une courte beatmap en autoplay, de bout en bout dans un vrai Player : sauts sur la grille puis un stream.
    /// </summary>
    [TestFixture]
    public partial class TestSceneCalibrePlayer : PlayerTestScene
    {
        protected override bool Autoplay => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap<CalibreHitObject>
            {
                BeatmapInfo = { Ruleset = ruleset },
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8, ApproachRate = 9, CircleSize = 4 },
            };

            double time = 1000;

            // Sauts entre les neuf zones d'une grille 3x3.
            for (int i = 0; i < 9; i++, time += 300)
                beatmap.HitObjects.Add(new HitCircle { StartTime = time, Position = new Vector2(96 + 160 * (i % 3), 72 + 120 * (i / 3)), NewCombo = i == 0 });

            // Stream de 8 notes à 1/4 de temps à 180 BPM.
            for (int i = 0; i < 8; i++, time += 1000 / 12.0)
                beatmap.HitObjects.Add(new HitCircle { StartTime = time + 500, Position = new Vector2(150 + 30 * i, 300 - 10 * i), NewCombo = i == 0 });

            return beatmap;
        }

        [Test]
        public void TestAutoplayHitsEveryCircleGreat()
        {
            AddUntilStep("tous les objets jugés", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("17 jugements", () => Player.Results.Count, () => Is.EqualTo(17));
            AddAssert("aucun résultat autre que Great", () => Player.Results.Where(r => r.Type != HitResult.Great), () => Is.Empty);
            AddAssert("précision de 100 %", () => Player.ScoreProcessor.Accuracy.Value, () => Is.EqualTo(1));
        }
    }
}

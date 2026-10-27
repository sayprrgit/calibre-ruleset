// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Joue en autoplay, dans un vrai Player, des sliders de chaque forme (avec ticks et répétitions), un spinner
    /// puis un cercle : chaque élément doit obtenir son meilleur jugement.
    /// </summary>
    [TestFixture]
    public partial class TestSceneSliderSpinnerPlayer : PlayerTestScene
    {
        protected override bool Autoplay => true;

        protected override Ruleset CreatePlayerRuleset() => new CalibreRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            // Sans point de timing : 1000 ms par temps, donc 140 px par seconde et un tick tous les 140 px.
            return new Beatmap<CalibreHitObject>
            {
                BeatmapInfo = { Ruleset = ruleset },
                Difficulty = new BeatmapDifficulty { OverallDifficulty = 8, ApproachRate = 9, CircleSize = 4 },
                HitObjects =
                {
                    new Slider
                    {
                        StartTime = 1000,
                        Position = new Vector2(80, 80),
                        Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
                        NewCombo = true,
                    },
                    new Slider
                    {
                        StartTime = 3500,
                        Position = new Vector2(120, 300),
                        Path = new SliderPath(PathType.PERFECT_CURVE, new[] { Vector2.Zero, new Vector2(140, -120), new Vector2(280, 0) }),
                    },
                    new Slider
                    {
                        StartTime = 6500,
                        Position = new Vector2(380, 120),
                        Path = new SliderPath(PathType.BEZIER, new[] { Vector2.Zero, new Vector2(-100, 120), new Vector2(-220, 40) }),
                        RepeatCount = 2,
                    },
                    new Spinner
                    {
                        StartTime = 12000,
                        EndTime = 14000,
                        Position = CalibrePlayfield.BASE_SIZE / 2,
                        NewCombo = true,
                    },
                    new HitCircle
                    {
                        StartTime = 15000,
                        Position = new Vector2(256, 300),
                        NewCombo = true,
                    },
                },
            };
        }

        [Test]
        public void TestAutoplayGetsEveryMaximumResult()
        {
            AddUntilStep("tous les objets jugés", () => Player.ScoreProcessor.HasCompleted.Value);

            AddAssert("ticks et répétitions présents", () => Player.Results.Count(r => r.HitObject is SliderTick or SliderRepeat), () => Is.GreaterThanOrEqualTo(4));
            AddAssert("chaque élément au maximum", () => Player.Results.Where(r => r.Type != r.Judgement.MaxResult).Select(r => r.HitObject.GetType().Name), () => Is.Empty);
            AddAssert("précision de 100 %", () => Player.ScoreProcessor.Accuracy.Value, () => Is.EqualTo(1));
            AddAssert("combo complet", () => Player.ScoreProcessor.HighestCombo.Value, () => Is.EqualTo(Player.ScoreProcessor.MaximumCombo));
            AddAssert("pas d'échec", () => Player.GameplayState.HasFailed, () => Is.False);
        }
    }
}

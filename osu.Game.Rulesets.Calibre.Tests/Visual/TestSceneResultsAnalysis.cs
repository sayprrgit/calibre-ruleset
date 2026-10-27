// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Statistics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Affiche les panneaux d'analyse que Calibre fournit à l'écran de résultats, pour une partie simulée :
    /// streams, sauts et sliders, avec un passage plus irrégulier et quelques misses.
    /// </summary>
    public partial class TestSceneResultsAnalysis : OsuTestScene
    {
        private StatisticItem[] items = null!;

        [Test]
        public void TestPanelsForASimulatedPlay()
        {
            AddStep("analyser la partie", () =>
            {
                var (beatmap, events) = simulatePlay();
                items = new CalibreRuleset().CreateStatisticsForScore(new ScoreInfo { HitEvents = events }, beatmap);
                show(items);
            });

            AddAssert("cinq panneaux", () => items.Length, () => Is.EqualTo(5));
            AddUntilStep("panneaux chargés", () => this.ChildrenOfType<HitErrorTimeline>().Any() && this.ChildrenOfType<PositionHeatmapGraph>().Any());
            AddAssert("chaque catégorie de mouvement présente", () => this.ChildrenOfType<PatternTable>().Single().ChildrenOfType<OsuSpriteText>().Select(t => t.Text.ToString()),
                () => Has.Some.EqualTo("Streams").And.Some.EqualTo("Sliders").And.Some.StartsWith("Sauts"));
        }

        [Test]
        public void TestPanelsWithoutHitEvents()
        {
            AddStep("analyser une partie vide", () => show(items = new CalibreRuleset().CreateStatisticsForScore(new ScoreInfo(), simulatePlay().beatmap)));
            AddUntilStep("panneaux chargés", () => this.ChildrenOfType<PositionHeatmapGraph>().Any());
        }

        private void show(StatisticItem[] statisticItems)
        {
            Child = new OsuScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(30),
                    Spacing = new Vector2(0, 20),
                    ChildrenEnumerable = statisticItems.SelectMany(item => new[]
                    {
                        new OsuSpriteText { Text = item.Name },
                        item.CreateContent(),
                    }),
                },
            };
        }

        /// <summary>
        /// Beatmap jouable (streams, sauts, sliders) et HitEvents simulés de façon déterministe.
        /// </summary>
        private static (IBeatmap beatmap, List<HitEvent> events) simulatePlay()
        {
            var random = new Random(4);
            var source = new Beatmap<CalibreHitObject>
            {
                BeatmapInfo = { Ruleset = new CalibreRuleset().RulesetInfo },
                Difficulty = new BeatmapDifficulty { CircleSize = 4, OverallDifficulty = 8 },
            };

            double time = 1000;

            for (int i = 0; i < 200; i++)
            {
                var position = new Vector2(40 + random.Next(432), 40 + random.Next(304));

                if (i % 25 == 0)
                {
                    source.HitObjects.Add(new Slider { StartTime = time, Position = position, Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(80, 0) }) });
                    time += 1200;
                }
                else if (i % 50 < 25 && i % 50 > 12)
                {
                    // Stream : 100 ms d'écart, quelques pixels d'espacement.
                    source.HitObjects.Add(new HitCircle { StartTime = time, Position = new Vector2(100 + i % 25 * 12, 200) });
                    time += 100;
                }
                else
                {
                    source.HitObjects.Add(new HitCircle { StartTime = time, Position = position });
                    time += 300;
                }
            }

            var beatmap = new FlatWorkingBeatmap(source).GetPlayableBeatmap(new CalibreRuleset().RulesetInfo);
            var events = new List<HitEvent>();

            foreach (var hitObject in beatmap.HitObjects.Cast<CalibreHitObject>())
            {
                HitObject timed = hitObject is Slider slider ? slider.HeadCircle! : hitObject;

                // Passage plus irrégulier au milieu de la map.
                double spread = hitObject.StartTime is > 20000 and < 30000 ? 40 : 12;
                double offset = (random.NextDouble() * 2 - 1) * spread;

                if (random.NextDouble() < 0.03)
                {
                    events.Add(new HitEvent(150, 1, HitResult.Miss, timed, null, null));
                    continue;
                }

                var result = Math.Abs(offset) < 31.5 ? HitResult.Great : Math.Abs(offset) < 75.5 ? HitResult.Ok : HitResult.Meh;
                var cursor = hitObject.Position + new Vector2((float)(random.NextDouble() * 2 - 1) * 20, (float)(random.NextDouble() * 2 - 1) * 20);

                events.Add(new HitEvent(offset, 1, result, timed, null, cursor));
            }

            return (beatmap, events);
        }
    }
}

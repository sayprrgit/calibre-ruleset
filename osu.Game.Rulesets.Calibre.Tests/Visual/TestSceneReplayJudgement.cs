// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
// Déroulé adapté de ReplayStabilityTestScene d'osu! (https://github.com/ppy/osu), © ppy Pty Ltd, licence MIT.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Replays;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Vérifie le jugement de bout en bout dans un vrai <see cref="ReplayPlayer"/> : un replay frappe un cercle
    /// avec un décalage et une entrée donnés, et on contrôle le résultat, la touche et la position enregistrées.
    /// </summary>
    /// <remarks>
    /// L'aller-retour par le format .osr n'est pas testable ici : lazer n'encode au format legacy que les scores
    /// des quatre rulesets officiels. La conversion des frames est couverte par <c>CalibreReplayFrameTest</c>.
    /// </remarks>
    public partial class TestSceneReplayJudgement : RateAdjustedBeatmapTestScene
    {
        private const double hit_circle_time = 100;

        private static readonly Vector2 circle_position = CalibrePlayfield.BASE_SIZE / 2;

        private static readonly object[][] test_cases =
        {
            // OD 5 : Great [-49,5 ; 49,5], Ok [-99,5 ; 99,5], Meh [-149,5 ; 149,5].
            new object[] { 5f, 0d, CalibreAction.Key1, HitResult.Great },
            new object[] { 5f, 49d, CalibreAction.Key2, HitResult.Great },
            new object[] { 5f, -49d, CalibreAction.Mouse1, HitResult.Great },
            new object[] { 5f, 50d, CalibreAction.Mouse2, HitResult.Ok },
            new object[] { 5f, -99d, CalibreAction.Key1, HitResult.Ok },
            new object[] { 5f, 100d, CalibreAction.Key2, HitResult.Meh },
            new object[] { 5f, 149d, CalibreAction.Mouse1, HitResult.Meh },

            // OD 10 : Great [-19,5 ; 19,5].
            new object[] { 10f, 19d, CalibreAction.Key1, HitResult.Great },
            new object[] { 10f, 20d, CalibreAction.Key2, HitResult.Ok },
        };

        private readonly List<JudgementResult> results = new List<JudgementResult>();

        private ReplayPlayer currentPlayer = null!;

        [TestCaseSource(nameof(test_cases))]
        public void TestHitWindows(float overallDifficulty, double hitOffset, CalibreAction action, HitResult expectedResult)
        {
            var aim = circle_position + new Vector2(10, -5);

            runReplay(overallDifficulty, new Replay
            {
                Frames =
                {
                    new CalibreReplayFrame(0, aim),
                    new CalibreReplayFrame(hit_circle_time + hitOffset, aim, action),
                    new CalibreReplayFrame(hit_circle_time + hitOffset + 20, aim),
                },
            });

            AddAssert("résultat attendu", () => results.Select(r => r.Type), () => Is.EqualTo(new[] { expectedResult }));
            AddAssert("touche enregistrée", () => ((CalibreHitCircleJudgementResult)results.Single()).HitAction, () => Is.EqualTo(action));
            AddAssert("position de frappe enregistrée", () => ((CalibreHitCircleJudgementResult)results.Single()).CursorPositionAtHit!.Value,
                () => Is.EqualTo(aim).Using(new Vector2Comparer(0.5f)));
        }

        [Test]
        public void TestPressOutsideCircleIsAMiss()
        {
            var aim = circle_position + new Vector2(200, 0);

            runReplay(5, new Replay
            {
                Frames =
                {
                    new CalibreReplayFrame(0, aim),
                    new CalibreReplayFrame(hit_circle_time, aim, CalibreAction.Key1),
                    new CalibreReplayFrame(hit_circle_time + 20, aim),
                },
            });

            AddAssert("miss", () => results.Select(r => r.Type), () => Is.EqualTo(new[] { HitResult.Miss }));
        }

        [Test]
        public void TestPressTooEarlyIsIgnored()
        {
            runReplay(5, new Replay
            {
                Frames =
                {
                    new CalibreReplayFrame(-1000, circle_position),
                    // Hors de la fenêtre de miss (400 ms) : la frappe est ignorée, puis le cercle expire en Miss.
                    new CalibreReplayFrame(hit_circle_time - 450, circle_position, CalibreAction.Key1),
                    new CalibreReplayFrame(hit_circle_time - 430, circle_position),
                },
            });

            AddAssert("miss par expiration, sans frappe", () => results.Select(r => r.Type), () => Is.EqualTo(new[] { HitResult.Miss }));
            AddAssert("aucune touche attribuée", () => ((CalibreHitCircleJudgementResult)results.Single()).HitAction, () => Is.Null);
        }

        /// <summary>
        /// Slider horizontal de 200 px à 0,14 px/ms (sans point de timing) : tick à 140 px (1100 ms), fin à ~1529 ms.
        /// </summary>
        private static Slider createSlider() => new Slider
        {
            StartTime = hit_circle_time,
            Position = new Vector2(156, 192),
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) }),
        };

        [Test]
        public void TestSliderHeldToTheEnd()
        {
            var slider = createSlider();

            runReplay(5, followSlider(slider, slider.StartTime + 1600), slider);

            AddAssert("tête Great, tick, fin, slider touché", () => results.Select(r => r.Type), () => Is.EqualTo(new[]
            {
                HitResult.Great, HitResult.LargeTickHit, HitResult.SliderTailHit, HitResult.IgnoreHit,
            }));
        }

        [Test]
        public void TestSliderReleasedEarly()
        {
            var slider = createSlider();

            // La touche est relâchée à 300 ms, avant le tick : le curseur continue de suivre la bille, sans effet.
            runReplay(5, followSlider(slider, 300), slider);

            AddAssert("tête Great puis tick et fin manqués", () => results.Select(r => r.Type), () => Is.EqualTo(new[]
            {
                HitResult.Great, HitResult.LargeTickMiss, HitResult.IgnoreMiss, HitResult.IgnoreHit,
            }));
        }

        [Test]
        public void TestSliderLeftByTheCursor()
        {
            var slider = createSlider();

            // Touche maintenue, mais le curseur reste sur la tête : la bille s'éloigne au-delà de la zone de suivi (2,4 rayons).
            runReplay(5, new Replay
            {
                Frames =
                {
                    new CalibreReplayFrame(0, slider.Position),
                    new CalibreReplayFrame(slider.StartTime, slider.Position, CalibreAction.Key1),
                    new CalibreReplayFrame(slider.StartTime + 1700, slider.Position, CalibreAction.Key1),
                    new CalibreReplayFrame(slider.StartTime + 1720, slider.Position),
                },
            }, slider);

            AddAssert("tête Great, tick et fin manqués", () => results.Select(r => r.Type).Take(3), () => Is.EqualTo(new[]
            {
                HitResult.Great, HitResult.LargeTickMiss, HitResult.IgnoreMiss,
            }));
        }

        [Test]
        public void TestSpinnerWithoutRotationIsMissed()
        {
            var spinner = new Spinner { StartTime = hit_circle_time, EndTime = hit_circle_time + 2000, Position = circle_position };

            runReplay(5, new Replay
            {
                Frames =
                {
                    new CalibreReplayFrame(0, circle_position + new Vector2(50, 0)),
                    new CalibreReplayFrame(spinner.StartTime, circle_position + new Vector2(50, 0), CalibreAction.Key1),
                    new CalibreReplayFrame(spinner.EndTime + 20, circle_position + new Vector2(50, 0)),
                },
            }, spinner);

            AddAssert("spinner manqué", () => results.Select(r => r.Type), () => Is.EqualTo(new[] { HitResult.Miss }));
        }

        /// <summary>
        /// Frames à 60 Hz qui suivent la bille du slider ; la touche reste enfoncée jusqu'à <paramref name="releaseTime"/>.
        /// </summary>
        private static Replay followSlider(Slider slider, double releaseTime)
        {
            var replay = new Replay();
            replay.Frames.Add(new CalibreReplayFrame(0, slider.Position));

            // Durée calculée comme en jeu : 200 px à 0,14 px/ms.
            double duration = 200 / 0.14;

            for (double time = slider.StartTime; time <= slider.StartTime + duration + 100; time += 1000 / 60.0)
            {
                double progress = Math.Clamp((time - slider.StartTime) / duration, 0, 1);
                var position = slider.Position + new Vector2((float)(200 * progress), 0);

                replay.Frames.Add(time < releaseTime
                    ? new CalibreReplayFrame(time, position, CalibreAction.Key1)
                    : new CalibreReplayFrame(time, position));
            }

            return replay;
        }

        private void runReplay(float overallDifficulty, Replay replay, CalibreHitObject? hitObject = null)
        {
            var beatmap = new Beatmap<CalibreHitObject>
            {
                HitObjects = { hitObject ?? new HitCircle { StartTime = hit_circle_time, Position = circle_position } },
                Difficulty = new BeatmapDifficulty { OverallDifficulty = overallDifficulty },
                BeatmapInfo = { Ruleset = new CalibreRuleset().RulesetInfo },
            };

            AddStep("charger la beatmap", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(beatmap);
                Ruleset.Value = beatmap.BeatmapInfo.Ruleset;
            });

            AddStep("lancer le replay", () =>
            {
                var player = new ReplayPlayer(new Score { Replay = replay, ScoreInfo = new ScoreInfo() });

                player.OnLoadComplete += _ => player.GameplayState.ScoreProcessor.NewJudgement += result =>
                {
                    if (currentPlayer == player)
                        results.Add(result);
                };

                results.Clear();
                LoadScreen(currentPlayer = player);
            });

            AddUntilStep("player chargé", () => currentPlayer.IsCurrentScreen());
            AddStep("passer l'intro", () =>
            {
                if (currentPlayer.ChildrenOfType<GameplayClockContainer>().Single().CurrentTime < -1000)
                    currentPlayer.Seek(-1000);
            });
            AddUntilStep("partie terminée", () => currentPlayer.GameplayState.HasCompleted);
        }

        private class Vector2Comparer : IEqualityComparer<Vector2>
        {
            private readonly float tolerance;

            public Vector2Comparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(Vector2 x, Vector2 y) => Vector2.Distance(x, y) <= tolerance;

            public int GetHashCode(Vector2 obj) => 0;
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Tests.Visual;
using osuTK.Input;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics
{
    /// <summary>
    /// Pilote les tests de latence et de stabilité au clavier, avec les vraies touches de jeu (Z = K1, X = K2),
    /// sur une horloge de métronome avancée à la main.
    /// </summary>
    public partial class TestSceneMetronomeTests : OsuManualInputManagerTestScene
    {
        // 2 temps d'annonce puis 10 temps mesurés, un toutes les 500 ms à partir de 1000 ms.
        private static readonly MetronomeSchedule schedule = new MetronomeSchedule(500, 2, 10);

        // Irrégularités de frappe de somme nulle : elles donnent un écart-type réaliste sans déplacer les moyennes attendues.
        private static readonly int[] jitter_a = { -9, 4, -3, 11, -6, 2, 7, -12, 5, 1, -7, 7 };
        private static readonly int[] jitter_b = { 14, -8, 3, -17, 9, -2, 12, -6, -11, 6, 4, -4 };

        private ManualMetronomeSource source = null!;

        [Test]
        public void TestLatencyTest()
        {
            LatencyTestRunner runner = null!;
            LatencyResult? result = null;

            AddStep("charger le test", () =>
            {
                runner = new LatencyTestRunner(schedule, schedule);
                runner.Completed += r => result = r;
                load(runner);
            });

            startPhase(() => runner);
            tapEveryBeat(beat => 20 + jitter_a[beat], _ => Key.Z);
            finishPhase();

            AddAssert("phase audio audible", () => source.Starts, () => Is.EqualTo(new[] { true }));
            AddAssert("pas encore de résultat", () => result, () => Is.Null);

            startPhase(() => runner);
            tapEveryBeat(beat => 40 + jitter_b[beat], beat => beat % 2 == 0 ? Key.Z : Key.X);
            finishPhase();

            AddAssert("phase visuelle muette", () => source.Starts, () => Is.EqualTo(new[] { true, false }));
            AddAssert("métronome arrêté", () => source.IsPlaying, () => Is.False);
            AddAssert("10 temps par phase", () => (result!.Audio.Statistics.Count, result.Visual.Statistics.Count), () => Is.EqualTo((10, 10)));
            AddAssert("offset audio recommandé", () => result!.RecommendedAudioOffset, () => Is.EqualTo(-20));
            AddAssert("offset visuel recommandé", () => result!.RecommendedVisualOffset, () => Is.EqualTo(-40));
            AddStep("afficher les résultats", () => Child = new Container { RelativeSizeAxes = Axes.Both, Padding = new MarginPadding(40), Child = new LatencyResultView(result!) });
        }

        [Test]
        public void TestKeyStabilityTest()
        {
            KeyStabilityTestRunner runner = null!;
            KeyStabilityResult? result = null;

            // 24 temps mesurés : 12 frappes par touche, au-dessus du minimum de comparaison.
            var stabilitySchedule = new MetronomeSchedule(300, 2, 24);

            AddStep("charger le test", () =>
            {
                runner = new KeyStabilityTestRunner(stabilitySchedule);
                runner.Completed += r => result = r;
                load(runner);
            });

            startPhase(() => runner);
            tapEveryBeat(beat => beat % 2 == 0 ? jitter_a[beat / 2] : 20 + jitter_b[beat / 2], beat => beat % 2 == 0 ? Key.Z : Key.X, stabilitySchedule);
            finishPhase(stabilitySchedule);

            AddAssert("K1 et K2 mesurées", () => result!.Keys.Select(k => k.Action), () => Is.EqualTo(new[] { CalibreAction.Key1, CalibreAction.Key2 }));
            AddAssert("K2 traînante", () => result!.Keyboard!.LaggingKey, () => Is.EqualTo(CalibreAction.Key2));
            AddStep("afficher les résultats", () => Child = new OsuScrollContainer { RelativeSizeAxes = Axes.Both, Padding = new MarginPadding(40), Child = new KeyStabilityResultView(result!) });
        }


        private void load(Drawable runner)
        {
            source = new ManualMetronomeSource();

            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies = new (Type, object)[] { (typeof(IMetronomeSource), source) },
                Child = new CalibreInputManager(new CalibreRuleset().RulesetInfo)
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(40),
                        Child = runner,
                    },
                },
            };
        }

        private void startPhase(Func<MetronomeTestRunner> runner)
        {
            AddUntilStep("bouton de démarrage visible", () => runner().ChildrenOfType<RoundedButton>().Single().IsPresent);
            AddStep("démarrer", () => runner().ChildrenOfType<RoundedButton>().Single().TriggerClick());
            AddUntilStep("phase en cours", () => runner().IsRunning);
        }

        private void tapEveryBeat(Func<int, double> offsetFor, Func<int, Key> keyFor, MetronomeSchedule? tapSchedule = null)
        {
            var s = tapSchedule ?? schedule;

            for (int i = 0; i < s.MeasuredBeats; i++)
            {
                int beat = i;

                AddStep($"frappe {beat + 1}", () =>
                {
                    source.Clock.CurrentTime = s.BeatTime(s.LeadInBeats + beat) + offsetFor(beat);
                    InputManager.Key(keyFor(beat));
                });
            }
        }

        private void finishPhase(MetronomeSchedule? phaseSchedule = null)
        {
            var s = phaseSchedule ?? schedule;

            AddStep("fin du métronome", () => source.Clock.CurrentTime = s.EndTime);
            AddUntilStep("phase terminée", () => !source.IsPlaying);
        }
    }
}

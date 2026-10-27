// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Timing;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics
{
    /// <summary>
    /// Pilote le test d'aim à la souris et au clavier : le curseur rejoint chaque cible et frappe avec Z (K1),
    /// sauf dans la zone en bas à droite où il vise systématiquement à côté.
    /// </summary>
    public partial class TestSceneAimGridTest : OsuManualInputManagerTestScene
    {
        private const int seed = 3;

        private readonly ManualClock clock = new ManualClock { IsRunning = true };

        private AimGridTestRunner runner = null!;
        private AimGridResult? result;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("charger le test", () =>
            {
                result = null;
                clock.CurrentTime = 0;

                runner = new AimGridTestRunner(seed, 1, clock);
                runner.Completed += r => result = r;

                Child = new CalibreInputManager(new CalibreRuleset().RulesetInfo)
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = runner,
                };
            });

            AddStep("démarrer", () => runner.ChildrenOfType<RoundedButton>().Single().TriggerClick());
        }

        [Test]
        public void TestWeakZoneIsFound()
        {
            var targets = AimTargetSequence.Generate(seed, 1, AimGridTestRunner.DEFAULT_RADIUS);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                // Bas droite : frappe à 1,5 rayon du centre, donc hors de la cible.
                var aim = target.Zone == 8 ? target.Position + new Vector2(AimGridTestRunner.DEFAULT_RADIUS * 1.5f, 0) : target.Position + new Vector2(4, 0);

                AddStep($"attendre la cible {i + 1}", () => clock.CurrentTime += AimTargetSequence.MAXIMUM_DELAY);
                AddUntilStep("cible affichée", () => runner.CurrentTarget?.Position, () => Is.EqualTo(target.Position));
                AddStep("viser et frapper", () =>
                {
                    clock.CurrentTime += 300;
                    InputManager.MoveMouseTo(runner.PlayfieldToScreenSpace(aim));
                    InputManager.Key(Key.Z);
                });
            }

            AddUntilStep("test terminé", () => result != null);
            AddAssert("9 tentatives, 8 touchées", () => (result!.Attempts.Count, result.Attempts.Count(a => a.Hit)), () => Is.EqualTo((9, 8)));
            AddAssert("temps de réaction de 300 ms", () => result!.Attempts.Select(a => a.ReactionTime), () => Has.All.EqualTo(300).Within(1e-6));
            AddAssert("distance mesurée en pixels de l'aire de jeu", () => result!.Zones[0].MeanDistance, () => Is.EqualTo(4).Within(0.5));
            AddAssert("zone faible : bas droite", () => result!.WeakZone, () => Is.EqualTo(8));
            AddStep("afficher les résultats", () => Child = new OsuScrollContainer { RelativeSizeAxes = Axes.Both, Child = new AimGridResultView(result!) });
        }

        [Test]
        public void TestUnansweredTargetsExpire()
        {
            for (int i = 0; i < 9; i++)
            {
                AddStep("attendre la cible", () => clock.CurrentTime += AimTargetSequence.MAXIMUM_DELAY);
                AddUntilStep("cible affichée", () => runner.CurrentTarget != null);
                AddStep("laisser expirer", () => clock.CurrentTime += AimGridTestRunner.TIMEOUT);
                AddUntilStep("cible retirée", () => runner.CurrentTarget == null);
            }

            AddUntilStep("test terminé", () => result != null);
            AddAssert("tout est manqué, sans frappe", () => result!.Attempts.All(a => !a.Hit && a.Press == null));
            AddAssert("taux de miss de 100 %", () => result!.Zones.Select(z => z.MissRate), () => Has.All.EqualTo(1));
        }
    }
}

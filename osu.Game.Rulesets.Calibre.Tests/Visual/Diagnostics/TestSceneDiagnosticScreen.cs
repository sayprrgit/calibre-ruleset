// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics
{
    /// <summary>
    /// Écran Diagnostic complet dans une pile d'écrans : navigation entre les tests, arrêt du métronome, export.
    /// </summary>
    public partial class TestSceneDiagnosticScreen : ScreenTestScene
    {
        private DiagnosticScreen screen = null!;
        private ManualMetronomeSource source = null!;

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("ouvrir l'écran", () =>
            {
                source = new ManualMetronomeSource();
                LoadScreen(screen = new DiagnosticScreen(new CalibreRuleset(), source));
            });
            AddUntilStep("écran affiché", () => screen.IsCurrentScreen());
        }

        [Test]
        public void TestNavigationBetweenTests()
        {
            AddAssert("export désactivé sans résultat", () => button("Exporter en JSON").Enabled.Value, () => Is.False);

            AddStep("ouvrir la latence", () => button("Latence perçue").TriggerClick());
            AddUntilStep("test de latence affiché", () => screen.CurrentContent is LatencyTestRunner);

            AddStep("démarrer la phase audio", () => screen.ChildrenOfType<LatencyTestRunner>().Single().ChildrenOfType<RoundedButton>().Single().TriggerClick());
            AddUntilStep("métronome lancé", () => source.IsPlaying);

            AddStep("passer à l'aim", () => button("Précision d'aim").TriggerClick());
            AddUntilStep("test d'aim affiché", () => screen.CurrentContent is AimGridTestRunner);
            AddAssert("métronome arrêté en quittant le test", () => source.IsPlaying, () => Is.False);

            AddStep("ouvrir la stabilité", () => button("Stabilité par touche").TriggerClick());
            AddUntilStep("test de stabilité affiché", () => screen.CurrentContent is KeyStabilityTestRunner);
        }

        [Test]
        public void TestExitStopsTheMetronome()
        {
            AddStep("ouvrir la stabilité", () => button("Stabilité par touche").TriggerClick());
            AddStep("démarrer", () => screen.ChildrenOfType<KeyStabilityTestRunner>().Single().ChildrenOfType<RoundedButton>().Single().TriggerClick());
            AddUntilStep("métronome lancé", () => source.IsPlaying);

            AddStep("quitter l'écran", () => screen.Exit());
            AddAssert("métronome arrêté", () => source.IsPlaying, () => Is.False);
        }

        [Test]
        public void TestSettingsOpenTheDiagnostic()
        {
            AddStep("afficher les réglages du ruleset", () => Add(new CalibreRuleset().CreateSettings()));
            AddUntilStep("bouton Diagnostic présent", () => this.ChildrenOfType<SettingsButton>().Any(b => b.Text.ToString() == "Diagnostic des périphériques"));
        }

        private RoundedButton button(string text) => screen.ChildrenOfType<RoundedButton>().First(b => b.Text.ToString().StartsWith(text, System.StringComparison.Ordinal));
    }
}

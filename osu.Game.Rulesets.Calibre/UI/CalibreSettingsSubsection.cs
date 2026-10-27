// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Screens;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Section de Calibre dans les réglages de lazer : point d'entrée des écrans propres au ruleset.
    /// </summary>
    public partial class CalibreSettingsSubsection : RulesetSettingsSubsection
    {
        private readonly Ruleset ruleset;

        [Resolved(canBeNull: true)]
        private IPerformFromScreenRunner? performer { get; set; }

        public CalibreSettingsSubsection(Ruleset ruleset)
            : base(ruleset)
        {
            this.ruleset = ruleset;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = "Diagnostic des périphériques",
                    TooltipText = "Latence perçue, stabilité par touche et précision d'aim par zone",
                    // Comme les réglages du jeu, on revient au menu principal avant d'ouvrir l'écran.
                    Action = () => performer?.PerformFromScreen(screen => screen.Push(new DiagnosticScreen(ruleset))),
                },
                new SettingsButton
                {
                    Text = "Entraînement adaptatif",
                    TooltipText = "Exercices générés sur les patterns les plus faibles de ton profil",
                    Action = () => performer?.PerformFromScreen(screen => screen.Push(new TrainingScreen(ruleset))),
                },
            };
        }
    }
}

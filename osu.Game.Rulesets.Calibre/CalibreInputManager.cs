// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Calibre
{
    public partial class CalibreInputManager : RulesetInputManager<CalibreAction>
    {
        public CalibreInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.Unique)
        {
        }

        /// <summary>
        /// Vrai si au moins une entrée de frappe est maintenue. Appelé à chaque frame par les sliders et spinners :
        /// <c>PressedActions</c> est une structure énumérée sans allocation.
        /// </summary>
        public bool IsAnyHitActionPressed()
        {
            foreach (var action in KeyBindingContainer.PressedActions)
            {
                if (action.IsHitAction())
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Les quatre entrées de frappe sont distinguées (et non fusionnées en « gauche / droite »)
    /// pour que le module Diagnostic puisse mesurer la stabilité de chaque touche séparément.
    /// </summary>
    public enum CalibreAction
    {
        [Description("Touche 1 (K1)")]
        Key1,

        [Description("Touche 2 (K2)")]
        Key2,

        [Description("Souris 1 (M1)")]
        Mouse1,

        [Description("Souris 2 (M2)")]
        Mouse2,
    }

    public static class CalibreActionExtensions
    {
        public static bool IsHitAction(this CalibreAction action) => action <= CalibreAction.Mouse2;

        /// <summary>
        /// Nom court affiché et exporté : K1, K2, M1 ou M2.
        /// </summary>
        public static string ShortName(this CalibreAction action)
        {
            switch (action)
            {
                case CalibreAction.Key1:
                    return "K1";

                case CalibreAction.Key2:
                    return "K2";

                case CalibreAction.Mouse1:
                    return "M1";

                default:
                    return "M2";
            }
        }
    }
}

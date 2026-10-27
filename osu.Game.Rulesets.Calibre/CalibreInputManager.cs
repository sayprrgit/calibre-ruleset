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
    }
}

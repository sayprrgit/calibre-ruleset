// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Beatmaps;
using osu.Game.Rulesets.Calibre.Mods;
using osu.Game.Rulesets.Calibre.Scoring;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Calibre
{
    public partial class CalibreRuleset : Ruleset
    {
        /// <remarks>
        /// Le nom de l'assembly et de la DLL doit rester « osu.Game.Rulesets.Calibre » : lazer ne charge que les fichiers
        /// <c>osu.Game.Rulesets.*.dll</c>, et sa version distribuée refuse ceux dont le nom contient certains mots
        /// (dont « trainer », « plugin », « loader » ou « inject »), vérification observée par analyse statique du binaire.
        /// </remarks>
        public override string Description => "Calibre";

        public override string ShortName => "calibre";

        public override string PlayingVerb => "S'entraîne";

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null) =>
            new DrawableCalibreRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) =>
            new CalibreBeatmapConverter(beatmap, this);

        public override ScoreProcessor CreateScoreProcessor() => new CalibreScoreProcessor(this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            new CalibreDifficultyCalculator(RulesetInfo, beatmap);

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.Automation:
                    return new[] { new CalibreModAutoplay() };

                default:
                    return Array.Empty<Mod>();
            }
        }

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => new[]
        {
            new KeyBinding(InputKey.Z, CalibreAction.Key1),
            new KeyBinding(InputKey.X, CalibreAction.Key2),
            new KeyBinding(InputKey.MouseLeft, CalibreAction.Mouse1),
            new KeyBinding(InputKey.MouseRight, CalibreAction.Mouse2),
        };

        public override Drawable CreateIcon() => new CalibreRulesetIcon();

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}

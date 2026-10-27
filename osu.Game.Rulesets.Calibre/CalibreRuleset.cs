// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Beatmaps;
using osu.Game.Rulesets.Calibre.Mods;
using osu.Game.Rulesets.Calibre.Scoring;
using osu.Game.Rulesets.Calibre.Statistics;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;

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

        // Le processeur de base calcule les combos (numéros et couleurs) ; sans lui, tous les cercles porteraient le numéro 1.
        public override IBeatmapProcessor CreateBeatmapProcessor(IBeatmap beatmap) => new BeatmapProcessor(beatmap);

        public override ScoreProcessor CreateScoreProcessor() => new CalibreScoreProcessor(this);

        // Le taux de drain est calibré sur la beatmap entière (ticks et queues compris) d'après son HP :
        // une partie sans faute garde une marge, une série de misses fait échouer.
        public override HealthProcessor CreateHealthProcessor(double drainStartTime) => new DrainingHealthProcessor(drainStartTime, 0);

        public override IEnumerable<HitResult> GetValidHitResults() => new[]
        {
            HitResult.Great,
            HitResult.Ok,
            HitResult.Meh,
            HitResult.LargeTickHit,
            HitResult.SliderTailHit,
        };

        /// <summary>
        /// Panneaux d'analyse de l'écran de résultats. lazer appelle cette méthode hors du thread de jeu
        /// (dans une tâche de fond) : toute l'analyse est faite ici, les drawables ne font qu'afficher.
        /// </summary>
        public override StatisticItem[] CreateStatisticsForScore(ScoreInfo score, IBeatmap playableBeatmap)
        {
            var hitEvents = score.HitEvents ?? new List<HitEvent>();
            var timingEvents = hitEvents.Where(HitEventExtensions.AffectsUnstableRate).ToList();

            var hits = TimedHits.From(playableBeatmap, hitEvents);
            var sections = UnstableSectionDetector.Detect(hits);
            var heatmap = PositionHeatmap.From(hits);
            var patterns = PatternClassifier.Summarise(hits);

            return new[]
            {
                new StatisticItem("Distribution des erreurs de timing", () => new HitEventTimingDistributionGraph(timingEvents)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 250,
                }, true, true),
                new StatisticItem(string.Empty, () => new SimpleStatisticTable(2, new SimpleStatisticItem[]
                {
                    new AverageHitError(timingEvents),
                    new UnstableRate(timingEvents),
                }), true, true),
                new StatisticItem("Erreur de timing au fil de la map (sections instables en rouge)", () => new HitErrorTimeline(hits, sections), true, true),
                new StatisticItem("Erreurs de position sur l'aire de jeu", () => new PositionHeatmapGraph(heatmap), true, false),
                new StatisticItem("Par type de pattern", () => new PatternTable(patterns), true, false),
            };
        }

        public override LocalisableString GetDisplayNameForHitResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.LargeTickHit:
                    return "tick de slider";

                case HitResult.SliderTailHit:
                    return "fin de slider";

                default:
                    return base.GetDisplayNameForHitResult(result);
            }
        }

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            new CalibreDifficultyCalculator(RulesetInfo, beatmap);

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.DifficultyReduction:
                    return new[] { new CalibreModNoFail() };

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

        // Les écrans propres à Calibre (Diagnostic, puis Entraînement) s'ouvrent depuis sa section des réglages.
        public override RulesetSettingsSubsection CreateSettings() => new CalibreSettingsSubsection(this);

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}

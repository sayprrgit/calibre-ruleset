// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Scoring
{
    public partial class CalibreScoreProcessor : ScoreProcessor
    {
        public CalibreScoreProcessor(CalibreRuleset ruleset)
            : base(ruleset)
        {
        }

        /// <summary>
        /// Ajoute la position du curseur au <see cref="HitEvent"/> : c'est la donnée de base de la heatmap spatiale.
        /// Les HitEvents sont ensuite exposés par lazer à l'écran de résultats, y compris pour les replays.
        /// </summary>
        protected override HitEvent CreateHitEvent(JudgementResult result)
            => base.CreateHitEvent(result).With((result as CalibreHitCircleJudgementResult)?.CursorPositionAtHit);
    }
}

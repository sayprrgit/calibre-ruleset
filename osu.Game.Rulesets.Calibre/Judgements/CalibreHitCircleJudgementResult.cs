// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Judgements
{
    /// <summary>
    /// Résultat de jugement enrichi des données nécessaires aux analyses post-partie.
    /// </summary>
    public class CalibreHitCircleJudgementResult : JudgementResult
    {
        /// <summary>
        /// Position du curseur au moment de la frappe, dans l'espace de l'aire de jeu (512x384).
        /// Nulle si l'objet a été manqué sans frappe.
        /// </summary>
        public Vector2? CursorPositionAtHit;

        /// <summary>
        /// Entrée qui a déclenché la frappe (K1, K2, M1 ou M2). Nulle pour un miss sans frappe.
        /// </summary>
        public CalibreAction? HitAction;

        public CalibreHitCircleJudgementResult(HitObject hitObject, Judgement judgement)
            : base(hitObject, judgement)
        {
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Judgements
{
    /// <summary>
    /// Jugement d'un objet qui ne compte ni au score ni au combo (le slider lui-même, porteur de ses éléments).
    /// </summary>
    public class CalibreIgnoreJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.IgnoreHit;
    }

    public class SliderTickJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.LargeTickHit;
    }

    public class SliderTailJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.SliderTailHit;
    }
}

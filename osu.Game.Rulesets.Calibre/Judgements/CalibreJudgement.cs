// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Judgements
{
    public class CalibreJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.Great;
    }
}

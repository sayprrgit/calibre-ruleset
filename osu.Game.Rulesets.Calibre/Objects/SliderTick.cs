// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Judgements;

namespace osu.Game.Rulesets.Calibre.Objects
{
    public class SliderTick : SliderNestedObject
    {
        public override Judgement CreateJudgement() => new SliderTickJudgement();
    }
}

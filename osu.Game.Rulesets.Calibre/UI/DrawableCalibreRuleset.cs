// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Objects.Drawables;
using osu.Game.Rulesets.Calibre.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Calibre.UI
{
    [Cached]
    public partial class DrawableCalibreRuleset : DrawableRuleset<CalibreHitObject>
    {
        public DrawableCalibreRuleset(CalibreRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new CalibrePlayfield();

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new CalibreFramedReplayInputHandler(replay);

        public override DrawableHitObject<CalibreHitObject> CreateDrawableRepresentation(CalibreHitObject h) => new DrawableCalibreHitObject(h);

        protected override PassThroughInputManager CreateInputManager() => new CalibreInputManager(Ruleset?.RulesetInfo);
    }
}

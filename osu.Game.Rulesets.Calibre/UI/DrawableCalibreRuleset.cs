// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Replays;
using osu.Game.Rulesets.Calibre.Training;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Calibre.UI
{
    [Cached]
    public partial class DrawableCalibreRuleset : DrawableRuleset<CalibreHitObject>
    {
        public DrawableCalibreRuleset(CalibreRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Overlays.Add(new ProfileRecorder());
        }

        protected override Playfield CreatePlayfield() => new CalibrePlayfield();

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() => new CalibrePlayfieldAdjustmentContainer();

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new CalibreFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) => new CalibreReplayRecorder(score);

        // Les drawables sont fournis par les pools enregistrés dans CalibrePlayfield.
        public override DrawableHitObject<CalibreHitObject>? CreateDrawableRepresentation(CalibreHitObject h) => null;

        protected override PassThroughInputManager CreateInputManager() => new CalibreInputManager(Ruleset.RulesetInfo);
    }
}

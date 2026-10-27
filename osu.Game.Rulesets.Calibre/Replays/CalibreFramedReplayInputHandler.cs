// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.StateChanges;
using osu.Framework.Utils;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Calibre.Replays
{
    public class CalibreFramedReplayInputHandler : FramedReplayInputHandler<CalibreReplayFrame>
    {
        private readonly List<CalibreAction> noActions = new List<CalibreAction>();

        public CalibreFramedReplayInputHandler(Replay replay)
            : base(replay)
        {
        }

        protected override bool IsImportant(CalibreReplayFrame frame) => frame.Actions.Count > 0;

        protected override void CollectReplayInputs(List<IInput> inputs)
        {
            // Position interpolée linéairement entre les deux frames qui encadrent l'instant courant de l'horloge de jeu.
            var position = Interpolation.ValueAt(CurrentTime, StartFrame.Position, EndFrame.Position, StartFrame.Time, EndFrame.Time);

            inputs.Add(new MousePositionAbsoluteInput { Position = GamefieldToScreenSpace(position) });
            inputs.Add(new ReplayState<CalibreAction> { PressedActions = CurrentFrame?.Actions ?? noActions });
        }
    }
}

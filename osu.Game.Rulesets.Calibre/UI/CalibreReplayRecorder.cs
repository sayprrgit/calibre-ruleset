// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Calibre.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Enregistre les entrées du joueur pendant la partie dans le replay du score en cours.
    /// Ce replay reste en mémoire (lazer ne le persiste pas pour un ruleset custom) et sert aux analyses post-partie.
    /// </summary>
    public partial class CalibreReplayRecorder : ReplayRecorder<CalibreAction>
    {
        public CalibreReplayRecorder(Score score)
            : base(score)
        {
        }

        protected override ReplayFrame HandleFrame(Vector2 mousePosition, List<CalibreAction> actions, ReplayFrame previousFrame)
            => new CalibreReplayFrame(Time.Current, mousePosition, actions.ToArray());
    }
}

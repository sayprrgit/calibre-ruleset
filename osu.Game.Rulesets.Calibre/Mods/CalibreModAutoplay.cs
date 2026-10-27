// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Replays;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Calibre.Mods
{
    public class CalibreModAutoplay : ModAutoplay
    {
        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new CalibreAutoGenerator(beatmap).Generate(), new ModCreatedUser { Username = "Calibre Auto" });
    }
}

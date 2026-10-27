// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Calibre.Replays
{
    public class CalibreAutoGenerator : AutoGenerator<CalibreReplayFrame>
    {
        public new Beatmap<CalibreHitObject> Beatmap => (Beatmap<CalibreHitObject>)base.Beatmap;

        public CalibreAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
        }

        protected override void GenerateFrames()
        {
            Frames.Add(new CalibreReplayFrame());

            foreach (CalibreHitObject hitObject in Beatmap.HitObjects)
            {
                Frames.Add(new CalibreReplayFrame
                {
                    Time = hitObject.StartTime,
                    Position = hitObject.Position,
                    // todo: add required inputs and extra frames.
                });
            }
        }
    }
}

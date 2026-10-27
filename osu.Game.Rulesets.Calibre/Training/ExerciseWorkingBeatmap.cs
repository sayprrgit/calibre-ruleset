// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Beatmap d'exercice générée en mémoire : pas de fond, pas de skin, et pour piste un métronome généré par code.
    /// </summary>
    public class ExerciseWorkingBeatmap : WorkingBeatmap
    {
        private readonly IBeatmap beatmap;
        private readonly Func<Track> createTrack;

        public ExerciseWorkingBeatmap(IBeatmap beatmap, Func<Track> createTrack, AudioManager audio)
            : base(beatmap.BeatmapInfo, audio)
        {
            this.beatmap = beatmap;
            this.createTrack = createTrack;
        }

        protected override IBeatmap GetBeatmap() => beatmap;

        public override Texture GetBackground() => null!;

        protected override Track GetBeatmapTrack() => createTrack();

        protected override ISkin GetSkin() => null!;

        public override Stream GetStream(string storagePath) => null!;
    }
}

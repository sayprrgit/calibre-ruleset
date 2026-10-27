// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Timing;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Audio
{
    /// <summary>
    /// Fournit l'horloge de référence d'un test de métronome.
    /// </summary>
    public interface IMetronomeSource
    {
        /// <summary>
        /// Démarre un métronome et renvoie l'horloge sur laquelle dater les frappes.
        /// </summary>
        /// <param name="schedule">Calendrier des temps à jouer.</param>
        /// <param name="audible">Faux pour le métronome visuel seul : la piste tourne, mais en silence.</param>
        IClock Start(MetronomeSchedule schedule, bool audible);

        void Stop();
    }

    /// <summary>
    /// Métronome joué par une piste audio générée : l'horloge du test est la position de lecture de cette piste,
    /// comme l'horloge de jeu de lazer suit la piste de la beatmap.
    /// </summary>
    public partial class TrackMetronomeSource : Component, IMetronomeSource
    {
        [Resolved]
        private AudioManager audio { get; set; } = null!;

        private readonly GeneratedAudioStore store = new GeneratedAudioStore();

        private ITrackStore trackStore = null!;
        private Track? track;
        private string? trackName;
        private int generatedCount;

        [BackgroundDependencyLoader]
        private void load()
        {
            trackStore = audio.GetTrackStore(store);
        }

        public IClock Start(MetronomeSchedule schedule, bool audible)
        {
            Stop();

            trackName = $"calibre-metronome-{++generatedCount}.wav";
            store.Add(trackName, MetronomeWaveGenerator.Generate(schedule));

            track = trackStore.Get(trackName);
            track.Volume.Value = audible ? 1 : 0;
            track.Start();

            return track;
        }

        public void Stop()
        {
            if (track != null)
            {
                track.Stop();
                track.Dispose();
                track = null;
            }

            if (trackName != null)
            {
                store.Remove(trackName);
                trackName = null;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            Stop();
            trackStore?.Dispose();
            store.Dispose();

            base.Dispose(isDisposing);
        }
    }
}

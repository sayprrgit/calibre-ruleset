// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics
{
    /// <summary>
    /// Charge la piste WAV générée par le gestionnaire audio du jeu et vérifie que sa position de lecture sert d'horloge.
    /// </summary>
    public partial class TestSceneTrackMetronomeSource : OsuTestScene
    {
        private static readonly MetronomeSchedule schedule = new MetronomeSchedule(200, 1, 4);

        private TrackMetronomeSource source = null!;
        private IClock clock = null!;

        [Test]
        public void TestGeneratedTrackDrivesTheClock()
        {
            AddStep("charger la source", () => Child = source = new TrackMetronomeSource());
            AddStep("démarrer en silence", () => clock = source.Start(schedule, false));

            AddAssert("piste de la durée du calendrier", () => ((Track)clock).Length, () => Is.EqualTo(schedule.TrackLength).Within(5));
            AddAssert("volume coupé", () => ((Track)clock).Volume.Value, () => Is.Zero);
            AddUntilStep("l'horloge avance avec la lecture", () => clock.CurrentTime, () => Is.GreaterThan(300));

            AddStep("arrêter", () => source.Stop());
            AddAssert("piste libérée", () => ((Track)clock).IsDisposed, () => Is.True);
        }
    }
}

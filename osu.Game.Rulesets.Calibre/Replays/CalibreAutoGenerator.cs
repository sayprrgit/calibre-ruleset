// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Replays
{
    /// <summary>
    /// Génère un replay parfait : le curseur rejoint chaque cercle et frappe exactement à son instant.
    /// </summary>
    public class CalibreAutoGenerator : AutoGenerator<CalibreReplayFrame>
    {
        /// <summary>
        /// Durée maximale du déplacement vers un cercle. Le déplacement commence au plus tôt après le relâchement précédent.
        /// </summary>
        public const double MAX_MOVE_DURATION = 250;

        /// <summary>
        /// En dessous de cet intervalle entre deux frappes, l'autoplay alterne K1 / K2 comme un joueur en stream.
        /// </summary>
        public const double ALTERNATE_THRESHOLD = 225;

        public new Beatmap<CalibreHitObject> Beatmap => (Beatmap<CalibreHitObject>)base.Beatmap;

        public CalibreAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
        }

        protected override void GenerateFrames()
        {
            var hitObjects = Beatmap.HitObjects;

            if (hitObjects.Count == 0)
                return;

            Vector2 position = CalibrePlayfield.BASE_SIZE / 2;
            double lastFrameTime = hitObjects[0].StartTime - 1000;
            double lastHitTime = double.NegativeInfinity;
            var lastAction = CalibreAction.Key2;

            Frames.Add(new CalibreReplayFrame(lastFrameTime, position));

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var hitObject = hitObjects[i];

                // Deux objets simultanés : on décale d'1 ms pour garder des frames strictement ordonnées (reste Great).
                double hitTime = Math.Max(hitObject.StartTime, lastFrameTime + 1);

                double moveStart = Math.Max(lastFrameTime, hitTime - MAX_MOVE_DURATION);
                if (moveStart > lastFrameTime)
                    Frames.Add(new CalibreReplayFrame(moveStart, position));

                var action = hitTime - lastHitTime < ALTERNATE_THRESHOLD && lastAction == CalibreAction.Key1
                    ? CalibreAction.Key2
                    : CalibreAction.Key1;

                position = hitObject.Position;
                Frames.Add(new CalibreReplayFrame(hitTime, position, action));

                // Relâchement avant la frappe suivante (au plus KEY_UP_DELAY après), sinon la touche resterait enfoncée.
                double releaseDelay = KEY_UP_DELAY;
                if (i + 1 < hitObjects.Count)
                    releaseDelay = Math.Min(releaseDelay, (hitObjects[i + 1].StartTime - hitTime) / 2);

                lastFrameTime = hitTime + Math.Max(releaseDelay, 0.5);
                Frames.Add(new CalibreReplayFrame(lastFrameTime, position));

                lastHitTime = hitTime;
                lastAction = action;
            }
        }
    }
}

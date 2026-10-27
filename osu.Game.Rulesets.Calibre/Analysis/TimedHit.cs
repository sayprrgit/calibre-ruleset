// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Analysis
{
    /// <summary>
    /// Frappe chronométrée (cercle ou tête de slider) : son jugement et la géométrie de son objet.
    /// Les temps sont en millisecondes réelles (divisés par la vitesse de jeu), comme l'UR de lazer.
    /// </summary>
    public readonly record struct TimedHit(
        double Time,
        double Offset,
        HitResult Result,
        Vector2 Target,
        Vector2? Cursor,
        float Radius,
        double EndTime,
        Vector2 EndPosition,
        bool IsSlider)
    {
        public bool IsHit => Result.IsHit();

        /// <summary>
        /// Distance entre le curseur et le centre de la cible, en pixels de l'aire de jeu. Nulle pour un miss sans frappe.
        /// </summary>
        public float? PositionError => Cursor is Vector2 cursor ? Vector2.Distance(cursor, Target) : null;
    }

    public static class TimedHits
    {
        /// <summary>
        /// Apparie chaque cercle et slider de la beatmap jouable au <see cref="HitEvent"/> de son timing, dans l'ordre de la map.
        /// </summary>
        /// <remarks>
        /// L'écran de résultats recrée la beatmap jouable : ses objets ne sont pas ceux de la partie. On les apparie donc
        /// par instant et position, ce qui donne aussi la fin et la position de sortie des sliders.
        /// </remarks>
        public static IReadOnlyList<TimedHit> From(IBeatmap playableBeatmap, IEnumerable<HitEvent> hitEvents)
        {
            var events = new Dictionary<(double, float, float), HitEvent>();

            foreach (var e in hitEvents)
            {
                if (e.HitObject is HitCircle circle)
                    events.TryAdd((circle.StartTime, circle.X, circle.Y), e);
            }

            var hits = new List<TimedHit>(events.Count);

            foreach (var hitObject in playableBeatmap.HitObjects.OfType<CalibreHitObject>())
            {
                if (hitObject is Spinner || !events.TryGetValue((hitObject.StartTime, hitObject.X, hitObject.Y), out var e))
                    continue;

                double rate = e.GameplayRate ?? 1;
                var slider = hitObject as Slider;

                hits.Add(new TimedHit(
                    hitObject.StartTime / rate,
                    e.TimeOffset / rate,
                    e.Result,
                    hitObject.Position,
                    e.Position,
                    hitObject.Radius,
                    hitObject.GetEndTime() / rate,
                    slider?.EndPosition ?? hitObject.Position,
                    slider != null));
            }

            return hits;
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Tentative sur une cible. <see cref="Press"/> et <see cref="ReactionTime"/> sont nuls si la cible a expiré sans frappe.
    /// </summary>
    public readonly record struct AimAttempt(int Zone, Vector2 Target, Vector2? Press, double? ReactionTime, bool Hit)
    {
        public float? Distance => Press is Vector2 press ? Vector2.Distance(press, Target) : null;
    }

    public class ZoneStats
    {
        public int Zone { get; }

        public int Attempts { get; }

        public int Hits { get; }

        /// <summary>
        /// Distance moyenne entre la frappe et le centre de la cible, en pixels de l'aire de jeu (frappes seulement).
        /// </summary>
        public double? MeanDistance { get; }

        /// <summary>
        /// Temps moyen entre l'apparition de la cible et la frappe, en ms (frappes seulement).
        /// </summary>
        public double? MeanReactionTime { get; }

        public double MissRate => Attempts == 0 ? 0 : 1 - (double)Hits / Attempts;

        /// <summary>
        /// Indice d'erreur de la zone : taux de miss + distance moyenne exprimée en rayons de cible.
        /// Les deux termes valent environ 1 pour une zone systématiquement ratée ; 0 = toutes les frappes au centre.
        /// </summary>
        public double ErrorIndex { get; }

        public ZoneStats(int zone, IReadOnlyList<AimAttempt> attempts, float targetRadius)
        {
            Zone = zone;
            Attempts = attempts.Count;
            Hits = attempts.Count(a => a.Hit);

            var pressed = attempts.Where(a => a.Press != null).ToArray();

            if (pressed.Length > 0)
            {
                MeanDistance = pressed.Average(a => a.Distance!.Value);
                MeanReactionTime = pressed.Average(a => a.ReactionTime!.Value);
            }

            // Une zone sans aucune frappe compte comme si chaque frappe avait atterri au bord de la cible.
            ErrorIndex = MissRate + (MeanDistance ?? targetRadius) / targetRadius;
        }
    }

    public class AimGridResult
    {
        /// <summary>
        /// La zone la plus faible n'est signalée que si son indice d'erreur dépasse d'au moins 25 % la moyenne des autres zones.
        /// </summary>
        public const double WEAK_ZONE_MARGIN = 1.25;

        public int Seed { get; }

        public float TargetRadius { get; }

        public IReadOnlyList<AimAttempt> Attempts { get; }

        /// <summary>
        /// Statistiques des 9 zones, indexées par numéro de zone.
        /// </summary>
        public IReadOnlyList<ZoneStats> Zones { get; }

        /// <summary>
        /// Zone dont l'indice d'erreur est le plus élevé, qu'elle soit nettement plus faible ou non.
        /// </summary>
        public int? WorstZone { get; }

        /// <summary>
        /// Zone nettement plus faible que les autres (voir <see cref="WEAK_ZONE_MARGIN"/>), sinon nulle.
        /// </summary>
        public int? WeakZone { get; }

        public double HitRate => Attempts.Count == 0 ? 0 : (double)Attempts.Count(a => a.Hit) / Attempts.Count;

        public AimGridResult(int seed, float targetRadius, IReadOnlyList<AimAttempt> attempts)
        {
            Seed = seed;
            TargetRadius = targetRadius;
            Attempts = attempts;

            Zones = Enumerable.Range(0, AimGrid.ZONE_COUNT)
                              .Select(zone => new ZoneStats(zone, attempts.Where(a => a.Zone == zone).ToArray(), targetRadius))
                              .ToArray();

            var tested = Zones.Where(z => z.Attempts > 0).ToArray();

            if (tested.Length == 0)
                return;

            var worst = tested.OrderByDescending(z => z.ErrorIndex).First();
            WorstZone = worst.Zone;

            if (tested.Length < 2)
                return;

            double othersMean = tested.Where(z => z != worst).Average(z => z.ErrorIndex);

            if (worst.ErrorIndex > 0 && worst.ErrorIndex >= othersMean * WEAK_ZONE_MARGIN)
                WeakZone = worst.Zone;
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Logic
{
    /// <summary>
    /// Découpage de l'aire de jeu (512x384) en 3x3 zones, numérotées de 0 (haut gauche) à 8 (bas droite).
    /// </summary>
    public static class AimGrid
    {
        public const int COLUMNS = 3;
        public const int ROWS = 3;
        public const int ZONE_COUNT = COLUMNS * ROWS;

        public static Vector2 ZoneSize => new Vector2(CalibrePlayfield.BASE_SIZE.X / COLUMNS, CalibrePlayfield.BASE_SIZE.Y / ROWS);

        public static RectangleF ZoneBounds(int zone)
        {
            var size = ZoneSize;
            return new RectangleF(zone % COLUMNS * size.X, zone / COLUMNS * size.Y, size.X, size.Y);
        }

        public static Vector2 ZoneCentre(int zone) => ZoneBounds(zone).Centre;

        private static readonly string[] zone_names =
        {
            "haut gauche", "haut", "haut droite",
            "gauche", "centre", "droite",
            "bas gauche", "bas", "bas droite",
        };

        public static string ZoneName(int zone) => zone_names[zone];
    }

    /// <summary>
    /// Cible du test d'aim : sa zone, sa position dans l'aire de jeu et le délai avant son apparition.
    /// </summary>
    public readonly record struct AimTarget(int Zone, Vector2 Position, double Delay);

    public static class AimTargetSequence
    {
        public const double MINIMUM_DELAY = 250;
        public const double MAXIMUM_DELAY = 650;

        /// <summary>
        /// Part de la demi-zone utilisable pour décaler la cible autour du centre de sa zone.
        /// </summary>
        private const float jitter_portion = 0.6f;

        /// <summary>
        /// Génère <paramref name="rounds"/> tours de 9 cibles. Chaque tour visite chaque zone une fois, dans un ordre mélangé.
        /// </summary>
        /// <remarks>
        /// Déterministe à graine égale : deux périphériques testés avec la même graine voient exactement les mêmes cibles,
        /// au même endroit et au même rythme, ce qui rend leurs résultats comparables.
        /// Le délai aléatoire entre deux cibles empêche d'anticiper l'apparition.
        /// </remarks>
        public static IReadOnlyList<AimTarget> Generate(int seed, int rounds, float targetRadius)
        {
            var random = new Random(seed);
            var targets = new List<AimTarget>(rounds * AimGrid.ZONE_COUNT);
            int[] order = new int[AimGrid.ZONE_COUNT];

            var halfZone = AimGrid.ZoneSize / 2;
            var maximumJitter = Vector2.ComponentMax(Vector2.Zero, (halfZone - new Vector2(targetRadius)) * jitter_portion);

            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < order.Length; i++)
                    order[i] = i;

                // Mélange de Fisher-Yates.
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                foreach (int zone in order)
                {
                    var jitter = new Vector2(
                        (float)(random.NextDouble() * 2 - 1) * maximumJitter.X,
                        (float)(random.NextDouble() * 2 - 1) * maximumJitter.Y);

                    double delay = MINIMUM_DELAY + random.NextDouble() * (MAXIMUM_DELAY - MINIMUM_DELAY);

                    targets.Add(new AimTarget(zone, AimGrid.ZoneCentre(zone) + jitter, delay));
                }
            }

            return targets;
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Calibre.Scoring
{
    /// <summary>
    /// Fenêtres de jugement identiques à osu!standard, afin que l'UR et les écarts mesurés par Calibre
    /// restent comparables à ceux du jeu.
    /// </summary>
    public class CalibreHitWindows : HitWindows
    {
        // Demi-largeur de chaque fenêtre en ms, interpolée linéairement selon l'OD (valeurs à OD 0 / 5 / 10).
        public static readonly DifficultyRange GREAT_WINDOW_RANGE = new DifficultyRange(80, 50, 20);
        public static readonly DifficultyRange OK_WINDOW_RANGE = new DifficultyRange(140, 100, 60);
        public static readonly DifficultyRange MEH_WINDOW_RANGE = new DifficultyRange(200, 150, 100);

        /// <summary>
        /// Fenêtre de miss fixe : en deçà, une frappe trop précoce est ignorée (et secoue le cercle) au lieu de compter comme miss.
        /// </summary>
        public const double MISS_WINDOW = 400;

        private double great;
        private double ok;
        private double meh;

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override void SetDifficulty(double difficulty)
        {
            // La valeur interpolée est tronquée à la milliseconde entière (spécification historique), puis réduite de 0,5 ms :
            // HitWindows compare |offset| <= fenêtre, et les replays stockent des temps entiers. Avec une borne en x,5,
            // un écart de 49 ms est Great et un écart de 50 ms ne l'est plus, sans ambiguïté d'arrondi.
            great = Math.Floor(IBeatmapDifficultyInfo.DifficultyRange(difficulty, GREAT_WINDOW_RANGE)) - 0.5;
            ok = Math.Floor(IBeatmapDifficultyInfo.DifficultyRange(difficulty, OK_WINDOW_RANGE)) - 0.5;
            meh = Math.Floor(IBeatmapDifficultyInfo.DifficultyRange(difficulty, MEH_WINDOW_RANGE)) - 0.5;
        }

        public override double WindowFor(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                    return great;

                case HitResult.Ok:
                    return ok;

                case HitResult.Meh:
                    return meh;

                case HitResult.Miss:
                    return MISS_WINDOW;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }
    }
}

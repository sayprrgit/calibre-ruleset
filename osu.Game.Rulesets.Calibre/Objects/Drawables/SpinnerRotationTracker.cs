// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Calibre.Objects.Drawables
{
    /// <summary>
    /// Cumule la rotation du curseur autour du centre d'un spinner, à partir de l'angle relevé à chaque frame.
    /// Logique pure (sans drawable) : testée unitairement et appelée à chaque frame sans allocation.
    /// </summary>
    public class SpinnerRotationTracker
    {
        /// <summary>
        /// Vitesse maximale comptabilisée : 477 tours par minute, le plafond d'osu!stable.
        /// </summary>
        public const double MAX_ROTATIONS_PER_SECOND = 477.0 / 60;

        private const double full_turn = 2 * Math.PI;

        private double? lastAngle;

        /// <summary>
        /// Rotation cumulée en radians, quel que soit le sens : changer de sens ne retire rien.
        /// </summary>
        public double TotalRotation { get; private set; }

        /// <summary>
        /// Rotation signée en radians (positive dans le sens horaire de l'écran), pour l'affichage.
        /// </summary>
        public double SignedRotation { get; private set; }

        public double CompletedSpins => TotalRotation / full_turn;

        public void Reset()
        {
            lastAngle = null;
            TotalRotation = 0;
            SignedRotation = 0;
        }

        /// <param name="angle">Angle du curseur autour du centre, en radians.</param>
        /// <param name="elapsedMilliseconds">Temps de jeu écoulé depuis la frame précédente (négatif lors d'un retour en arrière).</param>
        /// <param name="pressed">Vrai si une entrée de frappe est maintenue : sans elle, l'angle est suivi mais rien n'est compté.</param>
        public void AddSample(double angle, double elapsedMilliseconds, bool pressed)
        {
            if (lastAngle is double previous && pressed && elapsedMilliseconds > 0)
            {
                double delta = angle - previous;

                // Ramène l'écart dans ]-π, π] : passer de -179° à 179° est un pas de 2°, pas un demi-tour.
                if (delta > Math.PI)
                    delta -= full_turn;
                else if (delta <= -Math.PI)
                    delta += full_turn;

                double maximumDelta = MAX_ROTATIONS_PER_SECOND * full_turn * elapsedMilliseconds / 1000;
                delta = Math.Clamp(delta, -maximumDelta, maximumDelta);

                TotalRotation += Math.Abs(delta);
                SignedRotation += delta;
            }

            lastAngle = angle;
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
// Logique adaptée de StartTimeOrderedHitPolicy d'osu! (https://github.com/ppy/osu), © ppy Pty Ltd, licence MIT.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Calibre.Objects.Drawables;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// « Note lock » : les cercles et têtes de slider doivent être frappés dans l'ordre chronologique.
    /// En frapper un fait manquer tous ceux qui le précèdent et ne sont pas encore jugés. Les spinners ne bloquent rien.
    /// </summary>
    /// <remarks>
    /// Appelée uniquement sur une frappe ou un jugement, jamais à chaque frame.
    /// </remarks>
    public class StartTimeOrderedHitPolicy
    {
        public HitObjectContainer? HitObjectContainer { get; set; }

        public ClickAction CheckHittable(DrawableHitObject hitObject, double time, HitResult result)
        {
            DrawableHitObject? blockingObject = null;

            foreach (var obj in aliveObjects())
            {
                if (obj.HitObject.StartTime >= hitObject.HitObject.StartTime)
                    break;

                if (canBlockFutureHits(obj))
                    blockingObject = obj;
            }

            // Une frappe est refusée si le dernier cercle bloquant n'est pas encore jugé et que son instant n'est pas passé.
            // Les frappes simultanées (même instant) restent autorisées pour les maps qui en contiennent.
            if (blockingObject != null && !blockingObject.Judged && time < blockingObject.HitObject.StartTime)
                return ClickAction.Shake;

            // Frappe beaucoup trop tôt : hors de la fenêtre de miss.
            if (result == HitResult.None)
                return ClickAction.Shake;

            return ClickAction.Hit;
        }

        public void HandleHit(DrawableHitObject hitObject)
        {
            if (!canBlockFutureHits(hitObject))
                return;

            if (CheckHittable(hitObject, hitObject.HitObject.StartTime + hitObject.Result.TimeOffset, hitObject.Result.Type) != ClickAction.Hit)
                throw new InvalidOperationException($"{hitObject} a été jugé avant d'être frappable.");

            foreach (var obj in aliveObjects())
            {
                if (obj.HitObject.StartTime >= hitObject.HitObject.StartTime)
                    break;

                if (!obj.Judged && canBlockFutureHits(obj))
                    ((DrawableCalibreHitObject)obj).MissForcefully();
            }
        }

        private static bool canBlockFutureHits(DrawableHitObject hitObject) => hitObject is DrawableHitCircle;

        /// <summary>
        /// Objets vivants dans l'ordre chronologique, chaque slider étant représenté par sa tête (seule partie frappable).
        /// </summary>
        private IEnumerable<DrawableHitObject> aliveObjects()
        {
            if (HitObjectContainer == null)
                throw new InvalidOperationException($"{nameof(HitObjectContainer)} doit être défini avant utilisation.");

            foreach (var obj in HitObjectContainer.AliveObjects)
            {
                switch (obj)
                {
                    case DrawableSlider slider:
                        if (slider.HeadCircle != null)
                            yield return slider.HeadCircle;

                        break;

                    default:
                        yield return obj;

                        break;
                }
            }
        }
    }
}

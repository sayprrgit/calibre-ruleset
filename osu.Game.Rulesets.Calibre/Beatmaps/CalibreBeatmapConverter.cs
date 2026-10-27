// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Beatmaps
{
    /// <summary>
    /// Convertit une beatmap osu!standard en objets Calibre. La structure suit celle du convertisseur officiel
    /// d'osu! (licence MIT), réécrit ici pour ne pas dépendre de l'assembly du ruleset osu!.
    /// </summary>
    public class CalibreBeatmapConverter : BeatmapConverter<CalibreHitObject>
    {
        public CalibreBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
        }

        public override bool CanConvert() => Beatmap.HitObjects.All(h => h is IHasPosition);

        protected override IEnumerable<CalibreHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            var positionData = original as IHasPosition;
            var comboData = original as IHasCombo;

            switch (original)
            {
                // J1 : un slider est réduit à un cercle sur sa tête. Les sliders complets arrivent au J2.
                case IHasPathWithRepeats:
                    break;

                // J1 : les spinners ne sont pas encore pris en charge (J2).
                case IHasDuration:
                    yield break;
            }

            yield return new HitCircle
            {
                StartTime = original.StartTime,
                Samples = original.Samples,
                Position = positionData?.Position ?? Vector2.Zero,
                NewCombo = comboData?.NewCombo ?? false,
                ComboOffset = comboData?.ComboOffset ?? 0,
            };
        }
    }
}

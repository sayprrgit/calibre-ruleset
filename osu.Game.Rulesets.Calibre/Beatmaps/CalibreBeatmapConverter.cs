// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.UI;
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
                case IHasPathWithRepeats curveData:
                    yield return new Slider
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        Path = curveData.Path,
                        NodeSamples = curveData.NodeSamples,
                        RepeatCount = curveData.RepeatCount,
                        Position = positionData?.Position ?? Vector2.Zero,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                        TickDistanceMultiplier = legacyTickDistanceMultiplier(original, beatmap),
                        GenerateTicks = (original as IHasGenerateTicks)?.GenerateTicks ?? true,
                        SliderVelocityMultiplier = (original as IHasSliderVelocity)?.SliderVelocityMultiplier ?? 1,
                    };

                    yield break;

                case IHasDuration endTimeData:
                    yield return new Spinner
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        EndTime = endTimeData.EndTime,
                        Position = positionData?.Position ?? CalibrePlayfield.BASE_SIZE / 2,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                    };

                    yield break;

                default:
                    yield return new HitCircle
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        Position = positionData?.Position ?? Vector2.Zero,
                        NewCombo = comboData?.NewCombo ?? false,
                        ComboOffset = comboData?.ComboOffset ?? 0,
                    };

                    yield break;
            }
        }

        /// <summary>
        /// Avant le format v8, la vitesse locale ne modifiait pas le nombre de ticks générés sur une même durée :
        /// on compense par l'inverse de cette vitesse, comme osu!.
        /// </summary>
        private static double legacyTickDistanceMultiplier(HitObject original, IBeatmap beatmap)
        {
            if (beatmap.BeatmapVersion >= 8 ||beatmap.ControlPointInfo is not LegacyControlPointInfo legacyControlPoints)
                return 1;

            return 1 / legacyControlPoints.DifficultyPointAt(original.StartTime).SliderVelocity;
        }
    }
}

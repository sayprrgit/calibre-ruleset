// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Diagnostics.Charts;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Base des vues de résultats : titre, puis sections empilées verticalement.
    /// </summary>
    public abstract partial class ResultView : FillFlowContainer
    {
        protected ResultView(string title)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 12);

            Add(new OsuSpriteText
            {
                Text = title,
                Font = OsuFont.Default.With(size: 28, weight: FontWeight.Bold),
            });
        }

        protected static OsuSpriteText Heading(string text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.Default.With(size: 20, weight: FontWeight.SemiBold),
            Colour = CalibreColours.ACCENT,
            Margin = new MarginPadding { Top = 8 },
        };

        protected static OsuTextFlowContainer Paragraph(string text, float size = 16) => new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: size))
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Text = text,
        };
    }

    public partial class LatencyResultView : ResultView
    {
        public LatencyResultView(LatencyResult result)
            : base("Latence perçue : résultats")
        {
            addPhase("Métronome audio", result.Audio, CalibreColours.ACCENT);
            addPhase("Métronome visuel", result.Visual, CalibreColours.GREAT);

            Add(Heading("Recommandations"));

            Add(Paragraph(result.RecommendedAudioOffset is double audio
                ? $"Décalage audio à régler dans lazer (Réglages > Audio > Décalage audio) : {DiagnosticFormat.SignedMs(audio)}."
                : $"Pas de recommandation audio : moins de {LatencyResult.MINIMUM_TAPS} temps frappés."));

            Add(Paragraph(result.RecommendedVisualOffset is double visual
                ? $"Offset visuel recommandé, si tu joues à l'image plutôt qu'au son : {DiagnosticFormat.SignedMs(visual)}."
                : $"Pas de recommandation visuelle : moins de {LatencyResult.MINIMUM_TAPS} temps frappés."));

            if (result.AudioVisualGap is double gap)
                Add(Paragraph($"Ta réponse à l'image est décalée de {DiagnosticFormat.SignedMs(gap)} par rapport à ta réponse au son."));
        }

        private void addPhase(string title, LatencyPhaseResult phase, Colour4 colour)
        {
            var statistics = phase.Statistics;

            Add(Heading(title));
            Add(Paragraph($"Écart moyen {DiagnosticFormat.SignedMs(statistics.Mean)} · écart-type {DiagnosticFormat.Ms(statistics.StandardDeviation)} · "
                          + $"{statistics.Count} temps frappés sur {phase.Schedule.MeasuredBeats}"));
            Add(new OffsetHistogram(phase.Matched.Taps.Select(t => t.Offset).ToArray(), statistics.Mean, colour));
        }
    }

    public partial class KeyStabilityResultView : ResultView
    {
        public KeyStabilityResultView(KeyStabilityResult result)
            : base("Stabilité par touche : résultats")
        {
            if (result.Keys.Count == 0)
            {
                Add(Paragraph("Aucune frappe mesurée."));
                return;
            }

            Add(Heading("Par touche"));

            foreach (var key in result.Keys)
            {
                var statistics = key.Statistics;

                Add(new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 16))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = $"{key.Action.ShortName()} : UR {DiagnosticFormat.Number(statistics.UnstableRate)} · biais {DiagnosticFormat.SignedMs(statistics.Mean, "0.0")} "
                           + $"· dérive {DiagnosticFormat.SignedMs(key.DriftPerMinute, "0.0")}/min · {statistics.Count} frappes",
                    Colour = CalibreColours.ForAction(key.Action),
                });
            }

            Add(new OffsetTimelineChart(result.Schedule, result.Keys));

            Add(Heading("Comparaison"));

            if (result.Keyboard == null && result.Mouse == null)
                Add(Paragraph($"Pas de comparaison : il faut au moins {KeyStabilityResult.MINIMUM_TAPS_PER_KEY} frappes sur chacune des deux touches d'une paire (K1/K2 ou M1/M2)."));

            if (result.Keyboard != null)
                addComparison(result.Keyboard);

            if (result.Mouse != null)
                addComparison(result.Mouse);
        }

        private void addComparison(KeyComparison comparison)
        {
            string first = comparison.First.Action.ShortName();
            string second = comparison.Second.Action.ShortName();

            Add(Paragraph(comparison.LaggingKey is CalibreAction lagging
                ? $"{lagging.ShortName()} est traînante : {DiagnosticFormat.Ms(Math.Abs(comparison.MeanDifference), "0.0")} de retard en moyenne "
                  + $"sur l'autre touche ({DiagnosticFormat.PValue(comparison.MeanTest.PValue)}, test de Welch)."
                : $"{first} et {second} : pas de décalage significatif entre les deux touches "
                  + $"({DiagnosticFormat.SignedMs(comparison.MeanDifference, "0.0")}, {DiagnosticFormat.PValue(comparison.MeanTest.PValue)})."));

            Add(Paragraph(comparison.LessStableKey is CalibreAction lessStable
                ? $"{lessStable.ShortName()} est moins régulière : UR {DiagnosticFormat.Number(Math.Max(comparison.UnstableRateRatio, 1 / comparison.UnstableRateRatio), "0.00")} fois plus élevé "
                  + $"({DiagnosticFormat.PValue(comparison.VarianceTest.PValue)}, test F)."
                : $"{first} et {second} : régularité comparable ({DiagnosticFormat.PValue(comparison.VarianceTest.PValue)})."));
        }
    }

    public partial class AimGridResultView : ResultView
    {
        public AimGridResultView(AimGridResult result)
            : base("Précision d'aim : résultats")
        {
            Add(Paragraph($"{result.Attempts.Count} cibles · {DiagnosticFormat.Percent(result.HitRate)} touchées · rayon {DiagnosticFormat.Number(result.TargetRadius, "0.0")} px · graine {result.Seed}"));

            Add(new AimGridChart(result));

            if (result.WeakZone is int weak)
            {
                var zone = result.Zones[weak];

                Add(Paragraph($"Zone faible : {AimGrid.ZoneName(weak)} (miss {DiagnosticFormat.Percent(zone.MissRate)}, {DiagnosticFormat.Number(zone.MeanDistance)} px du centre en moyenne). "
                              + "Cause possible : bord du tapis, limite de la zone de la tablette, ou sensibilité à revoir."));
            }
            else
            {
                Add(Paragraph("Aucune zone nettement plus faible que les autres."));
            }
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Statistics
{
    /// <summary>
    /// Tableau par type de pattern : nombre d'objets, précision, UR et écart moyen.
    /// </summary>
    public partial class PatternTable : FillFlowContainer
    {
        private static readonly float[] column_widths = { 0.4f, 0.12f, 0.16f, 0.14f, 0.18f };

        public PatternTable(IReadOnlyList<PatternStats> patterns)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 6);

            Add(row(true, "Pattern", "Objets", "Précision", "UR", "Écart moyen"));

            foreach (var pattern in patterns)
            {
                Add(row(false,
                    PatternClassifier.DisplayName(pattern.Type),
                    pattern.Count.ToString(),
                    DiagnosticFormat.Percent(pattern.Accuracy),
                    DiagnosticFormat.Number(pattern.Timing.UnstableRate),
                    DiagnosticFormat.SignedMs(pattern.Timing.Mean, "0.0")));
            }
        }

        private static Drawable row(bool header, params string[] cells)
        {
            var row = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            float x = 0;

            for (int i = 0; i < cells.Length; i++)
            {
                row.Add(new OsuSpriteText
                {
                    RelativePositionAxes = Axes.X,
                    X = x,
                    Text = cells[i],
                    Font = OsuFont.Default.With(size: 14, weight: header ? FontWeight.Bold : FontWeight.Regular),
                    Colour = header ? CalibreColours.ACCENT : Colour4.White,
                });

                x += column_widths[i];
            }

            return row;
        }
    }
}

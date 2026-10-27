// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Statistics
{
    /// <summary>
    /// Heatmap des erreurs de position superposée à l'aire de jeu (4:3) : chaque case est teintée selon l'écart moyen
    /// des frappes sur les cibles qu'elle contient (vert = au centre, rouge = au bord de la cible ou au-delà).
    /// Une case peu fournie est plus transparente.
    /// </summary>
    public partial class PositionHeatmapGraph : CompositeDrawable
    {
        public PositionHeatmapGraph(PositionHeatmap heatmap)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var field = new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                FillAspectRatio = 4f / 3,
                Masking = true,
                BorderThickness = 2,
                BorderColour = CalibreColours.PLAYFIELD_BORDER,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.White,
                    Alpha = 0.03f,
                },
            };

            int busiest = 1;

            foreach (int count in heatmap.Counts)
                busiest = Math.Max(busiest, count);

            for (int column = 0; column < PositionHeatmap.COLUMNS; column++)
            {
                for (int row = 0; row < PositionHeatmap.ROWS; row++)
                {
                    if (heatmap.MeanErrorAt(column, row) is not double error)
                        continue;

                    field.Add(new Box
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Both,
                        Position = new Vector2((float)column / PositionHeatmap.COLUMNS, (float)row / PositionHeatmap.ROWS),
                        Size = new Vector2(1f / PositionHeatmap.COLUMNS, 1f / PositionHeatmap.ROWS),
                        Colour = CalibreColours.ForError(error),
                        Alpha = 0.35f + 0.65f * heatmap.Counts[column, row] / busiest,
                    });
                }
            }

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 240,
                        Child = field,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Font = OsuFont.Default.With(size: 13),
                        Text = heatmap.Count == 0
                            ? "Aucune frappe positionnée."
                            : $"Écart moyen {DiagnosticFormat.Number(heatmap.MeanError, "0.0")} px · biais moyen "
                              + $"({DiagnosticFormat.Number(heatmap.MeanOffset.X, "+0.0;-0.0;0")}, {DiagnosticFormat.Number(heatmap.MeanOffset.Y, "+0.0;-0.0;0")}) px "
                              + $"sur {heatmap.Count} frappes",
                    },
                },
            };
        }
    }
}

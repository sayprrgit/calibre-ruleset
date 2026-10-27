// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Charts
{
    /// <summary>
    /// Histogramme des écarts de timing, centré sur 0 ms (en avance à gauche, en retard à droite), avec la moyenne marquée.
    /// Les écarts hors de l'échelle sont regroupés dans la barre extrême de leur côté.
    /// </summary>
    public partial class OffsetHistogram : CompositeDrawable
    {
        private const float label_height = 16;

        public OffsetHistogram(IReadOnlyList<double> offsets, double? mean, Colour4 colour, double range = 120, double binWidth = 10)
        {
            RelativeSizeAxes = Axes.X;
            Height = 110;

            int binCount = (int)Math.Round(2 * range / binWidth);
            int[] counts = new int[binCount];

            foreach (double offset in offsets)
                counts[Math.Clamp((int)Math.Floor((offset + range) / binWidth), 0, binCount - 1)]++;

            int maxCount = Math.Max(1, max(counts));

            var bars = new Container { RelativeSizeAxes = Axes.Both, Padding = new MarginPadding { Bottom = label_height } };

            bars.Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.White,
                Alpha = 0.04f,
            });

            for (int i = 0; i < binCount; i++)
            {
                if (counts[i] == 0)
                    continue;

                bars.Add(new Box
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Both,
                    X = (float)i / binCount,
                    Width = 1f / binCount * 0.85f,
                    Height = (float)counts[i] / maxCount,
                    Colour = colour,
                });
            }

            bars.Add(verticalLine(0.5f, Colour4.White.Opacity(0.5f)));

            if (mean is double m)
                bars.Add(verticalLine((float)Math.Clamp((m + range) / (2 * range), 0, 1), Colour4.White));

            InternalChildren = new Drawable[]
            {
                bars,
                label($"-{range} ms", Anchor.BottomLeft),
                label("0", Anchor.BottomCentre),
                label($"+{range} ms", Anchor.BottomRight),
            };
        }

        private static int max(int[] values)
        {
            int result = 0;

            foreach (int value in values)
                result = Math.Max(result, value);

            return result;
        }

        private static Box verticalLine(float x, Colour4 colour) => new Box
        {
            RelativePositionAxes = Axes.X,
            RelativeSizeAxes = Axes.Y,
            Origin = Anchor.TopCentre,
            X = x,
            Width = 2,
            Colour = colour,
        };

        private static OsuSpriteText label(string text, Anchor anchor) => new OsuSpriteText
        {
            Anchor = anchor,
            Origin = anchor,
            Text = text,
            Font = OsuFont.Default.With(size: 12),
            Colour = Colour4.White.Opacity(0.7f),
        };
    }
}

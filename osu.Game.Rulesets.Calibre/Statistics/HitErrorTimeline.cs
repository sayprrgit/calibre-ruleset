// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Statistics
{
    /// <summary>
    /// Erreur de timing de chaque objet au fil de la map (en haut = en retard), misses marqués en bas,
    /// et sections où l'UR monte surlignées en rouge.
    /// </summary>
    public partial class HitErrorTimeline : CompositeDrawable
    {
        private const float label_width = 60;

        public HitErrorTimeline(IReadOnlyList<TimedHit> hits, IReadOnlyList<UnstableSection> sections)
        {
            RelativeSizeAxes = Axes.X;
            Height = 200;

            if (hits.Count == 0)
                return;

            double start = hits[0].Time;
            double span = Math.Max(1, hits[^1].Time - start);
            double range = rangeFor(hits);

            var plot = new Container { RelativeSizeAxes = Axes.Both };

            plot.Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.White,
                Alpha = 0.04f,
            });

            foreach (var section in sections)
            {
                plot.Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.X,
                    X = (float)((section.StartTime - start) / span),
                    Width = (float)Math.Max(0.004, (section.EndTime - section.StartTime) / span),
                    Colour = CalibreColours.MISS,
                    Alpha = 0.18f,
                });
            }

            plot.Add(new Box
            {
                RelativeSizeAxes = Axes.X,
                RelativePositionAxes = Axes.Y,
                Origin = Anchor.CentreLeft,
                Y = 0.5f,
                Height = 1,
                Colour = Colour4.White.Opacity(0.5f),
            });

            // Des carrés plutôt que des cercles : une map longue compte des milliers de points, sans masquage à dessiner.
            foreach (var hit in hits)
            {
                float x = (float)((hit.Time - start) / span);

                plot.Add(hit.IsHit
                    ? new Box
                    {
                        RelativePositionAxes = Axes.Both,
                        Origin = Anchor.Centre,
                        Size = new Vector2(3),
                        X = x,
                        Y = (float)Math.Clamp(0.5 - hit.Offset / (2 * range), 0, 1),
                        Colour = CalibreColours.ForResult(hit.Result),
                    }
                    : new Box
                    {
                        RelativePositionAxes = Axes.X,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(2, 10),
                        X = x,
                        Colour = CalibreColours.MISS,
                    });
            }

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = label_width },
                    Child = plot,
                },
                label($"+{range} ms", Anchor.TopLeft),
                label("0", Anchor.CentreLeft),
                label($"-{range} ms", Anchor.BottomLeft),
            };
        }

        /// <summary>
        /// Échelle verticale : le plus grand écart de la partie arrondi aux 25 ms supérieures, entre 50 et 200 ms.
        /// </summary>
        private static double rangeFor(IReadOnlyList<TimedHit> hits)
        {
            double largest = 0;

            foreach (var hit in hits)
            {
                if (hit.IsHit)
                    largest = Math.Max(largest, Math.Abs(hit.Offset));
            }

            return Math.Clamp(Math.Ceiling(largest / 25) * 25, 50, 200);
        }

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

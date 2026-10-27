// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Charts
{
    /// <summary>
    /// Écart de chaque frappe au fil du test, une couleur par touche : on y lit le biais, la dispersion et la dérive.
    /// En haut = en retard, en bas = en avance.
    /// </summary>
    public partial class OffsetTimelineChart : CompositeDrawable
    {
        private const float label_width = 60;

        public OffsetTimelineChart(MetronomeSchedule schedule, IReadOnlyList<KeyStats> keys, double range = 100)
        {
            RelativeSizeAxes = Axes.X;
            Height = 160;

            double start = schedule.BeatTime(schedule.LeadInBeats);
            double span = Math.Max(1, schedule.BeatTime(schedule.TotalBeats - 1) - start);

            var points = new Container { RelativeSizeAxes = Axes.Both };

            points.Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.White,
                Alpha = 0.04f,
            });

            points.Add(new Box
            {
                RelativeSizeAxes = Axes.X,
                RelativePositionAxes = Axes.Y,
                Origin = Anchor.CentreLeft,
                Y = 0.5f,
                Height = 2,
                Colour = Colour4.White.Opacity(0.5f),
            });

            foreach (var key in keys)
            {
                var colour = CalibreColours.ForAction(key.Action);

                foreach (var tap in key.Taps)
                {
                    points.Add(new Circle
                    {
                        RelativePositionAxes = Axes.Both,
                        Origin = Anchor.Centre,
                        Size = new Vector2(6),
                        X = (float)((tap.BeatTime - start) / span),
                        Y = (float)Math.Clamp(0.5 - tap.Offset / (2 * range), 0, 1),
                        Colour = colour,
                    });
                }
            }

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = label_width },
                    Child = points,
                },
                label($"+{range} ms", Anchor.TopLeft),
                label("0", Anchor.CentreLeft),
                label($"-{range} ms", Anchor.BottomLeft),
            };
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

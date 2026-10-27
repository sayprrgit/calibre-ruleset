// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Charts
{
    /// <summary>
    /// Carte 3x3 de l'aire de jeu : chaque zone est teintée selon son indice d'erreur (vert = précis, rouge = faible),
    /// avec sa distance moyenne, son temps de réaction et son taux de miss. La zone faible est encadrée.
    /// </summary>
    public partial class AimGridChart : CompositeDrawable
    {
        /// <summary>
        /// Indice d'erreur à partir duquel une zone est entièrement rouge.
        /// </summary>
        private const double worst_index = 1.5;

        public AimGridChart(AimGridResult result)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var grid = new Container
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                FillAspectRatio = 4f / 3,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
            };

            foreach (var zone in result.Zones)
            {
                int column = zone.Zone % AimGrid.COLUMNS;
                int row = zone.Zone / AimGrid.COLUMNS;
                bool isWeak = zone.Zone == result.WeakZone;

                grid.Add(new Container
                {
                    RelativePositionAxes = Axes.Both,
                    RelativeSizeAxes = Axes.Both,
                    Position = new Vector2((float)column / AimGrid.COLUMNS, (float)row / AimGrid.ROWS),
                    Size = new Vector2(1f / AimGrid.COLUMNS, 1f / AimGrid.ROWS),
                    Padding = new MarginPadding(3),
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 6,
                        BorderThickness = isWeak ? 4 : 0,
                        BorderColour = CalibreColours.ACCENT,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = zone.Attempts == 0 ? Colour4.Gray.Opacity(0.3f) : colourFor(zone.ErrorIndex),
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    text(AimGrid.ZoneName(zone.Zone), 13, FontWeight.Bold),
                                    text(zone.Attempts == 0 ? "non testée" : $"{DiagnosticFormat.Number(zone.MeanDistance)} px · {DiagnosticFormat.Ms(zone.MeanReactionTime)}", 13),
                                    text(zone.Attempts == 0 ? string.Empty : $"miss {DiagnosticFormat.Percent(zone.MissRate)}", 13),
                                },
                            },
                        },
                    },
                });
            }

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 300,
                Child = grid,
            };
        }

        /// <summary>
        /// Dégradé vert → jaune → rouge sur l'indice d'erreur, assombri pour garder le texte lisible.
        /// </summary>
        private static Colour4 colourFor(double errorIndex)
        {
            float t = (float)Math.Clamp(errorIndex / worst_index, 0, 1);

            var good = Colour4.FromHex("#3ddc97");
            var medium = Colour4.FromHex("#ffcc33");
            var bad = Colour4.FromHex("#ff5566");

            var colour = t < 0.5f ? interpolate(good, medium, t * 2) : interpolate(medium, bad, (t - 0.5f) * 2);
            return colour.Darken(0.6f);
        }

        private static Colour4 interpolate(Colour4 from, Colour4 to, float t)
            => new Colour4(from.R + (to.R - from.R) * t, from.G + (to.G - from.G) * t, from.B + (to.B - from.B) * t, 1);

        private static OsuSpriteText text(string value, float size, FontWeight weight = FontWeight.Regular) => new OsuSpriteText
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            Text = value,
            Font = OsuFont.Default.With(size: size, weight: weight),
        };
    }
}

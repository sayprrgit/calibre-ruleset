// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Calibre.Judgements;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Calibre.UI
{
    /// <summary>
    /// Élément de HUD propre à Calibre : pour chaque entrée (K1, K2, M1, M2), le nombre de frappes réussies
    /// et leur écart moyen à l'instant parfait. Négatif = en avance, positif = en retard.
    /// </summary>
    /// <remarks>
    /// Mis à jour à chaque jugement (et annulé lors d'un retour en arrière), jamais à chaque frame.
    /// </remarks>
    public partial class KeyOffsetDisplay : CompositeDrawable
    {
        [Resolved(canBeNull: true)]
        private ScoreProcessor? scoreProcessor { get; set; }

        private readonly KeyColumn[] columns =
        {
            new KeyColumn("K1"),
            new KeyColumn("K2"),
            new KeyColumn("M1"),
            new KeyColumn("M2"),
        };

        public KeyOffsetDisplay()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new FillFlowContainer<KeyColumn>
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(18, 0),
                Children = columns,
            };
        }

        public int CountFor(CalibreAction action) => columns[(int)action].HitCount;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Hors d'une partie (scène de test du playfield seul), il n'y a pas de score à suivre.
            if (scoreProcessor == null)
            {
                Hide();
                return;
            }

            scoreProcessor.NewJudgement += onNewJudgement;
            scoreProcessor.JudgementReverted += onJudgementReverted;
        }

        private void onNewJudgement(JudgementResult result) => apply(result, 1);

        private void onJudgementReverted(JudgementResult result) => apply(result, -1);

        private void apply(JudgementResult result, int direction)
        {
            // Seules les frappes réussies portent une touche et un écart de timing : cercles et têtes de slider.
            if (result is not CalibreHitCircleJudgementResult { HitAction: CalibreAction action } || !result.IsHit)
                return;

            columns[(int)action].Add(result.TimeOffset, direction);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (scoreProcessor != null)
            {
                scoreProcessor.NewJudgement -= onNewJudgement;
                scoreProcessor.JudgementReverted -= onJudgementReverted;
            }
        }

        private partial class KeyColumn : FillFlowContainer
        {
            public int HitCount { get; private set; }

            private double offsetSum;

            private readonly OsuSpriteText value;

            public KeyColumn(string label)
            {
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(5, 0);

                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = label,
                        Font = OsuFont.Default.With(size: 13, weight: FontWeight.Bold),
                        Colour = CalibreColours.ACCENT,
                    },
                    value = new OsuSpriteText
                    {
                        Text = "–",
                        Font = OsuFont.Default.With(size: 13),
                    },
                };
            }

            public void Add(double timeOffset, int direction)
            {
                HitCount += direction;
                offsetSum += timeOffset * direction;

                value.Text = HitCount == 0 ? "–" : $"{HitCount} · {offsetSum / HitCount:+0;-0;0} ms";
            }
        }
    }
}

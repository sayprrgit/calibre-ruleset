// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Timing;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects.Legacy;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Test de précision d'aim : des cibles apparaissent une à une sur une grille 3x3 de l'aire de jeu.
    /// L'aire de jeu a exactement le cadrage du jeu, pour que les distances mesurées soient celles d'une vraie partie.
    /// </summary>
    public partial class AimGridTestRunner : CompositeDrawable, IKeyBindingHandler<CalibreAction>
    {
        /// <summary>
        /// Graine fixe par défaut : deux périphériques testés voient la même séquence de cibles.
        /// </summary>
        public const int DEFAULT_SEED = 2026;

        public const int DEFAULT_ROUNDS = 5;

        /// <summary>
        /// Sans frappe dans ce délai, la cible est comptée comme manquée.
        /// </summary>
        public const double TIMEOUT = 1500;

        /// <summary>
        /// Rayon d'un cercle à CS 4, la taille la plus courante.
        /// </summary>
        public static readonly float DEFAULT_RADIUS = CalibreHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(4, true);

        public event Action<AimGridResult>? Completed;

        private readonly int seed;
        private readonly int rounds;
        private readonly float radius;

        private IClock? clock;

        private IReadOnlyList<AimTarget> targets = Array.Empty<AimTarget>();
        private readonly List<AimAttempt> attempts = new List<AimAttempt>();

        private State state;
        private int targetIndex;
        private double nextAppearTime;
        private double appearTime;

        private OsuTextFlowContainer instructions = null!;
        private RoundedButton startButton = null!;
        private OsuSpriteText progress = null!;
        private Container field = null!;
        private CircularContainer target = null!;

        public bool IsRunning => state == State.Waiting || state == State.Showing;

        /// <summary>
        /// Cible affichée en ce moment, s'il y en a une.
        /// </summary>
        public AimTarget? CurrentTarget => state == State.Showing ? targets[targetIndex] : null;

        /// <param name="seed">Graine de la séquence de cibles.</param>
        /// <param name="rounds">Nombre de tours de 9 cibles.</param>
        /// <param name="clock">Horloge de datation ; par défaut, un chronomètre lu à chaque réception de frappe.</param>
        public AimGridTestRunner(int seed = DEFAULT_SEED, int rounds = DEFAULT_ROUNDS, IClock? clock = null)
        {
            this.seed = seed;
            this.rounds = rounds;
            this.clock = clock;

            radius = DEFAULT_RADIUS;
            attempts.Capacity = rounds * AimGrid.ZONE_COUNT;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 12),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Précision d'aim par zone",
                                    Font = OsuFont.Default.With(size: 28, weight: FontWeight.Bold),
                                },
                                instructions = new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 17))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = $"{rounds * AimGrid.ZONE_COUNT} cibles apparaissent une à une sur une grille 3x3 de l'aire de jeu. "
                                           + "Vise le centre et frappe (K1, K2, M1 ou M2) le plus vite possible. "
                                           + $"Une cible non frappée en {TIMEOUT / 1000:0.#} s compte comme manquée.",
                                },
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(20, 0),
                                    Children = new Drawable[]
                                    {
                                        startButton = new RoundedButton
                                        {
                                            Width = 200,
                                            Height = 40,
                                            Text = "Démarrer",
                                            Action = start,
                                        },
                                        progress = new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Font = OsuFont.Default.With(size: 17),
                                        },
                                    },
                                },
                            },
                        },
                    },
                    new Drawable[]
                    {
                        new CalibrePlayfieldAdjustmentContainer
                        {
                            Child = field = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        BorderThickness = 2,
                                        BorderColour = CalibreColours.PLAYFIELD_BORDER,
                                        Child = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.White,
                                            Alpha = 0.02f,
                                        },
                                    },
                                    gridLine(1f / 3, true),
                                    gridLine(2f / 3, true),
                                    gridLine(1f / 3, false),
                                    gridLine(2f / 3, false),
                                    target = new CircularContainer
                                    {
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(radius * 2),
                                        Masking = true,
                                        BorderThickness = 8,
                                        BorderColour = Colour4.White,
                                        Alpha = 0,
                                        Child = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = CalibreColours.ACCENT,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        // Ligne de 1 px d'épaisseur sur toute la longueur : l'axe relatif reçoit 1 (100 %), l'autre 1 px.
        private static Box gridLine(float position, bool vertical) => new Box
        {
            RelativePositionAxes = vertical ? Axes.X : Axes.Y,
            RelativeSizeAxes = vertical ? Axes.Y : Axes.X,
            X = vertical ? position : 0,
            Y = vertical ? 0 : position,
            Width = 1,
            Height = 1,
            Origin = vertical ? Anchor.TopCentre : Anchor.CentreLeft,
            Colour = Colour4.White.Opacity(0.08f),
        };

        private void start()
        {
            clock ??= new StopwatchClock(true);

            targets = AimTargetSequence.Generate(seed, rounds, radius);
            attempts.Clear();
            targetIndex = 0;

            startButton.Hide();
            instructions.Alpha = 0.5f;

            scheduleNext(clock.CurrentTime);
        }

        private void scheduleNext(double now)
        {
            target.Alpha = 0;

            if (targetIndex >= targets.Count)
            {
                finish();
                return;
            }

            progress.Text = $"Cible {targetIndex + 1} sur {targets.Count}";
            nextAppearTime = now + targets[targetIndex].Delay;
            state = State.Waiting;
        }

        protected override void Update()
        {
            base.Update();

            if (clock == null)
                return;

            double now = clock.CurrentTime;

            switch (state)
            {
                case State.Waiting when now >= nextAppearTime:
                    target.Position = targets[targetIndex].Position;
                    target.Alpha = 1;
                    appearTime = now;
                    state = State.Showing;
                    break;

                case State.Showing when now - appearTime >= TIMEOUT:
                    var expired = targets[targetIndex];
                    attempts.Add(new AimAttempt(expired.Zone, expired.Position, null, null, false));
                    advance(now);
                    break;
            }
        }

        public bool OnPressed(KeyBindingPressEvent<CalibreAction> e)
        {
            if (state != State.Showing || clock == null || e.Repeat || !e.Action.IsHitAction())
                return false;

            // Datation à la réception de la frappe, et position ramenée dans l'espace de l'aire de jeu (512x384).
            double now = clock.CurrentTime;
            var press = field.ToLocalSpace(e.ScreenSpaceMousePosition);
            var current = targets[targetIndex];

            bool hit = Vector2.Distance(press, current.Position) <= radius;
            attempts.Add(new AimAttempt(current.Zone, current.Position, press, now - appearTime, hit));

            advance(now);
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<CalibreAction> e)
        {
        }

        /// <summary>
        /// Convertit une position de l'aire de jeu (512x384) en coordonnées écran.
        /// </summary>
        public Vector2 PlayfieldToScreenSpace(Vector2 position) => field.ToScreenSpace(position);

        private void advance(double now)
        {
            targetIndex++;
            scheduleNext(now);
        }

        private void finish()
        {
            state = State.Done;
            progress.Text = string.Empty;

            Completed?.Invoke(new AimGridResult(seed, radius, attempts.ToArray()));
        }

        private enum State
        {
            Idle,
            Waiting,
            Showing,
            Done,
        }
    }
}

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
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.UI;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Base des tests de frappe sur métronome : consignes, démarrage, progression, indicateur visuel et enregistrement.
    /// </summary>
    /// <remarks>
    /// Chaque frappe est datée à sa réception (dans <see cref="OnPressed"/>) avec l'horloge du métronome,
    /// c'est-à-dire la position de lecture de sa piste, et non l'heure de la frame affichée.
    /// Pendant une phase, <see cref="Update"/> et <see cref="OnPressed"/> n'allouent rien.
    /// </remarks>
    public abstract partial class MetronomeTestRunner : CompositeDrawable, IKeyBindingHandler<CalibreAction>
    {
        /// <summary>
        /// Durée de l'éclair du métronome visuel après chaque temps.
        /// </summary>
        private const double flash_duration = 150;

        [Resolved]
        private IMetronomeSource source { get; set; } = null!;

        protected OsuTextFlowContainer Instructions { get; private set; } = null!;

        protected OsuSpriteText Status { get; private set; } = null!;

        private readonly string title;

        private RoundedButton startButton = null!;
        private Circle flash = null!;
        private Box progressFill = null!;

        private TapRecorder recorder = new TapRecorder(0);
        private MetronomeSchedule? schedule;
        private IClock? clock;
        private bool visualCue;
        private Action<IReadOnlyList<TapSample>>? phaseCompleted;

        public bool IsRunning => clock != null;

        /// <summary>
        /// Frappes enregistrées pendant la phase en cours.
        /// </summary>
        public int RecordedTaps => recorder.Count;

        protected MetronomeTestRunner(string title)
        {
            this.title = title;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 20),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = title,
                        Font = OsuFont.Default.With(size: 28, weight: FontWeight.Bold),
                    },
                    Instructions = new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 17))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(140),
                        Children = new Drawable[]
                        {
                            new CircularContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                BorderThickness = 4,
                                BorderColour = Colour4.White.Opacity(0.5f),
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    AlwaysPresent = true,
                                },
                            },
                            flash = new Circle
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.85f),
                                Alpha = 0,
                            },
                        },
                    },
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(400, 6),
                        Masking = true,
                        CornerRadius = 3,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Colour4.White.Opacity(0.15f),
                            },
                            progressFill = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0,
                                Colour = CalibreColours.ACCENT,
                            },
                        },
                    },
                    startButton = new RoundedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 300,
                        Height = 40,
                    },
                    Status = new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Font = OsuFont.Default.With(size: 15),
                        Colour = Colour4.White.Opacity(0.8f),
                    },
                },
            };
        }

        /// <summary>
        /// Affiche le bouton de démarrage de la prochaine phase.
        /// </summary>
        protected void PrepareStart(string buttonText, Action start)
        {
            startButton.Text = buttonText;
            startButton.Action = start;
            startButton.Show();
        }

        protected void RunPhase(MetronomeSchedule phaseSchedule, bool audible, bool showVisualCue, Action<IReadOnlyList<TapSample>> completed)
        {
            // Allocation unique au démarrage : quatre frappes par temps laissent de la marge aux frappes parasites.
            recorder = new TapRecorder(phaseSchedule.TotalBeats * 4);

            schedule = phaseSchedule;
            visualCue = showVisualCue;
            phaseCompleted = completed;

            startButton.Hide();
            progressFill.Width = 0;
            flash.Alpha = 0;
            Status.Text = string.Empty;

            clock = source.Start(phaseSchedule, audible);
        }

        /// <summary>
        /// Interrompt la phase en cours sans produire de résultat (changement de test, sortie de l'écran).
        /// </summary>
        public void Abort()
        {
            if (clock == null)
                return;

            clock = null;
            phaseCompleted = null;
            source.Stop();
        }

        protected override void Update()
        {
            base.Update();

            if (clock == null || schedule == null)
                return;

            double time = clock.CurrentTime;

            progressFill.Width = (float)Math.Clamp(time / schedule.EndTime, 0, 1);

            if (visualCue)
                updateFlash(time);

            if (time >= schedule.EndTime)
                finishPhase();
        }

        private void updateFlash(double time)
        {
            int beat = (int)Math.Floor((time - schedule!.FirstBeatTime) / schedule.Interval);

            if (beat < 0 || beat >= schedule.TotalBeats)
            {
                flash.Alpha = 0;
                return;
            }

            // L'éclair s'allume à l'instant du temps puis s'éteint linéairement : calculé à chaque frame, sans transformation allouée.
            flash.Alpha = (float)Math.Max(0, 1 - (time - schedule.BeatTime(beat)) / flash_duration);
            flash.Colour = schedule.IsLeadIn(beat) ? Colour4.Gray : CalibreColours.ACCENT;
        }

        private void finishPhase()
        {
            var completed = phaseCompleted;

            clock = null;
            phaseCompleted = null;
            flash.Alpha = 0;
            source.Stop();

            completed?.Invoke(recorder.ToList());
        }

        public bool OnPressed(KeyBindingPressEvent<CalibreAction> e)
        {
            if (clock == null || e.Repeat || !e.Action.IsHitAction())
                return false;

            recorder.Record(e.Action, clock.CurrentTime);
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<CalibreAction> e)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            if (clock != null)
                source.Stop();

            base.Dispose(isDisposing);
        }
    }
}

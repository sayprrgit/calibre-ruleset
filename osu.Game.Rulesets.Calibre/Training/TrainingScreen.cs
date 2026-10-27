// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Calibre.Analysis;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.Diagnostics.Screens;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Statistics;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Écran « Entraînement adaptatif » : affiche le profil, cible ses 3 patterns les plus faibles et lance un exercice généré.
    /// </summary>
    public partial class TrainingScreen : OsuScreen
    {
        /// <summary>
        /// Patterns d'un premier exercice, tant que le profil ne compte pas assez de parties pour en juger.
        /// </summary>
        public static readonly PatternType[] DEFAULT_TARGETS = { PatternType.Stream, PatternType.ShortWideJump, PatternType.Slider };

        public override string Title => "Entraînement adaptatif";

        public override bool HideOverlaysOnEnter => true;

        public readonly BindableDouble Bpm = new BindableDouble(180) { MinValue = ExerciseGenerator.MINIMUM_BPM, MaxValue = ExerciseGenerator.MAXIMUM_BPM, Precision = 5 };
        public readonly BindableDouble Duration = new BindableDouble(60) { MinValue = 30, MaxValue = 180, Precision = 10 };
        public readonly BindableDouble Intensity = new BindableDouble(1) { MinValue = 0.5, MaxValue = 2, Precision = 0.1 };

        public PlayerProfile Profile { get; private set; } = new PlayerProfile();

        public IReadOnlyList<PatternType> Targets { get; private set; } = DEFAULT_TARGETS;

        /// <summary>
        /// Dernier exercice lancé et sa graine (affichée, pour rejouer le même exercice).
        /// </summary>
        public IBeatmap? LastExercise { get; private set; }

        public int? LastSeed { get; private set; }

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Orange);

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        private readonly Ruleset ruleset;
        private readonly int? fixedSeed;

        private readonly GeneratedAudioStore metronomeStore = new GeneratedAudioStore();
        private ITrackStore metronomeTracks = null!;

        private Container profileContainer = null!;
        private OsuTextFlowContainer targetsText = null!;
        private OsuSpriteText settingsSummary = null!;
        private OsuSpriteText status = null!;

        /// <param name="ruleset">Ruleset Calibre, pour les touches et la beatmap d'exercice.</param>
        /// <param name="seed">Graine imposée (tests) ; par défaut, une graine aléatoire à chaque exercice.</param>
        public TrainingScreen(Ruleset ruleset, int? seed = null)
        {
            this.ruleset = ruleset;
            fixedSeed = seed;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            metronomeTracks = audio.GetTrackStore(metronomeStore);

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("#16161c"),
                    Alpha = 0.95f,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 720,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 14),
                        Padding = new MarginPadding { Top = 40, Bottom = 100 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = "Entraînement adaptatif",
                                Font = OsuFont.Default.With(size: 32, weight: FontWeight.Bold),
                                Colour = CalibreColours.ACCENT,
                            },
                            new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 16))
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Text = "Chaque partie de Calibre enrichit ton profil. L'exercice cible tes trois patterns les plus faibles ; "
                                       + "un pattern réussi à plus de 95 % devient un peu plus dur, sous 80 % un peu plus facile.",
                            },
                            heading("Profil"),
                            profileContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            },
                            targetsText = new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 16))
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            },
                            heading("Exercice"),
                            new SettingsSlider<double> { LabelText = "BPM", Current = Bpm, KeyboardStep = 5 },
                            new SettingsSlider<double> { LabelText = "Durée (secondes)", Current = Duration, KeyboardStep = 10 },
                            new SettingsSlider<double> { LabelText = "Intensité (sauts, streams)", Current = Intensity, KeyboardStep = 0.1f },
                            settingsSummary = new OsuSpriteText { Font = OsuFont.Default.With(size: 15) },
                            new RoundedButton
                            {
                                Width = 260,
                                Height = 44,
                                Text = "Lancer l'exercice",
                                Action = StartExercise,
                            },
                            status = new OsuSpriteText
                            {
                                Font = OsuFont.Default.With(size: 14),
                                Colour = Colour4.White.Opacity(0.8f),
                            },
                        },
                    },
                },
            };

            Bpm.BindValueChanged(_ => updateSummary());
            Duration.BindValueChanged(_ => updateSummary());
            Intensity.BindValueChanged(_ => updateSummary(), true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            refreshProfile();
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            // Retour de l'écran de résultats : le profil vient peut-être d'être mis à jour.
            refreshProfile();
        }

        private void refreshProfile()
        {
            Profile = ProfileStore.Load(storage);

            var weakest = Profile.WeakestPatterns();
            Targets = weakest.Count > 0 ? weakest : DEFAULT_TARGETS;

            profileContainer.Child = Profile.GamesAnalysed == 0
                ? new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 16))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = "Aucune partie analysée pour l'instant.",
                }
                : new PatternTable(new[] { "Pattern", "Objets", "Précision", "UR", "Niveau" },
                    PlayerProfile.TRAINABLE.Where(Profile.Patterns.ContainsKey).Select(type => new[]
                    {
                        PatternClassifier.DisplayName(type) + (Targets.Contains(type) ? "  ◀" : string.Empty),
                        Profile.Patterns[type].Objects.ToString(),
                        DiagnosticFormat.Percent(Profile.Patterns[type].Accuracy),
                        DiagnosticFormat.Number(Profile.Patterns[type].UnstableRate),
                        "×" + DiagnosticFormat.Number(Profile.Patterns[type].Level, "0.0"),
                    }));

            targetsText.Text = (weakest.Count > 0
                                   ? $"{Profile.GamesAnalysed} parties analysées. Patterns ciblés : "
                                   : $"Pas encore assez de parties (au moins {PlayerProfile.MINIMUM_OBJECTS} objets par pattern). Premier exercice : ")
                               + string.Join(", ", Targets.Select(PatternClassifier.DisplayName)) + ".";
        }

        private void updateSummary()
            => settingsSummary.Text = $"{Bpm.Value:0} BPM · {Duration.Value:0} s · intensité {DiagnosticFormat.Number(Intensity.Value, "0.0")}";

        /// <summary>
        /// Génère l'exercice, en fait la beatmap courante et lance la partie.
        /// </summary>
        public void StartExercise()
        {
            int seed = fixedSeed ?? RNG.Next();
            var targets = Targets.Select(type => new PatternTarget(type, Profile.LevelOf(type))).ToArray();

            var beatmap = ExerciseGenerator.Generate(new ExerciseSettings(Bpm.Value, Duration.Value, Intensity.Value), targets, seed);

            beatmap.BeatmapInfo.Ruleset = ruleset.RulesetInfo;
            beatmap.BeatmapInfo.DifficultyName = $"graine {seed}";
            beatmap.BeatmapInfo.Metadata = new BeatmapMetadata
            {
                Title = "Exercice Calibre",
                Artist = "Calibre",
                Source = "Calibre",
            };

            LastExercise = beatmap;
            LastSeed = seed;
            status.Text = $"Exercice généré avec la graine {seed} : {string.Join(", ", Targets.Select(PatternClassifier.DisplayName))}.";

            var schedule = metronomeFor(beatmap);
            Beatmap.Value = new ExerciseWorkingBeatmap(beatmap, () => createMetronome(schedule), audio);
            Ruleset.Value = ruleset.RulesetInfo;

            this.Push(new PlayerLoader(() => new TrainingPlayer()));
        }

        /// <summary>
        /// Métronome de l'exercice : quatre temps d'annonce avant le premier objet, puis un clic par temps jusqu'au dernier.
        /// </summary>
        private static MetronomeSchedule metronomeFor(Beatmap<CalibreHitObject> beatmap)
        {
            double beat = beatmap.ControlPointInfo.TimingPointAt(0).BeatLength;
            double last = beatmap.HitObjects.Max(h => h.GetEndTime());

            return new MetronomeSchedule(beat, 4, (int)Math.Ceiling((last - ExerciseGenerator.START_TIME) / beat) + 1, ExerciseGenerator.START_TIME - 4 * beat);
        }

        private Track createMetronome(MetronomeSchedule schedule)
        {
            string name = $"calibre-exercice-{schedule.GetHashCode()}.wav";
            metronomeStore.Add(name, MetronomeWaveGenerator.Generate(schedule));
            return metronomeTracks.Get(name);
        }

        private static OsuSpriteText heading(string text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.Default.With(size: 22, weight: FontWeight.SemiBold),
            Margin = new MarginPadding { Top = 10 },
        };

        protected override void Dispose(bool isDisposing)
        {
            metronomeTracks?.Dispose();
            metronomeStore.Dispose();

            base.Dispose(isDisposing);
        }
    }
}

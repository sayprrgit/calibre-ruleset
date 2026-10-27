// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Rulesets.Calibre.Diagnostics.Audio;
using osu.Game.Rulesets.Calibre.Diagnostics.Export;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Screens;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Écran « Diagnostic des périphériques », ouvert depuis les réglages du ruleset : trois tests, leurs résultats
    /// et l'export JSON horodaté qui permet de comparer deux périphériques ou deux réglages.
    /// </summary>
    public partial class DiagnosticScreen : OsuScreen
    {
        public override string Title => "Diagnostic des périphériques";

        public override bool HideOverlaysOnEnter => true;

        public LatencyResult? Latency { get; private set; }

        public KeyStabilityResult? KeyStability { get; private set; }

        public AimGridResult? AimGrid { get; private set; }

        /// <summary>
        /// Nom du dernier fichier exporté, relatif au dossier d'export.
        /// </summary>
        public string? LastExportedFile { get; private set; }

        public Storage ExportStorage { get; private set; } = null!;

        /// <summary>
        /// Contenu affiché à droite : un test en cours ou une vue de résultats.
        /// </summary>
        public Drawable? CurrentContent { get; private set; }

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private MusicController? musicController { get; set; }

        private readonly Ruleset ruleset;
        private readonly IMetronomeSource metronome;

        private Container contentHost = null!;
        private RoundedButton latencyButton = null!;
        private RoundedButton stabilityButton = null!;
        private RoundedButton aimButton = null!;
        private RoundedButton exportButton = null!;
        private OsuTextBox labelBox = null!;
        private OsuTextFlowContainer exportStatus = null!;

        private bool musicWasPlaying;

        /// <param name="ruleset">Ruleset dont les touches (K1, K2, M1, M2) sont utilisées pendant les tests.</param>
        /// <param name="metronome">Source du métronome ; par défaut, une piste audio générée.</param>
        public DiagnosticScreen(Ruleset ruleset, IMetronomeSource? metronome = null)
        {
            this.ruleset = ruleset;
            this.metronome = metronome ?? new TrackMetronomeSource();
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(metronome);
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            ExportStorage = DiagnosticExporter.GetStorage(storage);

            if (metronome is Drawable metronomeDrawable)
                AddInternal(metronomeDrawable);

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("#16161c"),
                    Alpha = 0.95f,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 340),
                        new Dimension(),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            createSidebar(),
                            new CalibreInputManager(ruleset.RulesetInfo)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = contentHost = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 50, Top = 40, Bottom = 90 },
                                },
                            },
                        },
                    },
                },
            });

            refreshButtons();
        }

        private Drawable createSidebar() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                    Alpha = 0.3f,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Padding = new MarginPadding { Horizontal = 25, Top = 40 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = "Diagnostic",
                            Font = OsuFont.Default.With(size: 32, weight: FontWeight.Bold),
                            Colour = CalibreColours.ACCENT,
                        },
                        new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 15))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = "Mesure tes périphériques et ta frappe, puis exporte les résultats pour comparer deux réglages.",
                            Margin = new MarginPadding { Bottom = 10 },
                        },
                        latencyButton = sidebarButton(() => showLatency()),
                        stabilityButton = sidebarButton(() => showKeyStability()),
                        aimButton = sidebarButton(() => showAimGrid()),
                        new OsuSpriteText
                        {
                            Text = "Export",
                            Font = OsuFont.Default.With(size: 20, weight: FontWeight.SemiBold),
                            Margin = new MarginPadding { Top = 25 },
                        },
                        labelBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            PlaceholderText = "Libellé : périphérique, réglage…",
                            LengthLimit = 80,
                        },
                        exportButton = new RoundedButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            Text = "Exporter en JSON",
                            Action = export,
                        },
                        new RoundedButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            Text = "Ouvrir le dossier des exports",
                            Action = () => ExportStorage.PresentExternally(),
                        },
                        exportStatus = new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 14))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        },
                    },
                },
            },
        };

        private static RoundedButton sidebarButton(Action action) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 44,
            Action = action,
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            setContent(new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 18))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = "Choisis un test à gauche.\n\n"
                       + "• Latence perçue : frappe sur un métronome audio puis visuel, pour en déduire ton offset.\n"
                       + "• Stabilité par touche : UR, biais et dérive de K1, K2, M1 et M2, et détection d'une touche traînante.\n"
                       + "• Précision d'aim : distance, temps de réaction et taux de miss sur chaque zone d'une grille 3x3.\n\n"
                       + "Les tests utilisent tes touches de jeu Calibre. Coupe la musique et les sons système pendant les tests audio.",
            });
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            // La musique du menu fausserait le test audio : elle est coupée pendant le diagnostic, puis relancée à la sortie.
            musicWasPlaying = musicController?.IsPlaying == true;
            musicController?.Stop();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            abortCurrent();

            if (musicWasPlaying)
                musicController?.Play();

            return base.OnExiting(e);
        }

        private void showLatency()
        {
            if (Latency != null)
                showResult(new LatencyResultView(Latency), startLatency);
            else
                startLatency();
        }

        private void startLatency()
        {
            var runner = new LatencyTestRunner();

            runner.Completed += result => Schedule(() =>
            {
                Latency = result;
                refreshButtons();
                showResult(new LatencyResultView(result), startLatency);
            });

            setContent(runner);
        }

        private void showKeyStability()
        {
            if (KeyStability != null)
                showResult(new KeyStabilityResultView(KeyStability), startKeyStability);
            else
                startKeyStability();
        }

        private void startKeyStability()
        {
            var runner = new KeyStabilityTestRunner();

            runner.Completed += result => Schedule(() =>
            {
                KeyStability = result;
                refreshButtons();
                showResult(new KeyStabilityResultView(result), startKeyStability);
            });

            setContent(runner);
        }

        private void showAimGrid()
        {
            if (AimGrid != null)
                showResult(new AimGridResultView(AimGrid), startAimGrid);
            else
                startAimGrid();
        }

        private void startAimGrid()
        {
            var runner = new AimGridTestRunner();

            runner.Completed += result => Schedule(() =>
            {
                AimGrid = result;
                refreshButtons();
                showResult(new AimGridResultView(result), startAimGrid);
            });

            setContent(runner);
        }

        private void showResult(ResultView view, Action restart)
        {
            setContent(new OsuScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 20),
                    Children = new Drawable[]
                    {
                        view,
                        new RoundedButton
                        {
                            Width = 220,
                            Height = 40,
                            Text = "Recommencer le test",
                            Action = restart,
                        },
                    },
                },
            });
        }

        private void setContent(Drawable content)
        {
            abortCurrent();

            contentHost.Clear();
            contentHost.Add(content);
            CurrentContent = content;
        }

        private void abortCurrent()
        {
            if (CurrentContent is MetronomeTestRunner runner)
                runner.Abort();
        }

        private void refreshButtons()
        {
            latencyButton.Text = withCheck("Latence perçue", Latency != null);
            stabilityButton.Text = withCheck("Stabilité par touche", KeyStability != null);
            aimButton.Text = withCheck("Précision d'aim", AimGrid != null);

            exportButton.Enabled.Value = Latency != null || KeyStability != null || AimGrid != null;
        }

        private static string withCheck(string text, bool done) => done ? $"{text}  ✓" : text;

        private void export()
        {
            var report = DiagnosticReport.Create(DateTimeOffset.Now, labelBox.Text ?? string.Empty, Latency, KeyStability, AimGrid);

            try
            {
                LastExportedFile = DiagnosticExporter.Export(report, ExportStorage);
                exportStatus.Text = $"Exporté : {DiagnosticExporter.DIRECTORY}/{LastExportedFile}";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Échec de l'export du diagnostic Calibre.");
                exportStatus.Text = $"Échec de l'export : {ex.Message}";
            }
        }
    }
}

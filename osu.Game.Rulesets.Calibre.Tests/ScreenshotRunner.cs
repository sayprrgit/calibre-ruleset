// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osu.Game.Graphics.Cursor;
using osu.Game.Rulesets.Calibre.Tests.Visual;
using osu.Game.Rulesets.Calibre.Tests.Visual.Diagnostics;
using osu.Game.Rulesets.Calibre.Tests.Visual.Training;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace osu.Game.Rulesets.Calibre.Tests
{
    /// <summary>
    /// Régénère les captures du README : joue des scènes visuelles dans une vraie fenêtre et enregistre l'image obtenue.
    /// </summary>
    public partial class ScreenshotRunner : OsuGameBase
    {
        // Méthode de test jouée après la mise en place (null : mise en place seule), puis attente avant la capture.
        private static readonly (string File, Type Scene, string? Method, double Delay)[] shots =
        {
            ("gameplay.png", typeof(TestSceneExercisePlayer), null, 12000),
            ("analyse.png", typeof(TestSceneResultsAnalysis), nameof(TestSceneResultsAnalysis.TestPanelsForASimulatedPlay), 1000),
            ("diagnostic-latence.png", typeof(TestSceneMetronomeTests), nameof(TestSceneMetronomeTests.TestLatencyTest), 1000),
            ("diagnostic-stabilite.png", typeof(TestSceneMetronomeTests), nameof(TestSceneMetronomeTests.TestKeyStabilityTest), 1000),
            ("diagnostic-aim.png", typeof(TestSceneAimGridTest), nameof(TestSceneAimGridTest.TestWeakZoneIsFound), 1000),
            ("entrainement.png", typeof(TestSceneTrainingScreen), nameof(TestSceneTrainingScreen.TestWeakestPatternsAreTargeted), 1000),
        };

        private readonly string directory;
        private readonly Container content = new Container { RelativeSizeAxes = Axes.Both };

        public ScreenshotRunner(string directory)
        {
            this.directory = Path.GetFullPath(directory);
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig)
        {
            frameworkConfig.SetValue(FrameworkSetting.WindowMode, WindowMode.Windowed);
            frameworkConfig.SetValue(FrameworkSetting.WindowedSize, new System.Drawing.Size(1280, 960));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Add(content);
            Directory.CreateDirectory(directory);
            play(0);
        }

        private void play(int index)
        {
            if (index == shots.Length)
            {
                Exit();
                return;
            }

            var shot = shots[index];
            var scene = (TestScene)Activator.CreateInstance(shot.Scene)!;

            // Comme le navigateur de tests : étapes de mise en place, puis celles de la méthode, exécutées une fois la scène chargée.
            scene.OnLoadComplete += _ => Schedule(() =>
            {
                foreach (var setUp in shot.Scene.GetMethods().Where(m => m.IsDefined(typeof(SetUpStepsAttribute), true)))
                    setUp.Invoke(scene, null);

                if (shot.Method != null)
                    shot.Scene.GetMethod(shot.Method)!.Invoke(scene, null);

                scene.RunAllSteps(() =>
                {
                    // Rend la main à la vraie souris pour retirer le bandeau des scènes à entrée manuelle, et masque les
                    // curseurs qui la suivraient. Le curseur de jeu de Calibre, lui, reste visible.
                    foreach (var input in scene.ChildrenOfType<ManualInputManager>())
                    {
                        input.UseParentInput = true;
                        input.ShowVisualCursorGuide = false;
                    }

                    foreach (var cursor in this.ChildrenOfType<MenuCursorContainer>())
                        cursor.Alpha = 0;

                    Scheduler.AddDelayed(() => capture(scene, shot.File, index), shot.Delay);
                }, (step, e) =>
                {
                    Console.Error.WriteLine($"{shot.File} : échec de l'étape « {step.Text} » : {e.Message}");
                    Schedule(() => play(index + 1));
                });
            });

            content.Child = scene;
        }

        private async void capture(TestScene scene, string file, int index)
        {
            // Sans la liste des étapes, à gauche de la scène.
            int left = (int)Math.Ceiling(scene.StepsContainer.ScreenSpaceDrawQuad.TopRight.X);

            try
            {
                using (var image = await Host.TakeScreenshotAsync())
                {
                    image.Mutate(i => i.Crop(new Rectangle(left, 0, image.Width - left, image.Height)));
                    await image.SaveAsPngAsync(Path.Combine(directory, file));
                }

                Console.WriteLine($"capture : {file}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{file} : {e.Message}");
            }

            Schedule(() => play(index + 1));
        }
    }
}

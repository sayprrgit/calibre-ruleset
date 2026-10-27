// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Calibre.Objects.Drawables;
using osu.Game.Rulesets.Calibre.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Visual
{
    /// <summary>
    /// Affiche des cercles dans un playfield pour vérifier leur rendu : apparition, anneau d'approche, frappe et miss.
    /// </summary>
    public partial class TestSceneHitCircle : OsuTestScene
    {
        private readonly List<TestDrawableHitCircle> circles = new List<TestDrawableHitCircle>();

        [TestCase(2f)]
        [TestCase(5f)]
        [TestCase(7f)]
        public void TestHitAndMiss(float circleSize)
        {
            AddStep("créer les cercles", () => createCircles(circleSize));
            AddUntilStep("tous jugés", () => circles.TrueForAll(c => c.Judged));
            AddAssert("les cercles frappés sont Great", () => circles[0].Result.Type, () => Is.EqualTo(HitResult.Great));
            AddAssert("les cercles ignorés sont Miss", () => circles[1].Result.Type, () => Is.EqualTo(HitResult.Miss));
        }

        [Test]
        public void TestComboColoursAndNumbers()
        {
            AddStep("créer un stream", () =>
            {
                createCircles(4, count: 8, spacing: 50);
            });
            AddUntilStep("tous jugés", () => circles.TrueForAll(c => c.Judged));
        }

        private void createCircles(float circleSize, int count = 2, float spacing = 240)
        {
            circles.Clear();

            var playfield = new TestCalibrePlayfield();

            for (int i = 0; i < count; i++)
            {
                var circle = new HitCircle
                {
                    // Le premier cercle (et un sur deux ensuite) est frappé automatiquement, les autres sont manqués.
                    StartTime = Time.Current + 1000 + i * 150,
                    Position = new Vector2(256 - spacing * (count - 1) / 2 + spacing * i, 192),
                    IndexInCurrentCombo = i,
                    ComboIndex = i / 4,
                };

                circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { CircleSize = circleSize, ApproachRate = 8 });

                var drawable = new TestDrawableHitCircle(circle, autoHit: i % 2 == 0);
                circles.Add(drawable);
                playfield.Add(drawable);
            }

            Child = new CalibrePlayfieldAdjustmentContainer { Child = playfield };
        }

        private partial class TestCalibrePlayfield : CalibrePlayfield
        {
            public TestCalibrePlayfield()
            {
                RelativeSizeAxes = Axes.Both;
            }
        }

        private partial class TestDrawableHitCircle : DrawableHitCircle
        {
            private readonly bool autoHit;

            public TestDrawableHitCircle(HitCircle hitCircle, bool autoHit)
                : base(hitCircle)
            {
                this.autoHit = autoHit;
            }

            protected override void CheckForResult(bool userTriggered, double timeOffset)
            {
                if (autoHit && !userTriggered && timeOffset >= 0)
                {
                    ApplyResult(HitResult.Great);
                    return;
                }

                base.CheckForResult(userTriggered, timeOffset);
            }
        }
    }
}

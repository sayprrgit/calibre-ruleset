// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Calibre.Beatmaps;
using osu.Game.Rulesets.Calibre.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class CalibreBeatmapConverterTest
    {
        [Test]
        public void TestCanConvertStandardBeatmap()
        {
            var beatmap = TestBeatmaps.Decode(TestBeatmaps.MIXED_OBJECTS);

            Assert.That(new CalibreBeatmapConverter(beatmap, new CalibreRuleset()).CanConvert(), Is.True);
        }

        [Test]
        public void TestCirclesAndSliderHeadsAreConvertedAndSpinnersSkipped()
        {
            var objects = TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS).HitObjects.Cast<CalibreHitObject>().ToList();

            Assert.That(objects, Has.All.InstanceOf<HitCircle>());
            Assert.That(objects.Select(o => o.StartTime), Is.EqualTo(new[] { 1000d, 2000d, 5000d }));
            Assert.That(objects.Select(o => o.Position), Is.EqualTo(new[] { new Vector2(100, 100), new Vector2(200, 150), new Vector2(400, 300) }));
        }

        [Test]
        public void TestDefaultsFollowDifficulty()
        {
            var circle = (CalibreHitObject)TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS).HitObjects[0];

            // AR 9 : 1200 - (9 - 5) * 150 = 600 ms de préempt.
            Assert.That(circle.TimePreempt, Is.EqualTo(600));
            Assert.That(circle.Scale, Is.EqualTo(LegacyRulesetExtensions.CalculateScaleFromCircleSize(4, true)));
        }

        [Test]
        public void TestComboInformationIsComputed()
        {
            var objects = TestBeatmaps.DecodePlayable(TestBeatmaps.MIXED_OBJECTS).HitObjects.Cast<CalibreHitObject>().ToList();

            // Le dernier cercle porte le drapeau « nouveau combo » (type 5 = cercle + nouveau combo).
            Assert.That(objects.Select(o => o.IndexInCurrentCombo), Is.EqualTo(new[] { 0, 1, 0 }));
            Assert.That(objects[2].ComboIndex, Is.GreaterThan(objects[1].ComboIndex));
        }
    }
}

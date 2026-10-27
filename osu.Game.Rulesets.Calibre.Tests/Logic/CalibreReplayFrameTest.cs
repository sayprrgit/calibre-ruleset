// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Calibre.Replays;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic
{
    [TestFixture]
    public class CalibreReplayFrameTest
    {
        private static readonly object[] action_sets =
        {
            new CalibreAction[0],
            new[] { CalibreAction.Key1 },
            new[] { CalibreAction.Key2 },
            new[] { CalibreAction.Mouse1 },
            new[] { CalibreAction.Mouse2 },
            new[] { CalibreAction.Key1, CalibreAction.Key2 },
            new[] { CalibreAction.Mouse1, CalibreAction.Mouse2 },
            new[] { CalibreAction.Key1, CalibreAction.Mouse2 },
            new[] { CalibreAction.Mouse1, CalibreAction.Key2 },
        };

        [TestCaseSource(nameof(action_sets))]
        public void TestLegacyRoundTripKeepsEachInput(CalibreAction[] actions)
        {
            var beatmap = new Beatmap();
            var original = new CalibreReplayFrame(1234, new Vector2(12, 34), actions);

            var decoded = new CalibreReplayFrame();
            decoded.FromLegacy(original.ToLegacy(beatmap), beatmap);

            Assert.That(decoded.Position, Is.EqualTo(original.Position));
            Assert.That(decoded.Actions, Is.EquivalentTo(actions));
        }

        [Test]
        public void TestKeyboardUsesSecondaryBitLikeStable()
        {
            var legacy = new CalibreReplayFrame(0, Vector2.Zero, CalibreAction.Key1).ToLegacy(new Beatmap());

            Assert.That(legacy.ButtonState, Is.EqualTo(ReplayButtonState.Left1 | ReplayButtonState.Left2));
        }
    }
}

// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Game.Rulesets.Calibre.Diagnostics.Export;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;
using osuTK;

namespace osu.Game.Rulesets.Calibre.Tests.Logic.Diagnostics
{
    [TestFixture]
    public class DiagnosticExportTest
    {
        private static readonly DateTimeOffset created_at = new DateTimeOffset(2026, 10, 27, 16, 30, 12, TimeSpan.FromHours(1));

        private string directory = null!;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "calibre-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void TestJsonContainsEveryCompletedTest()
        {
            using var json = JsonDocument.Parse(DiagnosticExporter.Serialize(createReport()));
            var root = json.RootElement;

            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(DiagnosticReport.CURRENT_SCHEMA_VERSION));
            Assert.That(root.GetProperty("createdAt").GetString(), Is.EqualTo("2026-10-27T16:30:12+01:00"));
            Assert.That(root.GetProperty("label").GetString(), Is.EqualTo("Clavier A, 1000 Hz"));

            var latency = root.GetProperty("latency");
            Assert.That(latency.GetProperty("recommendedAudioOffsetMs").GetDouble(), Is.EqualTo(-20));
            Assert.That(latency.GetProperty("audio").GetProperty("offsetsMs").GetArrayLength(), Is.EqualTo(12));

            var keyboard = root.GetProperty("keyStability").GetProperty("keyboard");
            Assert.That(keyboard.GetProperty("laggingKey").GetString(), Is.EqualTo("K2"));
            Assert.That(keyboard.GetProperty("first").GetString(), Is.EqualTo("K1"));

            var aim = root.GetProperty("aimGrid");
            Assert.That(aim.GetProperty("zones").GetArrayLength(), Is.EqualTo(9));
            Assert.That(aim.GetProperty("zones")[8].GetProperty("name").GetString(), Is.EqualTo("bas droite"));
            Assert.That(aim.GetProperty("attempts").GetArrayLength(), Is.EqualTo(2));
        }

        [Test]
        public void TestMissingTestsAreNull()
        {
            var report = DiagnosticReport.Create(created_at, "souris", null, null, null);

            using var json = JsonDocument.Parse(DiagnosticExporter.Serialize(report));

            Assert.That(json.RootElement.GetProperty("latency").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(json.RootElement.GetProperty("aimGrid").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }

        [Test]
        public void TestRoundTrip()
        {
            var report = createReport();
            var read = DiagnosticExporter.Deserialize(DiagnosticExporter.Serialize(report))!;

            Assert.That(read.CreatedAt, Is.EqualTo(report.CreatedAt));
            Assert.That(read.KeyStability!.Keys.Select(k => k.Key), Is.EqualTo(new[] { "K1", "K2" }));
            Assert.That(read.AimGrid!.Attempts[1].PressX, Is.Null);
        }

        [Test]
        public void TestExportCreatesTimestampedFiles()
        {
            var storage = new NativeStorage(directory);
            var report = createReport();

            string first = DiagnosticExporter.Export(report, storage);
            string second = DiagnosticExporter.Export(report, storage);

            Assert.That(first, Is.EqualTo("calibre-diagnostic-20261027-163012.json"));
            Assert.That(second, Is.EqualTo("calibre-diagnostic-20261027-163012-2.json"), "pas d'écrasement à la même seconde");

            string content = File.ReadAllText(Path.Combine(directory, first));
            Assert.That(DiagnosticExporter.Deserialize(content)!.Label, Is.EqualTo("Clavier A, 1000 Hz"));
        }

        private static DiagnosticReport createReport()
        {
            var schedule = new MetronomeSchedule(600, 4, 12);
            var audio = Enumerable.Range(4, 12).Select(i => new TapSample(schedule.BeatTime(i) + 20, CalibreAction.Key1)).ToArray();
            var visual = Enumerable.Range(4, 12).Select(i => new TapSample(schedule.BeatTime(i) + 35, CalibreAction.Key1)).ToArray();
            var latency = new LatencyResult(new LatencyPhaseResult(schedule, audio), new LatencyPhaseResult(schedule, visual));

            var stabilitySchedule = new MetronomeSchedule(300, 4, 40);
            var taps = Enumerable.Range(4, 40)
                                 .Select(i => i % 2 == 0
                                     ? new TapSample(stabilitySchedule.BeatTime(i) + (i % 4 == 0 ? -3 : 3), CalibreAction.Key1)
                                     : new TapSample(stabilitySchedule.BeatTime(i) + (i % 4 == 1 ? 17 : 23), CalibreAction.Key2))
                                 .ToArray();
            var stability = new KeyStabilityResult(stabilitySchedule, taps);

            var aim = new AimGridResult(1, 36, new[]
            {
                new AimAttempt(8, new Vector2(420, 320), new Vector2(425, 322), 410, true),
                new AimAttempt(0, new Vector2(80, 60), null, null, false),
            });

            return DiagnosticReport.Create(created_at, "  Clavier A, 1000 Hz ", latency, stability, aim);
        }
    }
}

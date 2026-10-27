// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Game.Rulesets.Calibre.Diagnostics.Logic;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Export
{
    /// <summary>
    /// Contenu d'un export JSON du diagnostic. Les tests non effectués valent <c>null</c>.
    /// Les données brutes (écarts, tentatives) sont incluses pour pouvoir comparer deux exports finement.
    /// </summary>
    public class DiagnosticReport
    {
        /// <summary>
        /// Incrémenté à chaque changement incompatible du format.
        /// </summary>
        public const int CURRENT_SCHEMA_VERSION = 1;

        public int SchemaVersion { get; set; } = CURRENT_SCHEMA_VERSION;

        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Libellé libre saisi par le joueur : périphérique ou réglage testé.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        public string RulesetVersion { get; set; } = string.Empty;

        public string OperatingSystem { get; set; } = string.Empty;

        public LatencyReport? Latency { get; set; }

        public KeyStabilityReport? KeyStability { get; set; }

        public AimGridReport? AimGrid { get; set; }

        public static DiagnosticReport Create(DateTimeOffset createdAt, string label, LatencyResult? latency, KeyStabilityResult? keyStability, AimGridResult? aimGrid) => new DiagnosticReport
        {
            CreatedAt = createdAt,
            Label = label.Trim(),
            RulesetVersion = typeof(CalibreRuleset).Assembly.GetName().Version?.ToString() ?? string.Empty,
            OperatingSystem = RuntimeInformation.OSDescription,
            Latency = latency == null ? null : LatencyReport.From(latency),
            KeyStability = keyStability == null ? null : KeyStabilityReport.From(keyStability),
            AimGrid = aimGrid == null ? null : AimGridReport.From(aimGrid),
        };

        /// <summary>
        /// Arrondi au centième de milliseconde : au-delà, la précision affichée serait illusoire.
        /// </summary>
        internal static double? Round(double? value) => value is double v ? Math.Round(v, 2) : null;

    }

    public class MetronomeReport
    {
        public double BeatsPerMinute { get; set; }

        public double IntervalMs { get; set; }

        public int LeadInBeats { get; set; }

        public int MeasuredBeats { get; set; }

        public static MetronomeReport From(MetronomeSchedule schedule) => new MetronomeReport
        {
            BeatsPerMinute = Math.Round(schedule.BeatsPerMinute, 2),
            IntervalMs = schedule.Interval,
            LeadInBeats = schedule.LeadInBeats,
            MeasuredBeats = schedule.MeasuredBeats,
        };
    }

    public class OffsetStatisticsReport
    {
        public int Count { get; set; }

        public double? MeanMs { get; set; }

        public double? StandardDeviationMs { get; set; }

        public double? UnstableRate { get; set; }

        public static OffsetStatisticsReport From(OffsetStatistics statistics) => new OffsetStatisticsReport
        {
            Count = statistics.Count,
            MeanMs = DiagnosticReport.Round(statistics.Mean),
            StandardDeviationMs = DiagnosticReport.Round(statistics.StandardDeviation),
            UnstableRate = DiagnosticReport.Round(statistics.UnstableRate),
        };
    }

    public class LatencyPhaseReport
    {
        public MetronomeReport Metronome { get; set; } = new MetronomeReport();

        public OffsetStatisticsReport Statistics { get; set; } = new OffsetStatisticsReport();

        public int IgnoredTaps { get; set; }

        public double[] OffsetsMs { get; set; } = Array.Empty<double>();

        public static LatencyPhaseReport From(LatencyPhaseResult phase) => new LatencyPhaseReport
        {
            Metronome = MetronomeReport.From(phase.Schedule),
            Statistics = OffsetStatisticsReport.From(phase.Statistics),
            IgnoredTaps = phase.Matched.IgnoredTaps,
            OffsetsMs = phase.Matched.Taps.Select(t => Math.Round(t.Offset, 2)).ToArray(),
        };
    }

    public class LatencyReport
    {
        public LatencyPhaseReport Audio { get; set; } = new LatencyPhaseReport();

        public LatencyPhaseReport Visual { get; set; } = new LatencyPhaseReport();

        public double? RecommendedAudioOffsetMs { get; set; }

        public double? RecommendedVisualOffsetMs { get; set; }

        public double? AudioVisualGapMs { get; set; }

        public static LatencyReport From(LatencyResult result) => new LatencyReport
        {
            Audio = LatencyPhaseReport.From(result.Audio),
            Visual = LatencyPhaseReport.From(result.Visual),
            RecommendedAudioOffsetMs = result.RecommendedAudioOffset,
            RecommendedVisualOffsetMs = result.RecommendedVisualOffset,
            AudioVisualGapMs = DiagnosticReport.Round(result.AudioVisualGap),
        };
    }

    public class KeyReport
    {
        public string Key { get; set; } = string.Empty;

        public OffsetStatisticsReport Statistics { get; set; } = new OffsetStatisticsReport();

        public double? DriftMsPerMinute { get; set; }

        public double[] OffsetsMs { get; set; } = Array.Empty<double>();

        public static KeyReport From(KeyStats stats) => new KeyReport
        {
            Key = stats.Action.ShortName(),
            Statistics = OffsetStatisticsReport.From(stats.Statistics),
            DriftMsPerMinute = DiagnosticReport.Round(stats.DriftPerMinute),
            OffsetsMs = stats.Taps.Select(t => Math.Round(t.Offset, 2)).ToArray(),
        };
    }

    public class KeyComparisonReport
    {
        public string First { get; set; } = string.Empty;

        public string Second { get; set; } = string.Empty;

        public double MeanDifferenceMs { get; set; }

        public double MeanTestPValue { get; set; }

        public double UnstableRateRatio { get; set; }

        public double VarianceTestPValue { get; set; }

        public string? LaggingKey { get; set; }

        public string? LessStableKey { get; set; }

        public static KeyComparisonReport From(KeyComparison comparison) => new KeyComparisonReport
        {
            First = comparison.First.Action.ShortName(),
            Second = comparison.Second.Action.ShortName(),
            MeanDifferenceMs = Math.Round(comparison.MeanDifference, 2),
            MeanTestPValue = Math.Round(comparison.MeanTest.PValue, 6),
            UnstableRateRatio = finite(Math.Round(comparison.UnstableRateRatio, 3)),
            VarianceTestPValue = Math.Round(comparison.VarianceTest.PValue, 6),
            LaggingKey = comparison.LaggingKey is CalibreAction lagging ? lagging.ShortName() : null,
            LessStableKey = comparison.LessStableKey is CalibreAction lessStable ? lessStable.ShortName() : null,
        };

        // JSON n'admet ni l'infini ni NaN. UR nul sur la première touche seule : rapport infini, borné ici ;
        // UR nul sur les deux : rapport indéterminé, ramené à 1 (aucune différence).
        private static double finite(double value)
        {
            if (double.IsNaN(value))
                return 1;

            return double.IsInfinity(value) ? double.MaxValue : value;
        }
    }

    public class KeyStabilityReport
    {
        public MetronomeReport Metronome { get; set; } = new MetronomeReport();

        public int IgnoredTaps { get; set; }

        public List<KeyReport> Keys { get; set; } = new List<KeyReport>();

        public KeyComparisonReport? Keyboard { get; set; }

        public KeyComparisonReport? Mouse { get; set; }

        public static KeyStabilityReport From(KeyStabilityResult result) => new KeyStabilityReport
        {
            Metronome = MetronomeReport.From(result.Schedule),
            IgnoredTaps = result.Matched.IgnoredTaps,
            Keys = result.Keys.Select(KeyReport.From).ToList(),
            Keyboard = result.Keyboard == null ? null : KeyComparisonReport.From(result.Keyboard),
            Mouse = result.Mouse == null ? null : KeyComparisonReport.From(result.Mouse),
        };
    }

    public class ZoneReport
    {
        public int Zone { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Attempts { get; set; }

        public int Hits { get; set; }

        public double MissRate { get; set; }

        public double? MeanDistancePx { get; set; }

        public double? MeanReactionTimeMs { get; set; }

        public double ErrorIndex { get; set; }

        public static ZoneReport From(ZoneStats zone) => new ZoneReport
        {
            Zone = zone.Zone,
            Name = AimGrid.ZoneName(zone.Zone),
            Attempts = zone.Attempts,
            Hits = zone.Hits,
            MissRate = Math.Round(zone.MissRate, 4),
            MeanDistancePx = DiagnosticReport.Round(zone.MeanDistance),
            MeanReactionTimeMs = DiagnosticReport.Round(zone.MeanReactionTime),
            ErrorIndex = Math.Round(zone.ErrorIndex, 4),
        };
    }

    public class AimAttemptReport
    {
        public int Zone { get; set; }

        public double TargetX { get; set; }

        public double TargetY { get; set; }

        public double? PressX { get; set; }

        public double? PressY { get; set; }

        public double? ReactionTimeMs { get; set; }

        public bool Hit { get; set; }

        public static AimAttemptReport From(AimAttempt attempt) => new AimAttemptReport
        {
            Zone = attempt.Zone,
            TargetX = Math.Round(attempt.Target.X, 2),
            TargetY = Math.Round(attempt.Target.Y, 2),
            PressX = DiagnosticReport.Round(attempt.Press?.X),
            PressY = DiagnosticReport.Round(attempt.Press?.Y),
            ReactionTimeMs = DiagnosticReport.Round(attempt.ReactionTime),
            Hit = attempt.Hit,
        };
    }

    public class AimGridReport
    {
        public int Seed { get; set; }

        public double TargetRadiusPx { get; set; }

        public double HitRate { get; set; }

        public int? WorstZone { get; set; }

        public int? WeakZone { get; set; }

        public List<ZoneReport> Zones { get; set; } = new List<ZoneReport>();

        public List<AimAttemptReport> Attempts { get; set; } = new List<AimAttemptReport>();

        public static AimGridReport From(AimGridResult result) => new AimGridReport
        {
            Seed = result.Seed,
            TargetRadiusPx = Math.Round(result.TargetRadius, 2),
            HitRate = Math.Round(result.HitRate, 4),
            WorstZone = result.WorstZone,
            WeakZone = result.WeakZone,
            Zones = result.Zones.Select(ZoneReport.From).ToList(),
            Attempts = result.Attempts.Select(AimAttemptReport.From).ToList(),
        };
    }
}

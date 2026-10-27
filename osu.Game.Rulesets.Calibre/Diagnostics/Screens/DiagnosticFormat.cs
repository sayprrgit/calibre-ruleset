// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Screens
{
    /// <summary>
    /// Mise en forme des valeurs affichées : virgule décimale, signe explicite pour les écarts, tiret pour l'absence de valeur.
    /// </summary>
    public static class DiagnosticFormat
    {
        public const string MISSING = "–";

        private static readonly CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");

        public static string SignedMs(double? value, string format = "0") => value is double v ? $"{v.ToString($"+{format};-{format};0", culture)} ms" : MISSING;

        public static string Ms(double? value, string format = "0") => value is double v ? $"{v.ToString(format, culture)} ms" : MISSING;

        public static string Number(double? value, string format = "0") => value is double v ? v.ToString(format, culture) : MISSING;

        public static string Percent(double value) => $"{(value * 100).ToString("0", culture)} %";

        public static string PValue(double p) => p < 0.001 ? "p < 0,001" : $"p = {p.ToString("0.000", culture)}";
    }
}

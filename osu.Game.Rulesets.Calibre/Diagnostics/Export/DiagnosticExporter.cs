// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using System.Text.Json;
using osu.Framework.Platform;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Export
{
    /// <summary>
    /// Écrit les rapports de diagnostic en JSON dans le dossier de données du jeu (<c>calibre/diagnostics</c>).
    /// </summary>
    public static class DiagnosticExporter
    {
        public const string DIRECTORY = "calibre/diagnostics";

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string Serialize(DiagnosticReport report) => JsonSerializer.Serialize(report, options);

        public static DiagnosticReport? Deserialize(string json) => JsonSerializer.Deserialize<DiagnosticReport>(json, options);

        public static Storage GetStorage(Storage gameStorage) => gameStorage.GetStorageForDirectory(DIRECTORY);

        /// <summary>
        /// Nom horodaté (heure locale, à la seconde), complété d'un suffixe si un export existe déjà à la même seconde.
        /// </summary>
        public static string GetFileName(DiagnosticReport report, Storage storage)
        {
            string baseName = $"calibre-diagnostic-{report.CreatedAt:yyyyMMdd-HHmmss}";
            string fileName = $"{baseName}.json";

            for (int i = 2; storage.Exists(fileName); i++)
                fileName = $"{baseName}-{i}.json";

            return fileName;
        }

        /// <param name="report">Rapport à écrire.</param>
        /// <param name="storage">Dossier de destination, en général celui renvoyé par <see cref="GetStorage"/>.</param>
        /// <returns>Le nom du fichier créé dans <paramref name="storage"/>.</returns>
        public static string Export(DiagnosticReport report, Storage storage)
        {
            string fileName = GetFileName(report, storage);
            byte[] content = Encoding.UTF8.GetBytes(Serialize(report));

            // Écriture atomique : un export interrompu ne laisse pas de fichier tronqué.
            using (var stream = storage.CreateFileSafely(fileName))
                stream.Write(content, 0, content.Length);

            return fileName;
        }
    }
}

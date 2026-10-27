// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Rulesets.Calibre.Training
{
    /// <summary>
    /// Lecture et écriture du profil dans le dossier de données du jeu (<c>calibre/profile.json</c>).
    /// </summary>
    /// <remarks>
    /// Le profil est mis à jour depuis une tâche de fond après chaque partie : un verrou sérialise les lectures-écritures.
    /// </remarks>
    public static class ProfileStore
    {
        public const string DIRECTORY = "calibre";
        public const string FILE_NAME = "profile.json";

        /// <summary>
        /// Copie d'un profil illisible, conservée avant qu'il ne soit remplacé par un profil vierge.
        /// </summary>
        public const string CORRUPT_FILE_NAME = "profile.corrupt.json";

        private static readonly object gate = new object();

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static PlayerProfile Load(Storage gameStorage)
        {
            lock (gate)
                return read(gameStorage.GetStorageForDirectory(DIRECTORY));
        }

        /// <summary>
        /// Lit le profil, applique <paramref name="change"/> puis l'enregistre, sans qu'une autre mise à jour s'intercale.
        /// </summary>
        public static PlayerProfile Update(Storage gameStorage, Action<PlayerProfile> change)
        {
            lock (gate)
            {
                var storage = gameStorage.GetStorageForDirectory(DIRECTORY);
                var profile = read(storage);

                change(profile);

                byte[] content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(profile, options));

                // Écriture atomique : une coupure pendant l'écriture ne laisse pas un profil tronqué.
                using (var stream = storage.CreateFileSafely(FILE_NAME))
                    stream.Write(content, 0, content.Length);

                return profile;
            }
        }

        private static PlayerProfile read(Storage storage)
        {
            if (!storage.Exists(FILE_NAME))
                return new PlayerProfile();

            try
            {
                using (var stream = storage.GetStream(FILE_NAME))
                    return JsonSerializer.Deserialize<PlayerProfile>(stream, options) ?? new PlayerProfile();
            }
            catch (JsonException e)
            {
                // Le profil illisible est mis de côté plutôt qu'écrasé par la prochaine mise à jour.
                Logger.Error(e, $"Profil Calibre illisible : copie conservée dans {CORRUPT_FILE_NAME}, nouveau profil vierge.");
                File.Copy(storage.GetFullPath(FILE_NAME), storage.GetFullPath(CORRUPT_FILE_NAME), true);
                return new PlayerProfile();
            }
        }
    }
}

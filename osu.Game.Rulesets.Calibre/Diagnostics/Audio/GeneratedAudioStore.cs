// Copyright (c) 2026 Lynho. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;

namespace osu.Game.Rulesets.Calibre.Diagnostics.Audio
{
    /// <summary>
    /// Magasin de ressources en mémoire : sert au gestionnaire audio les pistes générées par code.
    /// </summary>
    /// <remarks>
    /// Lu depuis le thread audio : le dictionnaire est donc concurrent.
    /// </remarks>
    public class GeneratedAudioStore : IResourceStore<byte[]>
    {
        private readonly ConcurrentDictionary<string, byte[]> resources = new ConcurrentDictionary<string, byte[]>();

        public void Add(string name, byte[] data) => resources[name] = data;

        public void Remove(string name) => resources.TryRemove(name, out _);

        public byte[] Get(string name) => resources.TryGetValue(name, out byte[]? data) ? data : null!;

        public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Get(name));

        public Stream? GetStream(string name) => resources.TryGetValue(name, out byte[]? data) ? new MemoryStream(data, false) : null;

        public IEnumerable<string> GetAvailableResources() => resources.Keys;

        public void Dispose() => resources.Clear();
    }
}

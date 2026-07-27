using System.Collections.Generic;
using System.IO;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Core.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Persistence.Tests
{
    public sealed class FakeSaveService : ISaveService
    {
        private readonly Dictionary<string, string> _storage = new();

        public bool FailsOnWrite { get; set; }

        public int Writes { get; private set; }

        public UniTask SaveAsync<T>(string key, T data)
        {
            if (FailsOnWrite) 
                throw new IOException("хранилище недоступно");

            _storage[key] = JsonUtility.ToJson(data);
            Writes++;

            return UniTask.CompletedTask;
        }

        public UniTask<T> LoadAsync<T>(string key) => UniTask.FromResult(
            _storage.TryGetValue(key, out string json) ? JsonUtility.FromJson<T>(json) : default);

        public UniTask<bool> ExistsAsync(string key) => UniTask.FromResult(_storage.ContainsKey(key));

        public void Seed<T>(string key, T data) => _storage[key] = JsonUtility.ToJson(data);

        public T Peek<T>(string key) where T : class =>
            _storage.TryGetValue(key, out string json) ? JsonUtility.FromJson<T>(json) : null;
    }

    public sealed class FakeCargoCatalog : ICargoCatalog
    {
        public List<string> Requested { get; } = new();

        public List<Vector3> Positions { get; } = new();

        public UniTask<CargoTypeDefinition> LoadTypeAsync(
            string cargoTypeKey, CancellationToken ct = default) =>
            UniTask.FromResult<CargoTypeDefinition>(null);

        public UniTask<CargoBox> SpawnAsync(
            string cargoTypeKey, Vector3 position, Quaternion rotation, CancellationToken ct = default)
        {
            Requested.Add(cargoTypeKey);
            Positions.Add(position);

            return UniTask.FromResult<CargoBox>(null);
        }

        public void Despawn(CargoBox box)
        {
        }
    }
}

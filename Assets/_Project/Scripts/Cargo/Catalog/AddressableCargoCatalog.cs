using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrateExpectations.Cargo.Catalog
{
    public sealed class AddressableCargoCatalog : ICargoCatalog, IDisposable
    {
        private readonly CargoRegistryDefinition _registry;
        private readonly List<GameObject> _instances = new();

        private bool _disposed;

        public AddressableCargoCatalog(CargoRegistryDefinition registry) => _registry = registry;

        public UniTask<CargoTypeDefinition> LoadTypeAsync(
            string cargoTypeKey, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(cargoTypeKey))
                throw new ArgumentException("Пустой ключ типа груза.", nameof(cargoTypeKey));

            return UniTask.FromResult(_registry.CargoByKey(cargoTypeKey));
        }

        public async UniTask<CargoBox> SpawnAsync(
            string cargoTypeKey, Vector3 position, Quaternion rotation, CancellationToken ct = default)
        {
            CargoTypeDefinition type = await LoadTypeAsync(cargoTypeKey, ct);
            ThrowIfDisposed();

            if (type == null)
            {
                Debug.LogError($"Тип груза по ключу '{cargoTypeKey}' не заведён в реестре.");
                return null;
            }

            if (string.IsNullOrEmpty(type.PrefabKey))
            {
                Debug.LogError($"У типа груза по ключу '{cargoTypeKey}' не задан PrefabKey.");
                return null;
            }

            GameObject instance = await Addressables
                .InstantiateAsync(type.PrefabKey, position, rotation)
                .ToUniTask(cancellationToken: ct);

            if (instance == null) 
                return null;

            if (_disposed)
            {
                Addressables.ReleaseInstance(instance);
                return null;
            }

            _instances.Add(instance);

            if (!instance.TryGetComponent(out CargoBox box))
            {
                Debug.LogError($"Префаб '{type.PrefabKey}' - не ящик груза: нет CargoBox.", instance);
                return null;
            }

            box.AssignIdentity(type);

            return box;
        }

        public void Despawn(CargoBox box)
        {
            if (box == null) 
                return;

            GameObject instance = box.gameObject;

            if (_instances.Remove(instance)) 
                Addressables.ReleaseInstance(instance);
            else 
                UnityEngine.Object.Destroy(instance);
        }

        public void Dispose()
        {
            if (_disposed) 
                return;

            _disposed = true;

            for (int i = 0; i < _instances.Count; i++)
                if (_instances[i] != null) 
                    Addressables.ReleaseInstance(_instances[i]);

            _instances.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(AddressableCargoCatalog));
        }
    }
}

using System;
using System.Threading;
using CrateExpectations.Cargo.Events;
using CrateExpectations.Core.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Cargo.Catalog
{
    public sealed class CargoSpawner : MonoBehaviour
    {
        [SerializeField] private CargoManifestDefinition _manifest;
        [SerializeField] private Transform[] _spawnPoints;

        private ICargoCatalog _catalog;
        private IEventBus _bus;

        [Inject]
        public void Construct(ICargoCatalog catalog, IEventBus bus)
        {
            _catalog = catalog;
            _bus = bus;
        }

        private void Start() => SpawnAllAsync(destroyCancellationToken).Forget();

        private async UniTaskVoid SpawnAllAsync(CancellationToken ct)
        {
            if (_manifest == null || _spawnPoints == null) 
                return;

            string[] keys = _manifest.CargoKeys;
            int count = Mathf.Min(keys.Length, _spawnPoints.Length);

            if (keys.Length != _spawnPoints.Length)
            {
                Debug.LogWarning(
                    $"В манифесте {keys.Length} ключей, а точек спавна {_spawnPoints.Length}: " +
                    $"выставлено будет {count}.", this);
            }

            try
            {
                for (int i = 0; i < count; i++)
                {
                    Transform point = _spawnPoints[i];
                    CargoBox box = await _catalog.SpawnAsync(keys[i], point.position, point.rotation, ct);

                    if (box != null) 
                        _bus.Publish(new CargoSpawned(box));
                }
            }
            catch (OperationCanceledException)
            {
                // Сцену закрыли посреди заргузки (не ошибка);
            }
        }
    }
}

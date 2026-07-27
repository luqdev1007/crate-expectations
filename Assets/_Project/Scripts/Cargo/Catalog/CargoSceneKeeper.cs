using System;
using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo.Events;
using CrateExpectations.Core.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    public sealed class CargoSceneKeeper : IDisposable
    {
        private readonly ICargoCatalog _catalog;
        private readonly CargoRegistryDefinition _registry;
        private readonly IEventBus _bus;

        private readonly List<CargoBox> _live = new(8);

        public CargoSceneKeeper(
            ICargoCatalog catalog, CargoRegistryDefinition registry, IEventBus bus)
        {
            _catalog = catalog;
            _registry = registry;
            _bus = bus;

            _bus.Subscribe<CargoSpawned>(OnCargoSpawned);
        }

        public void Dispose() => _bus.Unsubscribe<CargoSpawned>(OnCargoSpawned);

        public CargoSceneSnapshot Capture()
        {
            Forget();

            var crates = new List<CargoCrateSnapshot>(_live.Count);

            for (int i = 0; i < _live.Count; i++)
            {
                CargoBox box = _live[i];
                string typeKey = _registry.KeyOf(box.Identity.TrueType);

                if (string.IsNullOrEmpty(typeKey)) 
                    continue;

                Transform transform = box.transform;

                crates.Add(new CargoCrateSnapshot
                {
                    TypeKey = typeKey,
                    DeclaredTypeKey = _registry.KeyOf(box.State.DeclaredType),
                    PaintId = CargoRegistryDefinition.IdOf(box.State.Paint),
                    StampId = CargoRegistryDefinition.IdOf(box.State.Stamp),
                    Position = transform.position,
                    Rotation = transform.rotation,
                });
            }

            return new CargoSceneSnapshot(crates.ToArray());
        }

        public async UniTask RestoreAsync(CargoSceneSnapshot snapshot, CancellationToken ct = default)
        {
            DespawnAll();

            CargoCrateSnapshot[] crates = snapshot.Crates;
            for (int i = 0; i < crates.Length; i++)
            {
                CargoCrateSnapshot crate = crates[i];

                CargoBox box = await _catalog.SpawnAsync(
                    crate.TypeKey, crate.Position, crate.Rotation, ct);

                if (box == null)
                {
                    Debug.LogWarning($"[Груз] В сохранении ящик '{crate.TypeKey}', а каталог его не отдал -> пропущен");
                    continue;
                }

                Restore(box, crate);
                _bus.Publish(new CargoSpawned(box));
            }
        }

        private void Restore(CargoBox box, in CargoCrateSnapshot crate)
        {
            CargoTypeDefinition declared = _registry.CargoByKey(crate.DeclaredTypeKey);

            var state = new CargoState(
                _registry.PaintById(crate.PaintId),
                _registry.StampById(crate.StampId),
                declared != null ? declared : box.Identity.TrueType);

            box.ApplyState(state);
        }

        private void DespawnAll()
        {
            Forget();

            for (int i = 0; i < _live.Count; i++) 
                _catalog.Despawn(_live[i]);

            _live.Clear();
        }

        private void OnCargoSpawned(CargoSpawned spawned)
        {
            if (spawned.Box != null) 
                _live.Add(spawned.Box);
        }

        private void Forget()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (_live[i] == null) 
                    _live.RemoveAt(i);
            }
        }
    }
}

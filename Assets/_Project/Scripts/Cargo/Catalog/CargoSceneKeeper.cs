using System;
using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo.Events;
using CrateExpectations.Core.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    /// <summary>
    /// Груз на локации целиком: кто сейчас стоит на доке и как его собрать заново.
    /// Модуль <c>Cargo</c> отвечает за свой кусок сохранения сам - координатор сохранения
    /// просит снимок и отдаёт снимок, а как ящик устроен, не знает.
    ///
    /// <para>Учёт ведётся по событию <see cref="CargoSpawned"/> - тому же, на котором живёт
    /// реестр груза. Сканировать сцену в поисках ящиков не нужно ни тому, ни другому.</para>
    /// </summary>
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

        /// <inheritdoc />
        public void Dispose() => _bus.Unsubscribe<CargoSpawned>(OnCargoSpawned);

        /// <summary>Снять состояние всего груза на доке</summary>
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

        /// <summary>
        /// Убрать всё, что стоит на доке, и выставить груз из сохранения. Ящики создаются
        /// тем же каталогом, что и при обычном спавне, и так же объявляются в шину -
        /// поэтому реестр груза и всё, что на него подписано, наполняются сами собой
        /// </summary>
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

        /// <summary>Вернуть ящику заявленное состояние: окраску, пломбу и то, чем он назывался</summary>
        private void Restore(CargoBox box, in CargoCrateSnapshot crate)
        {
            // Заявленный тип по умолчанию равен истинному: ящик, который не переливали,
            // сохраняется с тем же ключом, и разрешается он в тот же ассет
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

        /// <summary>Выбросить ящики, которых уже нет: изъятый груз увозят, и он не возвращается</summary>
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

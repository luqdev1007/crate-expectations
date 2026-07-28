using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Events;
using CrateExpectations.Core.Events;

namespace CrateExpectations.Inventory
{
    /// <summary>
    /// Единственное место, где реестр встречается со сценой: слушает появление ящиков
    /// и следит за их перекраской. Вынесен из <see cref="CargoInventory"/> нарочно -
    /// так сам реестр остаётся чистыми данными, а вся возня с подписками собрана
    /// в одном небольшом классе, который живёт и умирает вместе с контейнером
    /// </summary>
    public sealed class CargoRegistrar : IDisposable
    {
        private readonly ICargoInventory _inventory;
        private readonly IEventBus _bus;

        // Ящики, на чьи изменения мы подписаны: нужны только чтобы отписаться
        private readonly List<CargoBox> _tracked = new(8);

        public CargoRegistrar(ICargoInventory inventory, IEventBus bus)
        {
            _inventory = inventory;
            _bus = bus;

            _bus.Subscribe<CargoSpawned>(OnCargoSpawned);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bus.Unsubscribe<CargoSpawned>(OnCargoSpawned);

            for (int i = 0; i < _tracked.Count; i++)
            {
                if (_tracked[i] != null) _tracked[i].StateChanged -= OnCargoStateChanged;
            }

            _tracked.Clear();
        }

        private void OnCargoSpawned(CargoSpawned spawned)
        {
            CargoBox box = spawned.Box;
            if (box == null) return;

            _inventory.Register(box.GetInstanceID(), box.Identity, box.State);

            box.StateChanged += OnCargoStateChanged;
            _tracked.Add(box);
        }

        private void OnCargoStateChanged(CargoBox box) =>
            _inventory.Redeclare(box.GetInstanceID(), box.State);
    }
}

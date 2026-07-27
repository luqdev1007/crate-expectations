using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Events;
using CrateExpectations.Core.Events;

namespace CrateExpectations.Inventory
{
    public sealed class CargoRegistrar : IDisposable
    {
        private readonly ICargoInventory _inventory;
        private readonly IEventBus _bus;

        private readonly List<CargoBox> _tracked = new(8);

        public CargoRegistrar(ICargoInventory inventory, IEventBus bus)
        {
            _inventory = inventory;
            _bus = bus;

            _bus.Subscribe<CargoSpawned>(OnCargoSpawned);
        }

        public void Dispose()
        {
            _bus.Unsubscribe<CargoSpawned>(OnCargoSpawned);

            for (int i = 0; i < _tracked.Count; i++)
            {
                if (_tracked[i] != null) 
                    _tracked[i].StateChanged -= OnCargoStateChanged;
            }

            _tracked.Clear();
        }

        private void OnCargoSpawned(CargoSpawned spawned)
        {
            CargoBox box = spawned.Box;

            if (box == null) 
                return;

            _inventory.Register(box.GetInstanceID(), box.Identity, box.State);

            box.StateChanged += OnCargoStateChanged;
            _tracked.Add(box);
        }

        private void OnCargoStateChanged(CargoBox box) =>
            _inventory.Redeclare(box.GetInstanceID(), box.State);
    }
}

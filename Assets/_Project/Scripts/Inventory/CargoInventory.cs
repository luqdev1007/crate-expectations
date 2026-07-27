using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    public sealed class CargoInventory : ICargoInventory
    {
        private readonly List<CargoRecord> _records = new(8);

        private readonly Dictionary<int, int> _index = new(8);

        public IReadOnlyList<CargoRecord> Records => _records;

        public int OnDockCount { get; private set; }

        public int DeliveredCount { get; private set; }

        public int SeizedCount { get; private set; }

        public event Action Changed;

        public bool TryGet(int id, out CargoRecord record)
        {
            if (_index.TryGetValue(id, out int slot))
            {
                record = _records[slot];
                return true;
            }

            record = default;

            return false;
        }

        public void Register(int id, in CargoIdentity truth, in CargoState declared)
        {
            if (_index.ContainsKey(id)) 
                return;

            _index[id] = _records.Count;
            _records.Add(new CargoRecord(id, truth, declared));
            OnDockCount++;

            Changed?.Invoke();
        }

        public void Redeclare(int id, in CargoState declared)
        {
            if (!_index.TryGetValue(id, out int slot)) 
                return;

            CargoRecord current = _records[slot];

            if (current.Declared.Equals(declared)) 
                return;

            _records[slot] = current.Redeclared(declared);
            Changed?.Invoke();
        }

        public void Settle(int id, CargoStanding standing)
        {
            if (!_index.TryGetValue(id, out int slot)) 
                return;

            CargoRecord current = _records[slot];

            if (current.Standing == standing || !current.IsOnDock)
                return;

            _records[slot] = current.Settled(standing);
            OnDockCount--;

            if (standing == CargoStanding.Delivered) 
                DeliveredCount++;
            else if (standing == CargoStanding.Seized) 
                SeizedCount++;

            Changed?.Invoke();
        }

        public void Clear()
        {
            if (_records.Count == 0) 
                return;

            _records.Clear();
            _index.Clear();
            OnDockCount = 0;
            DeliveredCount = 0;
            SeizedCount = 0;

            Changed?.Invoke();
        }
    }
}

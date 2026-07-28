using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    /// <summary>
    /// Реестр груза. Обычный C#-класс без единой ссылки на сцену: список значений плюс
    /// индекс по идентификатору. Кто и когда его наполняет - не его забота, поэтому
    /// весь реестр целиком проверяется edit-mode тестами
    /// </summary>
    public sealed class CargoInventory : ICargoInventory
    {
        private readonly List<CargoRecord> _records = new(8);

        // Индекс "идентификатор → место в списке": записи правятся на каждую покраску,
        // а порядок в списке должен оставаться порядком появления ящиков на доке
        private readonly Dictionary<int, int> _index = new(8);

        /// <inheritdoc />
        public IReadOnlyList<CargoRecord> Records => _records;

        /// <inheritdoc />
        public int OnDockCount { get; private set; }

        /// <inheritdoc />
        public int DeliveredCount { get; private set; }

        /// <inheritdoc />
        public int SeizedCount { get; private set; }

        /// <inheritdoc />
        public event Action Changed;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Register(int id, in CargoIdentity truth, in CargoState declared)
        {
            if (_index.ContainsKey(id)) return;

            _index[id] = _records.Count;
            _records.Add(new CargoRecord(id, truth, declared));
            OnDockCount++;

            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void Redeclare(int id, in CargoState declared)
        {
            if (!_index.TryGetValue(id, out int slot)) return;

            CargoRecord current = _records[slot];
            if (current.Declared.Equals(declared)) return;

            _records[slot] = current.Redeclared(declared);
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void Settle(int id, CargoStanding standing)
        {
            if (!_index.TryGetValue(id, out int slot)) return;

            CargoRecord current = _records[slot];

            // Судьба решается один раз: инспектор может осмотреть ящик повторно,
            // но дважды сдать один и тот же ящик нельзя
            if (current.Standing == standing || !current.IsOnDock) return;

            _records[slot] = current.Settled(standing);
            OnDockCount--;

            if (standing == CargoStanding.Delivered) DeliveredCount++;
            else if (standing == CargoStanding.Seized) SeizedCount++;

            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_records.Count == 0) return;

            _records.Clear();
            _index.Clear();
            OnDockCount = 0;
            DeliveredCount = 0;
            SeizedCount = 0;

            Changed?.Invoke();
        }
    }
}

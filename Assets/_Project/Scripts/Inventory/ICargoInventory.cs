using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    public interface ICargoInventory
    {
        IReadOnlyList<CargoRecord> Records { get; }

        int OnDockCount { get; }

        int DeliveredCount { get; }

        int SeizedCount { get; }

        event Action Changed;

        bool TryGet(int id, out CargoRecord record);

        void Register(int id, in CargoIdentity truth, in CargoState declared);

        void Redeclare(int id, in CargoState declared);

        void Settle(int id, CargoStanding standing);

        void Clear();
    }
}

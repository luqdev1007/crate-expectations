using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Core.Events;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Economy;
using CrateExpectations.Inspection.Events;
using CrateExpectations.Inventory;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    public sealed class ContractManager : IContractManager, IDisposable
    {
        private readonly ContractCatalogDefinition _catalog;
        private readonly PayoutCalculator _calculator;
        private readonly IEconomyService _economy;
        private readonly ICargoInventory _inventory;
        private readonly IEventBus _bus;

        private ContractProgress _active;

        public ContractManager(
            ContractCatalogDefinition catalog,
            PayoutCalculator calculator,
            IEconomyService economy,
            ICargoInventory inventory,
            IEventBus bus)
        {
            _catalog = catalog;
            _calculator = calculator;
            _economy = economy;
            _inventory = inventory;
            _bus = bus;

            _bus.Subscribe<CargoInspected>(OnCargoInspected);
        }

        public ContractProgress Active => _active;

        public IReadOnlyList<ContractDefinition> Available =>
            _catalog != null ? _catalog.Contracts : Array.Empty<ContractDefinition>();

        public void Dispose() => _bus.Unsubscribe<CargoInspected>(OnCargoInspected);

        public bool CanAccept(ContractDefinition contract) =>
            contract != null && contract.IsPlayable && !_active.IsActive;

        public bool Accept(ContractDefinition contract)
        {
            if (!CanAccept(contract)) 
                return false;

            _active = new ContractProgress(contract);
            _bus.Publish(new ContractAccepted(_active));

            return true;
        }

        public ContractSnapshot Capture() => new()
        {
            ContractId = _active.IsActive ? _active.Contract.name : string.Empty,
            Delivered = _active.Delivered,
            Seized = _active.Seized,
        };

        public void Restore(in ContractSnapshot snapshot)
        {
            ContractDefinition contract = FindById(snapshot.ContractId);

            if (contract == null)
            {
                if (!string.IsNullOrEmpty(snapshot.ContractId))
                {
                    Debug.LogWarning(
                        $"[Контракты] В сохранении заказ '{snapshot.ContractId}', " +
                        "а в каталоге такого нет. Игрок остался без активного заказа");
                }

                _active = default;

                return;
            }

            _active = new ContractProgress(contract, snapshot.Delivered, snapshot.Seized);
        }

        private ContractDefinition FindById(string contractId)
        {
            if (string.IsNullOrEmpty(contractId)) 
                return null;

            IReadOnlyList<ContractDefinition> available = Available;

            for (int i = 0; i < available.Count; i++)
                if (available[i] != null && available[i].name == contractId) 
                    return available[i];

            return null;
        }

        private void OnCargoInspected(CargoInspected inspected)
        {
            if (inspected.Cargo == null) 
                return;

            int id = inspected.Cargo.GetInstanceID(); // fix

            if (_inventory.TryGet(id, out CargoRecord record) && !record.IsOnDock)
                return;

            bool seized = inspected.Verdict.IsBust;

            _inventory.Settle(id, seized ? CargoStanding.Seized : CargoStanding.Delivered);

            if (!_active.IsActive) 
                return;

            if (!MatchesActiveContract(inspected.Cargo))
                return;

            var delivery = new DeliveryReport(
                seized ? DeliveryOutcome.Seized : DeliveryOutcome.Cleared,
                spotless: inspected.Verdict.Clues.Count == 0);

            _economy.Apply(_calculator.Calculate(_active.Contract.Terms, delivery));

            _active = seized ? _active.WithSeizure() : _active.WithDelivery();
            _bus.Publish(new ContractProgressed(_active, seized));

            if (_active.IsComplete) 
                Close(completed: true);
            else if (_active.IsFailed) 
                Close(completed: false);
        }

        private bool MatchesActiveContract(CargoBox cargo)
        {
            if (cargo == null) 
                return false;

            CargoTypeDefinition truth = cargo.Identity.TrueType;

            if (truth == _active.Contract.Cargo) 
                return true;

            Debug.Log(
                $"[Контракты] Сдан груз не по заказу: внутри {Name(truth)}, " +
                $"а заказан {Name(_active.Contract.Cargo)}. Заказчик это не считает", cargo);
            return false;
        }

        private static string Name(CargoTypeDefinition type) =>
            type != null ? type.DisplayName : "неизвестно что";

        private void Close(bool completed)
        {
            ContractProgress finished = _active;
            _active = default;

            if (completed) 
                _bus.Publish(new ContractCompleted(finished));
            else 
                _bus.Publish(new ContractFailed(finished));
        }
    }
}

using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Core.Events;
using CrateExpectations.Economy;
using CrateExpectations.Inspection;
using CrateExpectations.Inspection.Events;
using CrateExpectations.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.Contracts.Tests
{
    public sealed class ContractManagerTests
    {
        private const int Reward = 200;
        private const int Penalty = 150;
        private const int CleanBonus = 50;
        private const int StartingBalance = 500;

        private EventBus _bus;
        private CargoInventory _inventory;
        private EconomyService _economy;
        private ContractManager _manager;

        private CargoTypeDefinition _rum;
        private CargoTypeDefinition _spices;

        private ContractDefinition _rumRun;  
        private ContractDefinition _spiceRun;  
        private ContractCatalogDefinition _catalog;

        private readonly List<ContractAccepted> _accepted = new();
        private readonly List<ContractProgressed> _progressed = new();
        private readonly List<ContractCompleted> _completed = new();
        private readonly List<ContractFailed> _failed = new();

        private readonly List<GameObject> _crates = new();

        [SetUp]
        public void SetUp()
        {
            _rum = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _spices = ScriptableObject.CreateInstance<CargoTypeDefinition>();

            _rumRun = Contract("Ром", _rum, crates: 2, allowedSeizures: 0);
            _spiceRun = Contract("Специи", _spices, crates: 1, allowedSeizures: 1);
            _catalog = Catalog(_rumRun, _spiceRun);

            _bus = new EventBus();
            _inventory = new CargoInventory();
            _economy = new EconomyService(new EconomyRules(StartingBalance, 400), _bus);
            _manager = new ContractManager(_catalog, new PayoutCalculator(), _economy, _inventory, _bus);

            _accepted.Clear();
            _progressed.Clear();
            _completed.Clear();
            _failed.Clear();

            _bus.Subscribe<ContractAccepted>(e => _accepted.Add(e));
            _bus.Subscribe<ContractProgressed>(e => _progressed.Add(e));
            _bus.Subscribe<ContractCompleted>(e => _completed.Add(e));
            _bus.Subscribe<ContractFailed>(e => _failed.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Dispose();

            for (int i = 0; i < _crates.Count; i++) 
                Object.DestroyImmediate(_crates[i]);

            _crates.Clear();

            Object.DestroyImmediate(_rumRun);
            Object.DestroyImmediate(_spiceRun);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_rum);
            Object.DestroyImmediate(_spices);
        }

        [Test]
        public void The_board_offers_what_the_catalog_holds()
        {
            Assert.That(_manager.Available.Count, Is.EqualTo(2));
            Assert.That(_manager.Active.IsActive, Is.False);
        }

        [Test]
        public void Accepting_a_contract_announces_it_and_makes_it_active()
        {
            Assert.That(_manager.Accept(_rumRun), Is.True);

            Assert.That(_manager.Active.Contract, Is.EqualTo(_rumRun));
            Assert.That(_manager.Active.Delivered, Is.Zero);
            Assert.That(_accepted.Count, Is.EqualTo(1));
            Assert.That(_accepted[0].Progress.Contract, Is.EqualTo(_rumRun));
        }

        [Test]
        public void Only_one_contract_runs_at_a_time()
        {
            _manager.Accept(_rumRun);

            Assert.That(_manager.CanAccept(_spiceRun), Is.False);
            Assert.That(_manager.Accept(_spiceRun), Is.False);
            Assert.That(_manager.Active.Contract, Is.EqualTo(_rumRun));
            Assert.That(_accepted.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_contract_without_cargo_is_never_offered()
        {
            ContractDefinition broken = Contract("Пустой", cargo: null, crates: 1, allowedSeizures: 0);

            Assert.That(_manager.CanAccept(broken), Is.False);
            Assert.That(_manager.Accept(broken), Is.False);

            Object.DestroyImmediate(broken);
        }

        [Test]
        public void A_cleared_delivery_pays_and_moves_the_contract_forward()
        {
            _manager.Accept(_rumRun);

            Deliver(Crate(_rum), Cleared());

            Assert.That(_manager.Active.Delivered, Is.EqualTo(1));
            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance + Reward + CleanBonus));
            Assert.That(_progressed.Count, Is.EqualTo(1));
            Assert.That(_progressed[0].Seized, Is.False);
        }

        [Test]
        public void Cargo_that_is_not_what_was_ordered_neither_pays_nor_counts()
        {
            _manager.Accept(_rumRun);

            Deliver(Crate(_spices), Cleared());

            Assert.That(_manager.Active.Delivered, Is.Zero);
            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance));
            Assert.That(_progressed, Is.Empty);
        }

        [Test]
        public void Cargo_that_is_not_what_was_ordered_still_lands_in_the_registry()
        {
            _manager.Accept(_rumRun);
            CargoBox stranger = Crate(_spices);

            Deliver(stranger, Cleared());

            Assert.That(_inventory.TryGet(stranger.GetInstanceID(), out CargoRecord record), Is.True);
            Assert.That(record.Standing, Is.EqualTo(CargoStanding.Delivered));
        }

        [Test]
        public void The_contract_closes_exactly_on_the_last_crate_asked_for()
        {
            _manager.Accept(_rumRun);

            Deliver(Crate(_rum), Cleared());
            Assert.That(_completed, Is.Empty, "заказ на два ящика не закрывается после первого");
            Assert.That(_manager.Active.IsActive, Is.True);

            Deliver(Crate(_rum), Cleared());

            Assert.That(_completed.Count, Is.EqualTo(1));
            Assert.That(_completed[0].Progress.Delivered, Is.EqualTo(2));
            Assert.That(_manager.Active.IsActive, Is.False, "закрытый заказ перестаёт быть активным");
        }

        [Test]
        public void Deliveries_after_the_contract_is_closed_are_not_paid_for()
        {
            _manager.Accept(_rumRun);
            Deliver(Crate(_rum), Cleared());
            Deliver(Crate(_rum), Cleared());
            int balanceAtClose = _economy.Balance;

            Deliver(Crate(_rum), Cleared());

            Assert.That(_economy.Balance, Is.EqualTo(balanceAtClose));
            Assert.That(_completed.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_seizure_costs_the_penalty_and_still_counts_as_progress()
        {
            _manager.Accept(_spiceRun);

            Deliver(Crate(_spices), Busted());

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance - Penalty));
            Assert.That(_manager.Active.Seized, Is.EqualTo(1));
            Assert.That(_manager.Active.Delivered, Is.Zero);
            Assert.That(_progressed.Count, Is.EqualTo(1));
            Assert.That(_progressed[0].Seized, Is.True);
            Assert.That(_failed, Is.Empty, "одно изъятие этот заказчик терпит");
        }

        [Test]
        public void One_seizure_too_many_fails_the_contract()
        {
            _manager.Accept(_rumRun);

            Deliver(Crate(_rum), Busted());

            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_failed[0].Progress.Seized, Is.EqualTo(1));
            Assert.That(_manager.Active.IsActive, Is.False);
            Assert.That(_completed, Is.Empty);
        }

        [Test]
        public void A_crate_already_settled_is_not_paid_for_twice()
        {
            _manager.Accept(_rumRun);
            CargoBox crate = Crate(_rum);

            Deliver(crate, Cleared());
            int balanceAfterFirst = _economy.Balance;

            Deliver(crate, Cleared());

            Assert.That(_economy.Balance, Is.EqualTo(balanceAfterFirst));
            Assert.That(_manager.Active.Delivered, Is.EqualTo(1));
            Assert.That(_progressed.Count, Is.EqualTo(1));
        }

        [Test]
        public void An_inspection_without_a_contract_updates_the_registry_but_pays_nothing()
        {
            CargoBox crate = Crate(_rum);

            Deliver(crate, Cleared());

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance));
            Assert.That(_progressed, Is.Empty);
            Assert.That(_inventory.DeliveredCount, Is.EqualTo(1));
        }

        [Test]
        public void A_clean_delivery_earns_the_bonus_and_a_scrutinised_one_does_not()
        {
            _manager.Accept(_rumRun);

            Deliver(Crate(_rum), Cleared());
            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance + Reward + CleanBonus));

            Deliver(Crate(_rum), ClearedWithClues());
            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance + 2 * Reward + CleanBonus));
        }

        [Test]
        public void A_disposed_manager_stops_listening_to_inspections()
        {
            _manager.Accept(_rumRun);
            _manager.Dispose();

            Deliver(Crate(_rum), Cleared());

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance));
            Assert.That(_progressed, Is.Empty);
        }

        // вспомогательное

        private void Deliver(CargoBox crate, Verdict verdict) =>
            _bus.Publish(new CargoInspected(crate, null, verdict));

        private static Verdict Cleared() =>
            new(VerdictOutcome.Pass, 0f, 5f, System.Array.Empty<Clue>());

        private static Verdict ClearedWithClues() =>
            new(VerdictOutcome.Pass, 2f, 5f, new[] { new Clue(ClueType.PaintMismatch, 2f) });

        private static Verdict Busted() =>
            new(VerdictOutcome.Bust, 7f, 5f, new[] { new Clue(ClueType.PaintMismatch, 7f) });

        private CargoBox Crate(CargoTypeDefinition trueType)
        {
            var go = new GameObject("Crate");
            _crates.Add(go);

            CargoBox box = go.AddComponent<CargoBox>();
            box.AssignIdentity(trueType);
            _inventory.Register(box.GetInstanceID(), box.Identity, box.State);
            return box;
        }

        private static ContractDefinition Contract(
            string name, CargoTypeDefinition cargo, int crates, int allowedSeizures)
        {
            var contract = ScriptableObject.CreateInstance<ContractDefinition>();
            var so = new SerializedObject(contract);
            so.FindProperty("<DisplayName>k__BackingField").stringValue = name;
            so.FindProperty("<Cargo>k__BackingField").objectReferenceValue = cargo;
            so.FindProperty("<Crates>k__BackingField").intValue = crates;
            so.FindProperty("<AllowedSeizures>k__BackingField").intValue = allowedSeizures;
            so.FindProperty("<RewardPerCrate>k__BackingField").intValue = Reward;
            so.FindProperty("<CleanBonus>k__BackingField").intValue = CleanBonus;
            so.FindProperty("<Penalty>k__BackingField").intValue = Penalty;
            so.ApplyModifiedPropertiesWithoutUndo();

            return contract;
        }

        private static ContractCatalogDefinition Catalog(params ContractDefinition[] contracts)
        {
            var catalog = ScriptableObject.CreateInstance<ContractCatalogDefinition>();
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_contracts");
            list.arraySize = contracts.Length;

            for (int i = 0; i < contracts.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = contracts[i];

            so.ApplyModifiedPropertiesWithoutUndo();

            return catalog;
        }
    }
}

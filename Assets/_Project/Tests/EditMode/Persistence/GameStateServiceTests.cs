using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Core.Events;
using CrateExpectations.Economy;
using CrateExpectations.Inventory;
using CrateExpectations.Persistence.Events;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrateExpectations.Persistence.Tests
{
    public sealed class GameStateServiceTests
    {
        private const int StartingBalance = 500;
        private const int DebtLimit = 400;

        private FakeSaveService _storage;
        private FakeCargoCatalog _catalog;
        private SaveSlotDefinition _slot;
        private CargoRegistryDefinition _registry;

        private EventBus _bus;
        private CargoInventory _inventory;
        private EconomyService _economy;
        private ContractManager _contracts;
        private CargoSceneKeeper _cargo;
        private GameStateService _state;

        private ContractDefinition _rumRun;
        private ContractCatalogDefinition _contractCatalog;
        private CargoTypeDefinition _rum;

        private readonly List<GameSaved> _saved = new();
        private readonly List<GameLoaded> _loaded = new();
        private readonly List<GameStateFailed> _failed = new();

        [SetUp]
        public void SetUp()
        {
            _rum = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _rumRun = Contract("Contract_RumRun", _rum);
            _contractCatalog = Catalog(_rumRun);

            _slot = ScriptableObject.CreateInstance<SaveSlotDefinition>();
            _registry = ScriptableObject.CreateInstance<CargoRegistryDefinition>();

            _storage = new FakeSaveService();
            _catalog = new FakeCargoCatalog();

            _bus = new EventBus();
            _inventory = new CargoInventory();
            _economy = new EconomyService(new EconomyRules(StartingBalance, DebtLimit), _bus);
            _contracts = new ContractManager(
                _contractCatalog, new PayoutCalculator(), _economy, _inventory, _bus);
            _cargo = new CargoSceneKeeper(_catalog, _registry, _bus);

            _state = new GameStateService(
                _storage, _slot, _economy, _contracts, _inventory, _cargo, _bus);

            _saved.Clear();
            _loaded.Clear();
            _failed.Clear();

            _bus.Subscribe<GameSaved>(e => _saved.Add(e));
            _bus.Subscribe<GameLoaded>(e => _loaded.Add(e));
            _bus.Subscribe<GameStateFailed>(e => _failed.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            _contracts.Dispose();
            _cargo.Dispose();

            Object.DestroyImmediate(_contractCatalog);
            Object.DestroyImmediate(_rumRun);
            Object.DestroyImmediate(_rum);
            Object.DestroyImmediate(_registry);
            Object.DestroyImmediate(_slot);
        }

        [Test]
        public void Saving_writes_the_slot_once_and_stamps_the_format_version()
        {
            Assert.That(Run(_state.SaveAsync()), Is.True);

            var written = _storage.Peek<GameSnapshot>(_slot.Key);

            Assert.That(_storage.Writes, Is.EqualTo(1));
            Assert.That(written, Is.Not.Null, "записали не в тот слот");
            Assert.That(written.Version, Is.EqualTo(GameSnapshot.CurrentVersion));
            Assert.That(_saved.Count, Is.EqualTo(1), "игроку сказали, что сохранились");
        }

        [Test]
        public void A_saved_shift_comes_back_with_its_money_and_its_contract()
        {
            _contracts.Accept(_rumRun);
            Earn(250);

            Assert.That(Run(_state.SaveAsync()), Is.True);

            Earn(-1000);
            _contracts.Restore(default);

            Assert.That(Run(_state.LoadAsync()), Is.True);

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance + 250));
            Assert.That(_contracts.Active.Contract, Is.EqualTo(_rumRun));
            Assert.That(_loaded.Count, Is.EqualTo(1));
            Assert.That(_failed, Is.Empty);
        }

        [Test]
        public void A_debt_is_restored_as_a_debt_and_not_clamped_to_zero()
        {
            Earn(-(StartingBalance + 100));
            Run(_state.SaveAsync());

            Earn(1000);
            Run(_state.LoadAsync());

            Assert.That(_economy.Balance, Is.EqualTo(-100));
        }

        [Test]
        public void Contract_progress_comes_back_crate_by_crate()
        {
            Seed(new ContractSnapshot
            {
                ContractId = _rumRun.name,
                Delivered = 2,
                Seized = 1,
            });

            Assert.That(Run(_state.LoadAsync()), Is.True);

            Assert.That(_contracts.Active.Contract, Is.EqualTo(_rumRun));
            Assert.That(_contracts.Active.Delivered, Is.EqualTo(2));
            Assert.That(_contracts.Active.Seized, Is.EqualTo(1));
        }

        [Test]
        public void Loading_an_empty_slot_leaves_the_shift_exactly_as_it_was()
        {
            _contracts.Accept(_rumRun);
            Earn(250);

            Assert.That(Run(_state.LoadAsync()), Is.False);

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance + 250));
            Assert.That(_contracts.Active.Contract, Is.EqualTo(_rumRun));
            Assert.That(_loaded, Is.Empty);
            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_failed[0].WasSaving, Is.False);
        }

        [Test]
        public void A_save_from_another_version_is_refused_whole()
        {
            _storage.Seed(_slot.Key, new GameSnapshot
            {
                Version = GameSnapshot.CurrentVersion + 1,
                Economy = new EconomySnapshot { Balance = 99999 },
            });

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Загрузка отменена"));

            Assert.That(Run(_state.LoadAsync()), Is.False);

            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance), "мир остался прежним");
            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_loaded, Is.Empty);
        }

        [Test]
        public void A_contract_that_left_the_catalog_leaves_the_player_free_rather_than_crashing()
        {
            Seed(new ContractSnapshot { ContractId = "Contract_Gone", Delivered = 1 });

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Contract_Gone"));

            Assert.That(Run(_state.LoadAsync()), Is.True);

            Assert.That(_contracts.Active.IsActive, Is.False);
            Assert.That(_contracts.CanAccept(_rumRun), Is.True);
        }

        [Test]
        public void An_unavailable_storage_costs_a_message_and_not_the_session()
        {
            _storage.FailsOnWrite = true;

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Записать не вышло"));

            Assert.That(Run(_state.SaveAsync()), Is.False);

            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_failed[0].WasSaving, Is.True);
            Assert.That(_saved, Is.Empty);
            Assert.That(_economy.Balance, Is.EqualTo(StartingBalance));
        }

        [Test]
        public void An_empty_slot_is_reported_as_nothing_to_load()
        {
            Assert.That(Run(_state.HasSaveAsync()), Is.False);

            Run(_state.SaveAsync());

            Assert.That(Run(_state.HasSaveAsync()), Is.True);
        }

        private static T Run<T>(UniTask<T> task) => task.GetAwaiter().GetResult();

        private void Earn(int amount) =>
            _economy.Apply(new PayoutResult(amount, new[] { new PayoutLine(PayoutReason.Delivery, amount) }));

        private void Seed(in ContractSnapshot contract) => _storage.Seed(_slot.Key, new GameSnapshot
        {
            Version = GameSnapshot.CurrentVersion,
            Economy = new EconomySnapshot { Balance = StartingBalance },
            Contract = contract,
        });

        private static ContractDefinition Contract(string assetName, CargoTypeDefinition cargo)
        {
            var contract = ScriptableObject.CreateInstance<ContractDefinition>();
            contract.name = assetName;

            var so = new SerializedObject(contract);
            so.FindProperty("<DisplayName>k__BackingField").stringValue = "Ромовый рейс";
            so.FindProperty("<Cargo>k__BackingField").objectReferenceValue = cargo;
            so.FindProperty("<Crates>k__BackingField").intValue = 3;
            so.FindProperty("<AllowedSeizures>k__BackingField").intValue = 1;
            so.FindProperty("<RewardPerCrate>k__BackingField").intValue = 200;
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

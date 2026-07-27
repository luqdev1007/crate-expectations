using CrateExpectations.Cargo;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Inventory.Tests
{
    public sealed class CargoInventoryTests
    {
        private const int RumCrate = 101;
        private const int SpiceCrate = 202;

        private CargoInventory _inventory;
        private int _changedCount;

        private CargoTypeDefinition _rum;
        private CargoTypeDefinition _spices;
        private PaintDefinition _navy;

        private CargoIdentity _rumInside;
        private CargoState _asRum;
        private CargoState _asSpices;

        [SetUp]
        public void SetUp()
        {
            _rum = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _spices = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _navy = ScriptableObject.CreateInstance<PaintDefinition>();

            _rumInside = new CargoIdentity(_rum);
            _asRum = CargoState.Undisguised(_rumInside);
            _asSpices = new CargoState(_navy, null, _spices);

            _inventory = new CargoInventory();
            _changedCount = 0;
            _inventory.Changed += () => _changedCount++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rum);
            Object.DestroyImmediate(_spices);
            Object.DestroyImmediate(_navy);
        }

        [Test]
        public void A_registered_crate_lands_on_the_dock()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);

            Assert.That(_inventory.OnDockCount, Is.EqualTo(1));
            Assert.That(_inventory.Records.Count, Is.EqualTo(1));
            Assert.That(_inventory.TryGet(RumCrate, out CargoRecord record), Is.True);
            Assert.That(record.Truth.TrueType, Is.EqualTo(_rum));
            Assert.That(record.IsOnDock, Is.True);
            Assert.That(record.IsDisguised, Is.False);
        }

        [Test]
        public void Registering_the_same_crate_twice_does_not_duplicate_it()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);
            _inventory.Register(RumCrate, _rumInside, _asRum);

            Assert.That(_inventory.Records.Count, Is.EqualTo(1));
            Assert.That(_inventory.OnDockCount, Is.EqualTo(1));
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void A_repainted_crate_keeps_its_truth_and_changes_only_what_it_declares()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);

            _inventory.Redeclare(RumCrate, _asSpices);

            Assert.That(_inventory.TryGet(RumCrate, out CargoRecord record), Is.True);
            Assert.That(record.Truth.TrueType, Is.EqualTo(_rum), "истину маскировкой не переписать");
            Assert.That(record.Declared.DeclaredType, Is.EqualTo(_spices));
            Assert.That(record.IsDisguised, Is.True);
            Assert.That(record.IsOnDock, Is.True);
        }

        [Test]
        public void Redeclaring_the_very_same_state_is_not_a_change()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);
            _changedCount = 0;

            _inventory.Redeclare(RumCrate, _asRum);

            Assert.That(_changedCount, Is.Zero);
        }

        [Test]
        public void An_unknown_crate_is_ignored_rather_than_invented()
        {
            _inventory.Redeclare(SpiceCrate, _asSpices);
            _inventory.Settle(SpiceCrate, CargoStanding.Delivered);

            Assert.That(_inventory.Records, Is.Empty);
            Assert.That(_inventory.DeliveredCount, Is.Zero);
            Assert.That(_changedCount, Is.Zero);
        }

        [Test]
        public void A_delivered_crate_leaves_the_dock_and_counts_as_delivered()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);

            _inventory.Settle(RumCrate, CargoStanding.Delivered);

            Assert.That(_inventory.OnDockCount, Is.Zero);
            Assert.That(_inventory.DeliveredCount, Is.EqualTo(1));
            Assert.That(_inventory.SeizedCount, Is.Zero);
        }

        [Test]
        public void A_seized_crate_counts_as_seized()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);

            _inventory.Settle(RumCrate, CargoStanding.Seized);

            Assert.That(_inventory.OnDockCount, Is.Zero);
            Assert.That(_inventory.SeizedCount, Is.EqualTo(1));
            Assert.That(_inventory.DeliveredCount, Is.Zero);
        }

        [Test]
        public void A_crate_whose_fate_is_settled_cannot_be_settled_again()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);
            _inventory.Settle(RumCrate, CargoStanding.Delivered);
            _changedCount = 0;

            _inventory.Settle(RumCrate, CargoStanding.Delivered);
            _inventory.Settle(RumCrate, CargoStanding.Seized);

            Assert.That(_inventory.DeliveredCount, Is.EqualTo(1));
            Assert.That(_inventory.SeizedCount, Is.Zero);
            Assert.That(_inventory.OnDockCount, Is.Zero);
            Assert.That(_changedCount, Is.Zero);
        }

        [Test]
        public void Records_keep_the_order_in_which_crates_appeared()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);
            _inventory.Register(SpiceCrate, new CargoIdentity(_spices), _asSpices);

            _inventory.Redeclare(RumCrate, _asSpices);

            Assert.That(_inventory.Records[0].Id, Is.EqualTo(RumCrate));
            Assert.That(_inventory.Records[1].Id, Is.EqualTo(SpiceCrate));
        }

        [Test]
        public void Clearing_forgets_everything()
        {
            _inventory.Register(RumCrate, _rumInside, _asRum);
            _inventory.Register(SpiceCrate, new CargoIdentity(_spices), _asSpices);
            _inventory.Settle(RumCrate, CargoStanding.Seized);

            _inventory.Clear();

            Assert.That(_inventory.Records, Is.Empty);
            Assert.That(_inventory.OnDockCount, Is.Zero);
            Assert.That(_inventory.DeliveredCount, Is.Zero);
            Assert.That(_inventory.SeizedCount, Is.Zero);
            Assert.That(_inventory.TryGet(RumCrate, out _), Is.False);
        }
    }
}

using System;
using CrateExpectations.Core.Hands;
using NUnit.Framework;

namespace CrateExpectations.Core.Tests
{
    /// <summary>
    /// Правила занятости рук. Ровно то, ради чего <see cref="HandsState"/> сделан обычным
    /// C#-классом: проверять их в play mode пришлось бы перебором из ящика, листка и сабли
    /// во всех сочетаниях, причём половина сочетаний руками не воспроизводится -
    /// например, «груз в руках И поднятый листок».
    /// </summary>
    public sealed class HandsStateTests
    {
        private sealed class FakeCarry : ICarryStateSource
        {
            public bool IsCarrying { get; set; }
        }

        private sealed class FakeWeapon : IWeaponStateSource
        {
            public bool IsWeaponDrawn { get; set; }
            public bool IsBusy { get; set; }
        }

        private sealed class FakeContract : IContractViewSource
        {
            public bool IsRaised { get; set; }
        }

        private FakeCarry _carry;
        private FakeWeapon _weapon;
        private FakeContract _contract;
        private HandsState _hands;

        [SetUp]
        public void SetUp()
        {
            _carry = new FakeCarry();
            _weapon = new FakeWeapon();
            _contract = new FakeContract();
            _hands = new HandsState(_carry, _weapon, _contract);
        }

        // --- вычисление занятости ---

        [Test]
        public void Empty_hands_are_free()
        {
            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Free));
        }

        [Test]
        public void Each_source_on_its_own_names_its_own_occupancy()
        {
            _carry.IsCarrying = true;
            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Carrying));

            _carry.IsCarrying = false;
            _contract.IsRaised = true;
            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Reading));

            _contract.IsRaised = false;
            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Combat));
        }

        [Test]
        public void Carrying_outranks_everything_else()
        {
            _carry.IsCarrying = true;
            _contract.IsRaised = true;
            _weapon.IsWeaponDrawn = true;
            _weapon.IsBusy = true;

            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Carrying));
        }

        [Test]
        public void Reading_outranks_combat()
        {
            _contract.IsRaised = true;
            _weapon.IsWeaponDrawn = true;
            _weapon.IsBusy = true;

            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Reading));
        }

        [Test]
        public void A_charge_held_with_the_weapon_away_still_counts_as_combat()
        {
            // Кнопку удара можно зажать и с убранной саблей: машина состояний при этом
            // в Sheathed, но руки уже заняты - игрок целится приёмом
            _weapon.IsWeaponDrawn = false;
            _weapon.IsBusy = true;

            Assert.That(_hands.Occupancy, Is.EqualTo(HandsOccupancy.Combat));
        }

        // --- запросы ---

        [Test]
        public void Grabbing_needs_completely_empty_hands()
        {
            Assert.That(_hands.CanGrab, Is.True);

            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.CanGrab, Is.False, "взяли ящик с саблей в руке");

            _weapon.IsWeaponDrawn = false;
            _contract.IsRaised = true;
            Assert.That(_hands.CanGrab, Is.False, "взяли ящик с листком в руке");
        }

        [Test]
        public void Only_a_carried_crate_can_be_dropped_or_thrown()
        {
            Assert.That(_hands.CanDropOrThrow, Is.False);

            _carry.IsCarrying = true;
            Assert.That(_hands.CanDropOrThrow, Is.True);
        }

        [Test]
        public void Attacking_works_empty_handed_and_with_the_weapon_out()
        {
            Assert.That(_hands.CanAttack, Is.True, "удар пустыми руками");

            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.CanAttack, Is.True, "удар с саблей в руке");
        }

        [Test]
        public void Attacking_is_refused_with_full_hands()
        {
            _carry.IsCarrying = true;
            Assert.That(_hands.CanAttack, Is.False, "удар с ящиком в руках");

            _carry.IsCarrying = false;
            _contract.IsRaised = true;
            Assert.That(_hands.CanAttack, Is.False, "удар с поднятым листком");
        }

        [Test]
        public void The_contract_sheet_cannot_be_raised_while_carrying()
        {
            _carry.IsCarrying = true;

            Assert.That(_hands.CanRaiseContract, Is.False);
        }

        [Test]
        public void The_contract_sheet_needs_free_hands_and_nothing_less()
        {
            Assert.That(_hands.CanRaiseContract, Is.True);

            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.CanRaiseContract, Is.False, "листок поднялся поверх сабли");
        }

        [Test]
        public void The_weapon_can_be_toggled_empty_handed_and_while_already_armed()
        {
            Assert.That(_hands.CanToggleWeapon, Is.True);

            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.CanToggleWeapon, Is.True, "саблю нельзя убрать обратно");

            _weapon.IsWeaponDrawn = false;
            _carry.IsCarrying = true;
            Assert.That(_hands.CanToggleWeapon, Is.False, "саблю достали с ящиком в руках");
        }

        [Test]
        public void Reaching_out_is_blocked_only_while_the_drawn_weapon_is_mid_action()
        {
            Assert.That(_hands.CanReachOut, Is.True, "пустыми руками не дотянулись");

            _weapon.IsWeaponDrawn = true;
            Assert.That(_hands.CanReachOut, Is.True, "с опущенной саблей не дотянулись");

            _weapon.IsBusy = true;
            Assert.That(_hands.CanReachOut, Is.False, "дотянулись посреди удара");
        }

        [Test]
        public void A_crate_or_a_sheet_in_hand_does_not_stop_reaching_out()
        {
            // Гейт на E - про занятость ДЕЙСТВИЕМ, а не про занятость рук: положить ящик
            // на станцию нужно уметь именно с ящиком в руках
            _carry.IsCarrying = true;
            _contract.IsRaised = true;

            Assert.That(_hands.CanReachOut, Is.True);
        }

        // --- проводка ---

        [Test]
        public void A_missing_source_is_a_wiring_error_and_says_so_at_once()
        {
            // Молча подставить "ничем не занято" значило бы получить модель, которая
            // всегда разрешает всё, и искать незакрытый гейт уже руками в игре
            Assert.Throws<ArgumentNullException>(() => new HandsState(null, _weapon, _contract));
            Assert.Throws<ArgumentNullException>(() => new HandsState(_carry, null, _contract));
            Assert.Throws<ArgumentNullException>(() => new HandsState(_carry, _weapon, null));
        }
    }
}

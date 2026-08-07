using System.Collections.Generic;
using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    public sealed class WeaponStateMachineTests
    {
        private const float Draw = 0.4f;
        private const float Sheathe = 0.4f;
        private const float Attack = 0.6f;

        private static WeaponStateMachine Machine() =>
            new(new WeaponTimings(Draw, Sheathe, Attack));

        /// <summary>Достаёт оружие и доводит машину до боевой стойки</summary>
        private static WeaponStateMachine Armed()
        {
            WeaponStateMachine machine = Machine();
            machine.ToggleWeapon();
            machine.Tick(Draw);
            return machine;
        }

        [Test]
        public void Starts_sheathed_with_the_weapon_hidden()
        {
            WeaponStateMachine machine = Machine();

            Assert.That(machine.State, Is.EqualTo(WeaponState.Sheathed));
            Assert.That(machine.IsWeaponVisible, Is.False);
        }

        [Test]
        public void Toggling_from_sheathed_starts_drawing()
        {
            WeaponStateMachine machine = Machine();

            machine.ToggleWeapon();

            Assert.That(machine.State, Is.EqualTo(WeaponState.Drawing));
        }

        [Test]
        public void The_weapon_appears_halfway_through_the_draw_not_at_its_start()
        {
            WeaponStateMachine machine = Machine();
            machine.ToggleWeapon();

            machine.Tick(Draw * 0.5f - 0.01f);
            Assert.That(machine.IsWeaponVisible, Is.False, "показалось раньше середины");

            machine.Tick(0.02f);
            Assert.That(machine.IsWeaponVisible, Is.True, "не показалось на середине");
        }

        [Test]
        public void Drawing_ends_in_ready_once_the_draw_duration_has_passed()
        {
            WeaponStateMachine machine = Machine();
            machine.ToggleWeapon();

            machine.Tick(Draw - 0.01f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Drawing));

            machine.Tick(0.02f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void A_sheathed_weapon_cannot_attack()
        {
            WeaponStateMachine machine = Machine();

            machine.Attack();

            Assert.That(machine.State, Is.EqualTo(WeaponState.Sheathed));
        }

        [Test]
        public void Attacking_returns_to_ready_after_the_attack_duration()
        {
            WeaponStateMachine machine = Armed();

            machine.Attack();
            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            machine.Tick(Attack - 0.01f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            machine.Tick(0.02f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void A_second_press_during_a_swing_is_swallowed()
        {
            WeaponStateMachine machine = Armed();
            machine.Attack();

            machine.Tick(Attack * 0.5f);
            machine.Attack();

            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            // Второе нажатие не должно было и продлить взмах
            machine.Tick(Attack * 0.5f + 0.01f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void The_weapon_cannot_be_put_away_mid_swing()
        {
            WeaponStateMachine machine = Armed();
            machine.Attack();

            machine.ToggleWeapon();

            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));
            Assert.That(machine.IsWeaponVisible, Is.True);
        }

        [Test]
        public void Toggling_from_ready_puts_the_weapon_away_halfway_through()
        {
            WeaponStateMachine machine = Armed();

            machine.ToggleWeapon();
            Assert.That(machine.State, Is.EqualTo(WeaponState.Sheathing));
            Assert.That(machine.IsWeaponVisible, Is.True, "исчезло в начале убирания");

            machine.Tick(Sheathe * 0.5f + 0.01f);
            Assert.That(machine.IsWeaponVisible, Is.False);

            machine.Tick(Sheathe * 0.5f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Sheathed));
        }

        [Test]
        public void Every_state_change_is_reported_once_and_in_order()
        {
            WeaponStateMachine machine = Machine();
            List<WeaponState> seen = new();
            machine.StateChanged += state => seen.Add(state);

            machine.ToggleWeapon();
            machine.Tick(Draw);
            machine.Attack();
            machine.Tick(Attack);
            machine.ToggleWeapon();
            machine.Tick(Sheathe);

            Assert.That(seen, Is.EqualTo(new[]
            {
                WeaponState.Drawing,
                WeaponState.Ready,
                WeaponState.Attacking,
                WeaponState.Ready,
                WeaponState.Sheathing,
                WeaponState.Sheathed,
            }));
        }

        [Test]
        public void Visibility_is_reported_only_when_it_actually_changes()
        {
            WeaponStateMachine machine = Machine();
            List<bool> seen = new();
            machine.WeaponVisibilityChanged += visible => seen.Add(visible);

            machine.ToggleWeapon();
            machine.Tick(Draw);
            machine.Attack();
            machine.Tick(Attack);

            Assert.That(seen, Is.EqualTo(new[] { true }));
        }
    }
}

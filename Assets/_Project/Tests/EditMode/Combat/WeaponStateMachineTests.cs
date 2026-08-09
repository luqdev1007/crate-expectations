using System.Collections.Generic;
using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    public sealed class WeaponStateMachineTests
    {
        private const float Draw = 0.4f;
        private const float Sheathe = 0.4f;
        private const float Attack = 0.6f;

        /// <summary>
        /// Точка отмены за пределами удара: приём, который нельзя прервать ничем.
        /// Значение по умолчанию для тестов, которым отмена не интересна
        /// </summary>
        private const float NoCancel = 1f;

        private static WeaponStateMachine Machine() =>
            new(new WeaponTimings(Draw, Sheathe));

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

            machine.Attack(Attack, NoCancel);

            Assert.That(machine.State, Is.EqualTo(WeaponState.Sheathed));
        }

        [Test]
        public void Attacking_returns_to_ready_after_the_attack_duration()
        {
            WeaponStateMachine machine = Armed();

            machine.Attack(Attack, NoCancel);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            machine.Tick(Attack - 0.01f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            machine.Tick(0.02f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void An_attack_that_did_not_start_says_so()
        {
            WeaponStateMachine machine = Machine();

            Assert.That(machine.Attack(Attack, NoCancel), Is.False, "убранное оружие ударило");
            Assert.That(Armed().Attack(Attack, NoCancel), Is.True, "готовое оружие не ударило");
        }

        [Test]
        public void Each_attack_runs_for_its_own_duration_not_a_shared_one()
        {
            WeaponStateMachine machine = Armed();

            machine.Attack(0.2f, NoCancel);
            machine.Tick(0.21f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready), "короткий приём не кончился вовремя");

            machine.Attack(1.2f, NoCancel);
            machine.Tick(0.21f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking), "длинный приём кончился в темпе короткого");
        }

        [Test]
        public void The_recovery_of_an_attack_can_be_cut_short_by_the_next_one()
        {
            const float cancelAfter = 0.7f;

            WeaponStateMachine machine = Armed();
            machine.Attack(Attack, cancelAfter);

            machine.Tick(Attack * cancelAfter - 0.01f);
            Assert.That(machine.CanAttack, Is.False, "удар отменился до своего окна отмены");

            machine.Tick(0.02f);
            Assert.That(machine.CanAttack, Is.True, "удар не отменяется в своём окне отмены");

            // Отмена - это новый удар с начала, а не продолжение старого
            Assert.That(machine.Attack(Attack, cancelAfter), Is.True);
            Assert.That(machine.AttackProgress, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void A_second_press_during_a_swing_is_swallowed()
        {
            WeaponStateMachine machine = Armed();
            machine.Attack(Attack, NoCancel);

            machine.Tick(Attack * 0.5f);
            machine.Attack(Attack, NoCancel);

            Assert.That(machine.State, Is.EqualTo(WeaponState.Attacking));

            // Второе нажатие не должно было и продлить взмах
            machine.Tick(Attack * 0.5f + 0.01f);
            Assert.That(machine.State, Is.EqualTo(WeaponState.Ready));
        }

        [Test]
        public void The_weapon_cannot_be_put_away_mid_swing()
        {
            WeaponStateMachine machine = Armed();
            machine.Attack(Attack, NoCancel);

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
            machine.Attack(Attack, NoCancel);
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
            machine.Attack(Attack, NoCancel);
            machine.Tick(Attack);

            Assert.That(seen, Is.EqualTo(new[] { true }));
        }
    }
}

using CrateExpectations.Core.Input;
using NUnit.Framework;

namespace CrateExpectations.Core.Tests
{
    /// <summary>
    /// Удержание кнопки - общий механизм для заряженного удара и для замаха броском.
    /// Проверяется тестом, а не секундомером в play mode, по той же причине, что и буфер
    /// ввода: это счёт времени, и ошибка в нём выглядит не как баг, а как «что-то не то
    /// с ощущением».
    /// <para>
    /// Тесты идут двумя раскладками, потому что раскладки ровно две: боевая - без мёртвой
    /// зоны, где любое нажатие уже удар; бросковая - с мёртвой зоной, где короткий тап
    /// обязан не сделать вообще ничего
    /// </para>
    /// </summary>
    public sealed class HoldChargeTests
    {
        private const float Duration = 0.25f;

        // Числа броска из CarryDefinition: порог активации и время набора замаха
        private const float Activation = 0.18f;
        private const float WindupDuration = 0.7f;

        private static HoldCharge Combat()
        {
            var charge = new HoldCharge();
            charge.Begin(Duration, 0f);

            return charge;
        }

        private static HoldCharge Windup()
        {
            var charge = new HoldCharge();
            charge.Begin(WindupDuration, Activation);

            return charge;
        }

        // --- общее поведение таймера ---

        [Test]
        public void A_fresh_charge_is_not_charging_anything()
        {
            var charge = new HoldCharge();

            Assert.That(charge.IsHolding, Is.False);
            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Tick(1f), Is.False, "заряд без нажатия сам себя не набрал");
        }

        [Test]
        public void The_full_charge_is_reported_exactly_once()
        {
            HoldCharge charge = Combat();

            Assert.That(charge.Tick(Duration - 0.01f), Is.False, "полный заряд сработал раньше времени");
            Assert.That(charge.Tick(0.01f), Is.True, "полный заряд не сработал ровно на своём значении");

            // Второй раз - это второй удар с одного нажатия. Кнопка всё ещё зажата,
            // и без этой отсечки приём уходил бы каждый кадр
            Assert.That(charge.Tick(1f), Is.False, "полный заряд сработал повторно на зажатой кнопке");
        }

        [Test]
        public void Reaching_full_does_not_end_the_hold_by_itself()
        {
            // Ровно то, ради чего механизм общий: удар с полного заряда уходит сам,
            // а замах броском на единице ЖДЁТ отпускания. Решает это владелец,
            // а не счётчик времени
            HoldCharge charge = Windup();
            charge.Tick(Activation + WindupDuration + 0.01f);

            Assert.That(charge.IsHolding, Is.True);
            Assert.That(charge.IsCharging, Is.True);
            Assert.That(charge.ChargeT, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Releasing_early_gives_back_what_was_actually_held()
        {
            HoldCharge charge = Combat();
            charge.Tick(0.1f);

            Assert.That(charge.Release(), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(charge.IsHolding, Is.False);
        }

        [Test]
        public void Releasing_a_charge_that_was_already_taken_gives_nothing()
        {
            // Так владелец защищается от второго удара с одного нажатия: заряд,
            // ушедший в удар на пороге, снят, и отпускание кнопки уже ничего не значит
            HoldCharge charge = Combat();
            charge.Tick(Duration);
            charge.Release();

            Assert.That(charge.Release(), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void A_zero_duration_is_full_the_moment_it_starts()
        {
            // Так ведёт себя направление без заряда, если его всё же зарядить:
            // ждать нечего, и первый же кадр обязан отдать удар
            var charge = new HoldCharge();
            charge.Begin(0f, 0f);

            Assert.That(charge.Tick(0f), Is.True);
            Assert.That(charge.ChargeT, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Pressing_again_starts_the_hold_from_zero()
        {
            HoldCharge charge = Combat();
            charge.Tick(0.2f);
            charge.Begin(Duration, 0f);

            Assert.That(charge.Held, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(charge.Tick(0.2f), Is.False, "новое нажатие досчитало заряд со старого");
        }

        // --- мёртвая зона: раскладка броска ---

        [Test]
        public void A_tap_shorter_than_the_dead_zone_never_starts_the_windup()
        {
            HoldCharge charge = Windup();
            charge.Tick(Activation - 0.01f);

            Assert.That(charge.IsHolding, Is.True, "кнопка отпущена сама собой");
            Assert.That(charge.IsCharging, Is.False, "замах пошёл внутри мёртвой зоны");
            Assert.That(charge.ChargeT, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void The_windup_starts_exactly_at_the_dead_zone_edge()
        {
            HoldCharge charge = Windup();
            charge.Tick(Activation);

            Assert.That(charge.IsCharging, Is.True);

            // Именно ноль, а не «чуть больше»: заряд считается ОТ порога, и бросок
            // на самой границе обязан быть тем же, что бросок без замаха
            Assert.That(charge.ChargeT, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void The_charge_ramps_over_its_duration_and_clamps_at_one()
        {
            HoldCharge charge = Windup();

            charge.Tick(Activation + WindupDuration * 0.5f);
            Assert.That(charge.ChargeT, Is.EqualTo(0.5f).Within(1e-4f), "середина заряда посчитана не от порога");

            charge.Tick(WindupDuration * 0.5f + 0.01f);
            Assert.That(charge.ChargeT, Is.EqualTo(1f).Within(1e-5f));

            charge.Tick(10f);
            Assert.That(charge.ChargeT, Is.EqualTo(1f).Within(1e-5f), "заряд перевалил за единицу");
        }

        [Test]
        public void The_charge_survives_release_so_the_owner_can_still_read_it()
        {
            // Иначе владельцу пришлось бы снимать заряд ДО отпускания и помнить
            // об этом порядке - ровно тот вид знания, который однажды теряется
            HoldCharge charge = Windup();
            charge.Tick(Activation + WindupDuration + 0.01f);
            charge.Release();

            Assert.That(charge.IsHolding, Is.False);
            Assert.That(charge.ChargeT, Is.EqualTo(1f).Within(1e-5f));
        }

        // --- отмена ---

        [Test]
        public void Cancelling_drops_the_hold_without_firing()
        {
            HoldCharge charge = Combat();
            charge.Tick(0.1f);
            charge.Cancel();

            Assert.That(charge.IsHolding, Is.False);
            Assert.That(charge.Tick(1f), Is.False, "погашенный заряд всё-таки выстрелил");
            Assert.That(charge.Release(), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Cancelling_mid_windup_leaves_nothing_behind()
        {
            HoldCharge charge = Windup();
            charge.Tick(Activation + WindupDuration * 0.6f);
            charge.Cancel();

            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Held, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(charge.ChargeT, Is.EqualTo(0f).Within(1e-5f), "от отменённого замаха остался хвост");
        }

        [Test]
        public void The_next_hold_after_a_cancel_starts_from_scratch()
        {
            HoldCharge charge = Windup();
            charge.Tick(Activation + WindupDuration + 0.01f);
            charge.Cancel();

            charge.Begin(WindupDuration, Activation);
            charge.Tick(Activation - 0.01f);

            Assert.That(charge.IsCharging, Is.False, "новый замах унаследовал заряд отменённого");
        }
    }
}

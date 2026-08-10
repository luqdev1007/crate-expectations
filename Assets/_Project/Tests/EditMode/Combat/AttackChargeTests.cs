using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Удержание кнопки удара. Проверяется тестом, а не секундомером в play mode,
    /// по той же причине, что и буфер ввода: это счёт времени, и ошибка в нём
    /// выглядит не как баг, а как "что-то не то с ощущением"
    /// </summary>
    public sealed class AttackChargeTests
    {
        private const float Threshold = 0.25f;

        private static AttackCharge Charging()
        {
            var charge = new AttackCharge();
            charge.Begin(Threshold);

            return charge;
        }

        [Test]
        public void A_fresh_charge_is_not_charging_anything()
        {
            var charge = new AttackCharge();

            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Tick(1f), Is.False, "заряд без нажатия сам себя не набрал");
        }

        [Test]
        public void The_threshold_fires_exactly_once()
        {
            AttackCharge charge = Charging();

            Assert.That(charge.Tick(Threshold - 0.01f), Is.False, "порог сработал раньше времени");
            Assert.That(charge.Tick(0.01f), Is.True, "порог не сработал ровно на своём значении");

            // Второй раз - это второй удар с одного нажатия. Кнопка всё ещё зажата,
            // и без этой отсечки приём уходил бы каждый кадр
            Assert.That(charge.Tick(1f), Is.False, "порог сработал повторно на зажатой кнопке");
        }

        [Test]
        public void Releasing_early_gives_back_what_was_actually_held()
        {
            AttackCharge charge = Charging();
            charge.Tick(0.1f);

            Assert.That(charge.Release(), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(charge.IsCharging, Is.False);
        }

        [Test]
        public void Releasing_after_the_charge_already_fired_gives_nothing()
        {
            // Иначе отпускание кнопки после полного заряда клало бы в буфер второй удар -
            // тот самый, который уже ушёл
            AttackCharge charge = Charging();
            charge.Tick(Threshold);

            Assert.That(charge.Release(), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void A_zero_threshold_is_full_the_moment_it_starts()
        {
            // Так ведёт себя направление без заряда, если его всё же зарядить:
            // ждать нечего, и первый же кадр обязан отдать удар
            var charge = new AttackCharge();
            charge.Begin(0f);

            Assert.That(charge.Tick(0f), Is.True);
        }

        [Test]
        public void Clearing_drops_the_hold_without_firing()
        {
            AttackCharge charge = Charging();
            charge.Tick(0.1f);
            charge.Clear();

            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Tick(1f), Is.False, "погашенный заряд всё-таки выстрелил");
            Assert.That(charge.Release(), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Pressing_again_starts_the_hold_from_zero()
        {
            AttackCharge charge = Charging();
            charge.Tick(0.2f);
            charge.Begin(Threshold);

            Assert.That(charge.Held, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(charge.Tick(0.2f), Is.False, "новое нажатие досчитало заряд со старого");
        }
    }
}

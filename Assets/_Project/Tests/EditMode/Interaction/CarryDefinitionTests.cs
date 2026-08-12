using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Interaction.Tests
{
    /// <summary>
    /// Перевод заряда замаха в силу броска и в глубину оттяжки. Числа живут в ассете,
    /// но САМ ПЕРЕВОД - это правило, и проверять его перебрасыванием ящиков в play mode
    /// значило бы мерить дальность полёта на глаз.
    /// <para>
    /// Ассет здесь создаётся пустой, со значениями по умолчанию из кода: тест проверяет
    /// не «сила равна 18», а «на полном заряде уходит ровно то, что записано в
    /// ChargedThrowForce», - иначе он ломался бы каждый раз, когда автор подкручивает число
    /// </para>
    /// </summary>
    public sealed class CarryDefinitionTests
    {
        private CarryDefinition _definition;

        [SetUp]
        public void SetUp() => _definition = ScriptableObject.CreateInstance<CarryDefinition>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_definition);

        [Test]
        public void An_uncharged_throw_is_exactly_the_plain_throw_force()
        {
            // Ровно тот бросок, который был в игре до всякого замаха: заряд ничего
            // не отнимает, он только добавляет
            Assert.That(
                _definition.ThrowForceAt(0f),
                Is.EqualTo(_definition.ThrowForce).Within(1e-4f));
        }

        [Test]
        public void A_full_charge_is_exactly_the_charged_throw_force()
        {
            Assert.That(
                _definition.ThrowForceAt(1f),
                Is.EqualTo(_definition.ChargedThrowForce).Within(1e-4f));
        }

        [Test]
        public void A_charged_throw_is_stronger_than_an_uncharged_one()
        {
            Assert.That(_definition.ThrowForceAt(1f), Is.GreaterThan(_definition.ThrowForceAt(0f)));
            Assert.That(_definition.ThrowForceAt(0.5f), Is.GreaterThan(_definition.ThrowForceAt(0f)));
            Assert.That(_definition.ThrowForceAt(0.5f), Is.LessThan(_definition.ThrowForceAt(1f)));
        }

        [Test]
        public void A_charge_outside_zero_to_one_cannot_produce_a_throw_of_unknown_strength()
        {
            // Кривую автор правит руками, и вылет за края не должен превращаться
            // ни в бросок сильнее заряженного, ни в бросок слабее обычного
            Assert.That(
                _definition.ThrowForceAt(-3f),
                Is.EqualTo(_definition.ThrowForce).Within(1e-4f));

            Assert.That(
                _definition.ThrowForceAt(3f),
                Is.EqualTo(_definition.ChargedThrowForce).Within(1e-4f));
        }

        [Test]
        public void Without_a_charge_the_hold_point_does_not_move_at_all()
        {
            // Это и есть «отменённый замах не оставил остаточной силы» со стороны картинки:
            // заряд снят, ChargeT снова ноль - и точка удержания там же, где была всегда
            Assert.That(_definition.WindupOffsetAt(0f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void A_full_charge_pulls_the_hold_point_by_the_whole_offset()
        {
            Vector3 offset = _definition.WindupOffsetAt(1f);

            Assert.That(offset.x, Is.EqualTo(_definition.WindupOffset.x).Within(1e-4f));
            Assert.That(offset.y, Is.EqualTo(_definition.WindupOffset.y).Within(1e-4f));
            Assert.That(offset.z, Is.EqualTo(_definition.WindupOffset.z).Within(1e-4f));
        }

        [Test]
        public void The_windup_lifts_the_cargo_up()
        {
            // Знак важнее величины. Раньше замах уводил груз вниз-назад, к плечу, - и это
            // оказалось ошибкой: за камерой груза не видно, и заряд читался только по тому,
            // что ящик пропал из кадра. Подъём виден весь, от начала до конца
            Assert.That(_definition.WindupOffset.y, Is.GreaterThan(0f), "замах не поднимает груз");
        }

        [Test]
        public void The_windup_never_brings_the_cargo_closer_to_the_eye()
        {
            // Груз на замахе обязан оставаться не ближе, чем висел: подтяжка к камере -
            // это путь в ближнюю плоскость отсечения, и увидел бы её игрок ровно в тот
            // момент, когда целится
            float hold = _definition.HoldDistance;
            Vector3 charged = new Vector3(0f, 0f, hold) + _definition.WindupOffsetAt(1f);

            Assert.That(charged.magnitude, Is.GreaterThanOrEqualTo(hold - 1e-4f),
                "полный замах подтягивает груз к глазу");
        }
    }
}

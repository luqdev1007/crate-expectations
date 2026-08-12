using CrateExpectations.Player.View;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Player.Tests
{
    /// <summary>
    /// Насколько глаз повторяет качку груди. Числа подбирают в игре, правила - нет:
    /// "нулевой вес возвращает неподвижную камеру", "сильнее тела камеру не качает",
    /// "телепорт не доигрывает старую качку на новом месте" держатся здесь.
    /// </summary>
    public sealed class EyeBobFilterTests
    {
        private const float Dt = 0.02f;

        private EyeBobFilter _bob;

        [SetUp]
        public void SetUp() => _bob = new EyeBobFilter();

        /// <summary>Без сглаживания: вес проверяем отдельно от доезда к нему</summary>
        private Vector3 Instant(Vector3 deviation, float follow) =>
            _bob.Tick(deviation, follow, 0f, Dt);

        [Test]
        public void A_body_standing_perfectly_still_does_not_move_the_eye()
        {
            Assert.That(Instant(Vector3.zero, 1f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Full_follow_puts_the_eye_exactly_where_the_chest_went()
        {
            Vector3 deviation = new Vector3(0.01f, 0.06f, 0.03f);

            Assert.That(Instant(deviation, 1f), Is.EqualTo(deviation));
        }

        [Test]
        public void Zero_follow_is_the_old_motionless_camera()
        {
            // Ноль обязан возвращать прежнее поведение целиком - это путь отката,
            // а не «почти не качает»
            Assert.That(Instant(new Vector3(0.01f, 0.06f, 0.03f), 0f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Half_follow_is_half_the_bob()
        {
            Vector3 half = Instant(new Vector3(0f, 0.08f, 0f), 0.5f);

            Assert.That(half.y, Is.EqualTo(0.04f).Within(1e-5f));
        }

        [Test]
        public void The_camera_is_never_shaken_harder_than_the_body_itself()
        {
            // Вес подбирают руками, и значение больше единицы означало бы камеру,
            // которую качает сильнее, чем грудь, - качку из ниоткуда
            Vector3 deviation = new Vector3(0f, 0.06f, 0f);

            Assert.That(Instant(deviation, 4f), Is.EqualTo(deviation));
        }

        [Test]
        public void A_negative_follow_does_not_invert_the_bob()
        {
            Assert.That(Instant(new Vector3(0f, 0.06f, 0f), -1f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Smoothing_does_not_arrive_in_a_single_frame()
        {
            Vector3 afterOneFrame = _bob.Tick(new Vector3(0f, 0.08f, 0f), 1f, 0.05f, Dt);

            Assert.That(afterOneFrame.y, Is.GreaterThan(0f));
            Assert.That(afterOneFrame.y, Is.LessThan(0.08f));
        }

        [Test]
        public void Smoothing_gets_there_eventually()
        {
            for (int frame = 0; frame < 60; frame++)
                _bob.Tick(new Vector3(0f, 0.08f, 0f), 1f, 0.05f, Dt);

            Assert.That(_bob.Current.y, Is.EqualTo(0.08f).Within(0.001f));
        }

        [Test]
        public void A_teleported_player_does_not_finish_the_old_bob_at_the_new_place()
        {
            for (int frame = 0; frame < 60; frame++)
                _bob.Tick(new Vector3(0f, 0.08f, 0f), 1f, 0.05f, Dt);

            _bob.Reset();

            Assert.That(_bob.Current, Is.EqualTo(Vector3.zero));

            // Сброс обязан гасить и накопленную скорость сглаживания, иначе первый же
            // кадр после него продолжил бы прежний доезд
            Assert.That(_bob.Tick(Vector3.zero, 1f, 0.05f, Dt), Is.EqualTo(Vector3.zero));
        }
    }
}

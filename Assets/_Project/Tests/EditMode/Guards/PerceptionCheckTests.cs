using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Guards.Tests
{
    /// <summary>
    /// Геометрия восприятия - единственное, что <see cref="PerceptionCheck"/> считает сам,
    /// и единственное в фазе F, что вообще можно посчитать без сцены. Тесты стерегут
    /// три вещи, которые ломаются молча: слух не должен обрасти направлением, зрение -
    /// потерять либо угол, либо дальность, а конус - незаметно расплющиться в сектор.
    /// <para>
    /// Числа в тестах свои, а не из ассета: тест проверяет правило, а не текущий баланс,
    /// и не должен краснеть от того, что дизайнер подвинул радиус слуха
    /// </para>
    /// </summary>
    public sealed class PerceptionCheckTests
    {
        private static readonly Vector3 Guard = Vector3.zero;

        [Test]
        public void A_scuffle_inside_the_hearing_radius_is_heard()
        {
            Assert.That(PerceptionCheck.CanHear(Guard, new Vector3(0f, 0f, 5.9f), radius: 6f),
                        Is.True);
        }

        [Test]
        public void A_scuffle_beyond_the_hearing_radius_is_not()
        {
            Assert.That(PerceptionCheck.CanHear(Guard, new Vector3(0f, 0f, 6.1f), radius: 6f),
                        Is.False);
        }

        /// <summary>
        /// Граница включительна: ровно на радиусе - слышно. Проверяется не ради
        /// педантизма, а чтобы правило было одно и записанное, а не «как получится»
        /// </summary>
        [Test]
        public void Exactly_on_the_edge_still_counts_as_heard()
        {
            Assert.That(PerceptionCheck.CanHear(Guard, new Vector3(0f, 0f, 6f), radius: 6f),
                        Is.True);
        }

        /// <summary>
        /// Главное свойство слуха и причина, по которой он отдельный метод, а не зрение
        /// с широким конусом: направление ему безразлично. Драка за спиной слышна ровно
        /// так же, как драка перед носом
        /// </summary>
        [Test]
        public void Hearing_does_not_care_which_way_the_guard_is_facing()
        {
            Assert.That(PerceptionCheck.CanHear(Guard, new Vector3(0f, 0f, -5f), radius: 6f),
                        Is.True);
        }

        /// <summary>Выключенный слух не слышит даже драки под самым носом</summary>
        [Test]
        public void A_zero_radius_hears_nothing_at_all()
        {
            Assert.That(PerceptionCheck.CanHear(Guard, Guard, radius: 0f), Is.False);
        }

        [Test]
        public void What_falls_inside_the_vision_cone_is_seen()
        {
            Vector3 incident = Quaternion.Euler(0f, 45f, 0f) * Vector3.forward * 5f;

            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, incident, range: 10f, fovFullDegrees: 100f),
                        Is.True);
        }

        /// <summary>
        /// Пара к предыдущему, и пара намеренная: дистанция у обеих точек ОДНА И ТА ЖЕ.
        /// Разойтись они могут только по углу - иначе тест доказывал бы дальность,
        /// а не конус
        /// </summary>
        [Test]
        public void What_falls_outside_the_cone_is_not_seen_at_the_same_distance()
        {
            Vector3 incident = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward * 5f;

            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, incident, range: 10f, fovFullDegrees: 100f),
                        Is.False);
        }

        [Test]
        public void Straight_ahead_and_within_range_is_seen()
        {
            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, new Vector3(0f, 0f, 9.5f),
                            range: 10f, fovFullDegrees: 100f),
                        Is.True);
        }

        /// <summary>
        /// Зеркало предыдущего: точка по центру конуса, угол идеальный - и всё равно
        /// не видно, потому что далеко. Вместе эти два теста показывают, что проверок
        /// именно две и что ни одна не подменяет другую
        /// </summary>
        [Test]
        public void Straight_ahead_but_too_far_is_not_seen()
        {
            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, new Vector3(0f, 0f, 10.5f),
                            range: 10f, fovFullDegrees: 100f),
                        Is.False);
        }

        [Test]
        public void Nothing_behind_the_guard_is_seen()
        {
            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, new Vector3(0f, 0f, -3f),
                            range: 10f, fovFullDegrees: 100f),
                        Is.False);
        }

        /// <summary>
        /// Конус - настоящий конус, а не плоский сектор: то, что ровно над головой,
        /// в него не попадает, хотя по горизонтали стоит в самом центре взгляда.
        /// Без этого теста проверка угла однажды «оптимизировалась» бы до плоской
        /// и молча начала бы засчитывать всё, что сверху
        /// </summary>
        [Test]
        public void The_cone_is_a_cone_and_not_a_flat_sector()
        {
            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, new Vector3(0f, 5f, 0f),
                            range: 10f, fovFullDegrees: 100f),
                        Is.False);
        }

        /// <summary>Слепой стражник не видит ничего, даже упёршись взглядом</summary>
        [Test]
        public void A_zero_range_sees_nothing_at_all()
        {
            Assert.That(PerceptionCheck.CanSee(
                            Guard, Vector3.forward, new Vector3(0f, 0f, 1f),
                            range: 0f, fovFullDegrees: 100f),
                        Is.False);
        }
    }
}

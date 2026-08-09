using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Единственное правило, которое <see cref="AttackDefinition"/> считает сам, -
    /// с какого момента приём разрешено прервать следующим. Считается оно от ДОВОДКИ,
    /// а не от всего приёма, и это ровно та арифметика, которую легко перепутать
    /// при правке и невозможно заметить глазами в play mode
    /// </summary>
    public sealed class AttackDefinitionTests
    {
        private const float SweepEnd = 0.5f;

        /// <summary>
        /// Приём с заданным концом активного окна. Поле закрыто на запись намеренно -
        /// его правят в инспекторе, - поэтому тест кладёт значение тем же способом,
        /// что и инспектор
        /// </summary>
        private static AttackDefinition Attack(float sweepEnd)
        {
            AttackDefinition attack = ScriptableObject.CreateInstance<AttackDefinition>();

            var serialized = new SerializedObject(attack);
            serialized.FindProperty("<SweepEnd>k__BackingField").floatValue = sweepEnd;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return attack;
        }

        [Test]
        public void A_zero_cancel_window_means_the_attack_has_to_be_watched_to_its_end()
        {
            Assert.That(Attack(SweepEnd).CancelAfter(0f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void A_full_cancel_window_opens_the_moment_the_active_window_closes()
        {
            Assert.That(Attack(SweepEnd).CancelAfter(1f), Is.EqualTo(SweepEnd).Within(1e-5f));
        }

        [Test]
        public void The_window_is_measured_off_the_recovery_not_off_the_whole_attack()
        {
            // Доводка здесь - половина приёма, треть от неё - 0.15: отмена в 0.85,
            // а не в 0.7, как вышло бы при отсчёте от всего приёма
            Assert.That(Attack(SweepEnd).CancelAfter(0.3f), Is.EqualTo(0.85f).Within(1e-5f));
        }

        [Test]
        public void The_moment_of_contact_can_never_be_cancelled_away()
        {
            // Число за границами 0..1 - это опечатка в ассете, и она не должна
            // позволять отменять удар до того, как он попал
            Assert.That(Attack(SweepEnd).CancelAfter(5f), Is.GreaterThanOrEqualTo(SweepEnd));
            Assert.That(Attack(SweepEnd).CancelAfter(-5f), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void An_attack_that_stays_active_to_its_last_frame_cannot_be_cancelled_at_all()
        {
            // Доводки нет - отменять нечего, какое окно ни поставь
            Assert.That(Attack(1f).CancelAfter(1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Attack(1f).CancelAfter(0.3f), Is.EqualTo(1f).Within(1e-5f));
        }
    }
}

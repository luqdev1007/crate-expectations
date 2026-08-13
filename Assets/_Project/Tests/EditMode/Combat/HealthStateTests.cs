using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Арифметика здоровья вся на границах, и именно они ломаются молча. Главная -
    /// удар по уже мёртвому: один взмах сабли задевает несколько коллайдеров одной цели
    /// и проверяется до восьми раз за окно, поэтому «умер» обязано взвестись ровно один
    /// раз. Заметить это в play mode нельзя - смерть просто отыграется дважды поверх себя
    /// </summary>
    public sealed class HealthStateTests
    {
        private const float MaxHp = 60f;

        [Test]
        public void A_fresh_body_starts_at_full_health()
        {
            var health = new HealthState(MaxHp);

            Assert.That(health.CurrentHp, Is.EqualTo(MaxHp).Within(1e-5f));
            Assert.That(health.MaxHp, Is.EqualTo(MaxHp).Within(1e-5f));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void An_ordinary_hit_takes_its_damage_off_and_nothing_more()
        {
            var health = new HealthState(MaxHp);

            DamageResult result = health.ApplyDamage(20f, AttackTier.Light);

            Assert.That(result.NewHp, Is.EqualTo(40f).Within(1e-5f));
            Assert.That(result.Died, Is.False);
            Assert.That(health.CurrentHp, Is.EqualTo(40f).Within(1e-5f));
        }

        [Test]
        public void The_hit_that_empties_the_bar_is_the_one_that_reports_the_death()
        {
            var health = new HealthState(MaxHp);
            health.ApplyDamage(MaxHp - 5f, AttackTier.Light);

            DamageResult killing = health.ApplyDamage(5f, AttackTier.Heavy);

            Assert.That(killing.NewHp, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(killing.Died, Is.True);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void Hitting_a_corpse_changes_nothing_and_does_not_report_a_second_death()
        {
            var health = new HealthState(MaxHp);
            health.ApplyDamage(MaxHp, AttackTier.Heavy);

            DamageResult again = health.ApplyDamage(30f, AttackTier.Heavy);

            Assert.That(again.Died, Is.False, "смерть объявлена второй раз");
            Assert.That(again.NewHp, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(health.CurrentHp, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Overkill_stops_at_zero_instead_of_running_into_the_negatives()
        {
            // Отрицательное здоровье означало бы, что следующий удар обязан «долечить»
            // цель до нуля, прежде чем она умрёт второй раз
            var health = new HealthState(MaxHp);

            DamageResult result = health.ApplyDamage(MaxHp * 10f, AttackTier.Heavy);

            Assert.That(result.NewHp, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(result.Died, Is.True);
            Assert.That(health.CurrentHp, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Damage_that_would_heal_is_treated_as_no_damage_at_all()
        {
            // Отрицательный урон - это опечатка в данных, а не механика лечения
            var health = new HealthState(MaxHp);

            DamageResult result = health.ApplyDamage(-25f, AttackTier.Light);

            Assert.That(result.NewHp, Is.EqualTo(MaxHp).Within(1e-5f));
            Assert.That(health.CurrentHp, Is.EqualTo(MaxHp).Within(1e-5f));
        }
    }
}

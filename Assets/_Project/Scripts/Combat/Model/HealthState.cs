using System;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Здоровье как чистая логика: сколько осталось и умер ли от этого удара.
    /// Тот же паттерн, что <c>GuardBrain</c> и <c>ClueEvaluator</c> - ни сцены,
    /// ни <c>MonoBehaviour</c>, ни собственной случайности, поэтому всё поведение
    /// на границах закрывается edit-mode тестами.
    /// <para>
    /// Главное правило здесь одно и оно не косметическое: <b>по мёртвому не бьют</b>.
    /// Один взмах сабли способен задеть несколько коллайдеров одной цели, а свип
    /// идёт до восьми раз за окно - без этого правила добивающий удар объявил бы
    /// о смерти дважды, и все подписчики отыграли бы её дважды тоже
    /// </para>
    /// </summary>
    public sealed class HealthState
    {
        /// <param name="maxHp">
        /// Полное здоровье. Приходит из <c>HealthDefinition</c>, а не числом из кода
        /// </param>
        public HealthState(float maxHp)
        {
            MaxHp = Math.Max(0f, maxHp);
            CurrentHp = MaxHp;
        }

        /// <summary>Полное здоровье</summary>
        public float MaxHp { get; }

        /// <summary>Сколько осталось. Ниже нуля не опускается</summary>
        public float CurrentHp { get; private set; }

        /// <summary>Здоровье кончилось</summary>
        public bool IsDead => CurrentHp <= 0f;

        /// <summary>Принять урон</summary>
        /// <param name="amount">
        /// Сколько снять. Отрицательное значение трактуется как ноль: «урон»,
        /// который лечит, - это опечатка в данных, а не механика
        /// </param>
        /// <param name="tier">
        /// Насколько тяжёлым был удар. Само здоровье его сейчас не учитывает - тир
        /// нужен реакции, а не арифметике, - но он часть контракта осознанно:
        /// брони и сопротивления тиру встанут именно сюда, и тогда сигнатура
        /// не поедет у всех вызывающих разом
        /// </param>
        public DamageResult ApplyDamage(float amount, AttackTier tier)
        {
            // По мёртвому не бьют: состояние не меняется, о смерти второй раз
            // не объявляется. Это и есть та самая защита от двойного события
            if (IsDead)
                return new DamageResult(CurrentHp, died: false);

            CurrentHp = Math.Max(0f, CurrentHp - Math.Max(0f, amount));

            return new DamageResult(CurrentHp, died: IsDead);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Решает, каким приёмом ответить на нажатие: переводит намерение игрока в
    /// направление и достаёт по нему приём из раскладки.
    /// <para>
    /// Обычный C#-класс: правила выбора - это логика, а не поведение объекта сцены,
    /// и проверять их надо тестом, а не тыканьем клавиш в play mode. Про
    /// <see cref="MonoBehaviour"/>, ввод и аниматор он не знает ничего - ему дают
    /// вектор и флаг, он отдаёт приём.
    /// </para>
    /// <para>
    /// Счётчик вариаций живёт здесь же и по одному на направление. Общий счётчик на всех
    /// означал бы, что удар влево переключает вариацию удара вправо: пять раз шагнув
    /// вправо, игрок получил бы пять разных ударов вместо честного чередования двух.
    /// </para>
    /// </summary>
    public sealed class AttackSelector
    {
        /// <summary>
        /// Порог, с которого клавиша считается нажатой. Не ноль, потому что композит
        /// Input System отдаёт диагональ нормализованной: W+A - это (-0.71, 0.71),
        /// а не (-1, 1). Порог обязан быть заметно ниже 0.71 и заметно выше дребезга
        /// </summary>
        private const float PressThreshold = 0.3f;

        private static readonly int DirectionCount =
            System.Enum.GetValues(typeof(AttackDirection)).Length;

        private readonly IAttackTable _table;
        private readonly int[] _variation;

        public AttackSelector(IAttackTable table)
        {
            _table = table;
            _variation = new int[DirectionCount];
        }

        /// <summary>
        /// Куда игрок собирался идти. Отдельно от <see cref="Select"/> и статически -
        /// это чистое правило без состояния, и тест на него не должен собирать таблицу
        /// приёмов.
        /// <para>
        /// Два приоритета. Прыжок важнее всего: в воздухе игрок уже не «идёт влево»,
        /// он падает, и удар должен быть тем самым, который отыгрывает падение.
        /// Диагональ разрешается в пользу боковой составляющей: W+A - это шаг в сторону
        /// с доворотом, и боковой удар читается на нём честнее, чем наступательный.
        /// </para>
        /// </summary>
        /// <param name="moveInput">Нажатые клавиши движения: X вправо, Y вперёд</param>
        /// <param name="airborne">Игрок не касается земли</param>
        public static AttackDirection ResolveDirection(Vector2 moveInput, bool airborne)
        {
            if (airborne)
                return AttackDirection.Air;

            if (Mathf.Abs(moveInput.x) >= PressThreshold)
                return moveInput.x > 0f ? AttackDirection.Right : AttackDirection.Left;

            if (Mathf.Abs(moveInput.y) >= PressThreshold)
                return moveInput.y > 0f ? AttackDirection.Forward : AttackDirection.Backward;

            return AttackDirection.Neutral;
        }

        /// <summary>
        /// Приём на это нажатие. Возвращает <c>null</c>, только если бить нечем вообще -
        /// то есть если пуст и запрошенный ряд раскладки, и <see cref="AttackDirection.Neutral"/>
        /// </summary>
        public AttackDefinition Select(Vector2 moveInput, bool airborne) =>
            Select(ResolveDirection(moveInput, airborne));

        /// <summary>Приём на готовое направление, без заряда</summary>
        public AttackDefinition Select(AttackDirection direction) => Select(direction, 0f);

        /// <summary>
        /// Приём на готовое направление с учётом того, сколько игрок продержал кнопку.
        /// <para>
        /// Правило одно: из строки берутся приёмы, чей <see cref="AttackDefinition.HoldTime"/>
        /// уже набран, и из них - САМАЯ ВЫСОКАЯ ступень. То есть короткое нажатие всегда
        /// отдаёт незаряженный вариант, а полностью набранное удержание - заряженный.
        /// Чередование вариаций при этом остаётся ВНУТРИ ступени: две вариации лёгкого
        /// укола продолжат сменять друг друга, и заряженный приём в это чередование
        /// не влезет.
        /// </para>
        /// </summary>
        /// <param name="direction">Куда бьём</param>
        /// <param name="heldTime">Сколько кнопка была зажата к моменту удара, с</param>
        public AttackDefinition Select(AttackDirection direction, float heldTime)
        {
            AttackDefinition attack = Take(direction, heldTime);

            // Направление может быть не расписано - это нормальное состояние раскладки
            // на полпути, а не ошибка. Бить при этом игрок всё равно должен, и бьёт он
            // тем, что стоит на месте: удар стоя определён всегда
            if (attack == null && direction != AttackDirection.Neutral)
                attack = Take(AttackDirection.Neutral, heldTime);

            return attack;
        }

        /// <summary>
        /// Сколько надо держать кнопку, чтобы это направление отдало свой заряженный
        /// приём. Ноль - заряжать здесь нечего, и нажатие должно уходить в удар сразу.
        /// <para>
        /// Спрашивается на КАЖДОМ нажатии, ещё до начала удара: по этому числу решают,
        /// начинать удар прямо сейчас или сперва копить удержание. Поэтому берётся
        /// максимум по строке, а не ближайшая ступень: заряд считается набранным,
        /// когда доступен самый тяжёлый приём направления.
        /// </para>
        /// </summary>
        public float ChargeTime(AttackDirection direction)
        {
            IReadOnlyList<AttackDefinition> attacks = _table.Get(direction);

            if (attacks == null)
                return 0f;

            float longest = 0f;

            for (int i = 0; i < attacks.Count; i++)
                if (attacks[i] != null && attacks[i].HoldTime > longest)
                    longest = attacks[i].HoldTime;

            return longest;
        }

        /// <summary>
        /// Сбрасывает чередование. Нужен там, где серия закончилась: после убирания
        /// оружия следующий удар должен начинаться с первой вариации, а не с той,
        /// на которой прервались полчаса назад
        /// </summary>
        public void ResetVariations()
        {
            for (int i = 0; i < _variation.Length; i++)
                _variation[i] = 0;
        }

        private AttackDefinition Take(AttackDirection direction, float heldTime)
        {
            IReadOnlyList<AttackDefinition> attacks = _table.Get(direction);

            if (attacks == null || attacks.Count == 0)
                return null;

            float tier = HighestReachedTier(attacks, heldTime);

            // Вся строка заряженная, а заряда нет. Строка при этом не «пустая» -
            // отдавать её приём было бы неправильно, а падать на Neutral правильно
            if (tier < 0f)
                return null;

            int index = (int)direction;

            // Пустые ячейки в раскладке пропускаем, а не считаем вариацией: иначе
            // недозаполненная строка давала бы удары через раз. Чужая ступень заряда
            // пропускается тем же способом и по той же причине
            for (int i = 0; i < attacks.Count; i++)
            {
                int slot = (_variation[index] + i) % attacks.Count;
                AttackDefinition attack = attacks[slot];

                if (attack == null || !Mathf.Approximately(attack.HoldTime, tier))
                    continue;

                _variation[index] = (slot + 1) % attacks.Count;

                return attack;
            }

            return null;
        }

        /// <summary>
        /// Самая высокая ступень заряда, до которой игрок дотянул удержанием.
        /// Минус один - не дотянул ни до одной
        /// </summary>
        private static float HighestReachedTier(IReadOnlyList<AttackDefinition> attacks, float heldTime)
        {
            float tier = -1f;

            for (int i = 0; i < attacks.Count; i++)
            {
                AttackDefinition attack = attacks[i];

                if (attack != null && attack.HoldTime <= heldTime && attack.HoldTime > tier)
                    tier = attack.HoldTime;
            }

            return tier;
        }
    }
}

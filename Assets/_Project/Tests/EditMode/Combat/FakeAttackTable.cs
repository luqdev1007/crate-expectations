using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Раскладка на две строки для тестов выбора удара. Боевой <see cref="AttackSet"/>
    /// сюда не годится: он ассет, его правят под игру, и тест на правилах выбора начал бы
    /// падать от перестановки двух приёмов местами - то есть проверял бы содержимое
    /// раскладки вместо самих правил
    /// </summary>
    internal sealed class FakeAttackTable : IAttackTable
    {
        private readonly Dictionary<AttackDirection, AttackDefinition[]> _rows = new();

        /// <summary>
        /// Кладёт в строку приёмы с такими именами. Имя - единственное, чем приёмы
        /// в этих тестах отличаются друг от друга: остальные поля к выбору отношения не имеют
        /// </summary>
        public FakeAttackTable With(AttackDirection direction, params string[] names)
        {
            var attacks = new AttackDefinition[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                // Пустое имя - это дырка в раскладке, а не приём: селектор обязан её
                // перешагнуть, и без такой заглушки эту ветку не проверить
                if (string.IsNullOrEmpty(names[i]))
                    continue;

                attacks[i] = ScriptableObject.CreateInstance<AttackDefinition>();
                attacks[i].name = names[i];
            }

            _rows[direction] = attacks;

            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<AttackDefinition> Get(AttackDirection direction) =>
            _rows.TryGetValue(direction, out AttackDefinition[] attacks)
                ? attacks
                : System.Array.Empty<AttackDefinition>();
    }
}

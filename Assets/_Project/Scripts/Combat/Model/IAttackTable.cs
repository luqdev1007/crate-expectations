using System.Collections.Generic;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Откуда <see cref="AttackSelector"/> берёт приёмы. Интерфейс, а не сам
    /// <c>AttackSet</c>, ровно ради тестов: выбор направления - это правила, и проверять
    /// их надо на подставной таблице из двух строк, а не на боевом ассете, который
    /// придётся чинить после каждой правки раскладки
    /// </summary>
    public interface IAttackTable
    {
        /// <summary>
        /// Что положено на это направление. Пустой список - легальный ответ:
        /// направление может быть не расписано, и решать, чем его заменить,
        /// должен вызывающий, а не таблица
        /// </summary>
        IReadOnlyList<AttackDefinition> Get(AttackDirection direction);
    }
}

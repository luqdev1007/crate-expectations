using System;

namespace CrateExpectations.Economy
{
    /// <summary>
    /// Состояние кошелька для сохранения. Отдельный тип от <see cref="EconomyRules"/>:
    /// правила - это настройка смены, а снимок - то, чем смена кончилась.
    ///
    /// <para>Публичные поля, а не свойства, и обычная <c>struct</c>, а не <c>readonly</c>: это
    /// формат на диске, и его читает <c>JsonUtility</c>, который умеет только сериализуемые
    /// поля. Ровно поэтому доменные значения (<see cref="PayoutResult"/> и прочие) сюда
    /// не подставляются - у формата хранения своя жизнь и своя совместимость.</para>
    /// </summary>
    [Serializable]
    public struct EconomySnapshot
    {
        /// <summary>Баланс на момент сохранения. Может быть отрицательным - долг разрешён</summary>
        public int Balance;
    }
}

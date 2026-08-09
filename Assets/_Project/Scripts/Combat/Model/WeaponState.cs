namespace CrateExpectations.Combat
{
    /// <summary>
    /// Что сейчас происходит с оружием в руке. Промежуточные состояния существуют потому,
    /// что доставание и удар занимают время, в течение которого ввод игнорируется
    /// </summary>
    public enum WeaponState
    {
        /// <summary>Оружие убрано, тело в обычной стойке</summary>
        Sheathed,

        /// <summary>Достаёт: идёт переход в боевую стойку</summary>
        Drawing,

        /// <summary>Достал, боевая стойка, можно бить</summary>
        Ready,

        /// <summary>Идёт взмах</summary>
        Attacking,

        /// <summary>Убирает: идёт переход в обычную стойку</summary>
        Sheathing,

        /// <summary>Поднимает клинок в блок</summary>
        BlockRaise,

        /// <summary>Держит блок, пока зажата кнопка</summary>
        Blocking,

        /// <summary>Опускает клинок из блока</summary>
        BlockLower,
    }
}

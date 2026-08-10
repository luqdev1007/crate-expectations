namespace CrateExpectations.Core.Hands
{
    /// <summary>
    /// Чем заняты руки игрока. Ровно одно значение за раз: совпасть источники могут,
    /// а занятость - нет, и разрешает совпадения приоритет в <see cref="HandsState"/>
    /// </summary>
    public enum HandsOccupancy
    {
        /// <summary>Руки пусты: можно всё</summary>
        Free,

        /// <summary>В руках груз</summary>
        Carrying,

        /// <summary>В руках листок заказа</summary>
        Reading,

        /// <summary>В руке оружие</summary>
        Combat,
    }
}

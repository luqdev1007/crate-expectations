using CrateExpectations.Core.Hands;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Занятость рук числом для аниматора. Отдельный класс, а не приведение
    /// <see cref="HandsOccupancy"/> к <c>int</c>, по очень простой причине: порядок значений
    /// в перечислении задан ПРИОРИТЕТОМ (груз важнее листка, листок важнее сабли),
    /// а порядок здесь - удобством графа. Совпадать они не обязаны и уже не совпадают.
    /// <para>
    /// Числа объявлены здесь ровно один раз, и берут их отсюда оба конца: водитель, который
    /// пишет параметр в рантайме, и генератор графа, который ставит эти же числа в условия
    /// переходов. Разъехаться им негде - ровно как номерам приёмов, которые и граф,
    /// и рантайм берут из одного <c>AttackSet</c>.
    /// </para>
    /// </summary>
    public static class HandsAnimatorMode
    {
        /// <summary>Руки пусты</summary>
        public const int Free = 0;

        /// <summary>В руке оружие</summary>
        public const int Combat = 1;

        /// <summary>В руках груз</summary>
        public const int Carrying = 2;

        /// <summary>В руках листок заказа</summary>
        public const int Reading = 3;

        /// <summary>Перевести занятость в число для параметра аниматора</summary>
        public static int Of(HandsOccupancy occupancy)
        {
            switch (occupancy)
            {
                case HandsOccupancy.Combat: return Combat;
                case HandsOccupancy.Carrying: return Carrying;
                case HandsOccupancy.Reading: return Reading;
                default: return Free;
            }
        }
    }
}

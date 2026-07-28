namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Состояния инспектора. Отдельный enum, а не ссылки друг на друга: состояние называет,
    /// куда хочет перейти, а какой инстанс за этим стоит - знает только
    /// <see cref="InspectorAI"/>. Так состояния не знают друг о друге и остаются заменяемыми.
    /// </summary>
    public enum InspectorPhase
    {
        /// <summary>Прибирает за прошлым досмотром и решает, куда идти дальше</summary>
        Idle,

        /// <summary>Обходит пост по точкам, пока делать нечего</summary>
        Patrol,

        /// <summary>Заметил прохожего с грузом в руках и обернулся к нему</summary>
        Notice,

        /// <summary>Возвращается на пост после досмотра</summary>
        Return,

        /// <summary>Идёт к зоне досмотра</summary>
        Approach,

        /// <summary>Осматривает груз по шагам</summary>
        Examine,

        /// <summary>Оглашает исход</summary>
        Verdict,
    }
}

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Чем стражник занят. Это НАМЕРЕНИЕ, а не состояние машины: решение о том,
    /// чего стражник хочет, принимает <see cref="GuardBrain"/>, а как именно это
    /// выглядит - состояние FSM, которое ему соответствует
    /// </summary>
    public enum GuardIntent
    {
        /// <summary>Стоять на посту и никуда не идти</summary>
        HoldPost,

        /// <summary>Обходить маршрут с остановками</summary>
        Patrol,
    }
}

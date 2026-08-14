namespace CrateExpectations.Guards
{
    /// <summary>
    /// Всё, что <see cref="GuardBrain"/> знает о стражнике в момент решения.
    /// Тот же паттерн, что у <c>InspectionSubject</c> и <c>PayoutTerms</c>:
    /// неизменяемый снимок входных данных без единой ссылки на Unity-типы -
    /// именно поэтому решение можно проверить edit-mode тестом
    /// </summary>
    public readonly struct GuardContext
    {
        /// <param name="hasPatrolRoute">Назначен ли стражнику маршрут обхода</param>
        /// <param name="isStaggered">
        /// Не оправился ли от удара. Со значением по умолчанию намеренно: «не оглушён» -
        /// это базовый случай, и снимок, собранный без единого упоминания драки, обязан
        /// описывать спокойного стражника. Та же логика, что у <c>AttackTier.Light = 0</c>
        /// в блоке 2: значение по умолчанию - безопасное
        /// </param>
        public GuardContext(bool hasPatrolRoute, bool isStaggered = false)
        {
            HasPatrolRoute = hasPatrolRoute;
            IsStaggered = isStaggered;
        }

        /// <summary>
        /// Есть ли маршрут. Маршрут привязан к месту на доке, а не к типу стражника:
        /// назначили ссылку - стражник патрулирует, оставили пустой - стоит на посту
        /// </summary>
        public bool HasPatrolRoute { get; }

        /// <summary>
        /// Стражника только что ударили, и вздрагивание ещё играет. Флагом, а не
        /// оставшимся временем: сколько именно осталось - подробность того, кто это
        /// считает, а решению нужно только «уже можно идти или ещё нет»
        /// </summary>
        public bool IsStaggered { get; }
    }
}

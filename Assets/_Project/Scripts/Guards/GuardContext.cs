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
        public GuardContext(bool hasPatrolRoute) => HasPatrolRoute = hasPatrolRoute;

        /// <summary>
        /// Есть ли маршрут. Маршрут привязан к месту на доке, а не к типу стражника:
        /// назначили ссылку - стражник патрулирует, оставили пустой - стоит на посту
        /// </summary>
        public bool HasPatrolRoute { get; }
    }
}

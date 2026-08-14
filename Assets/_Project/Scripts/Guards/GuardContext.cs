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
        /// <param name="isDead">
        /// Здоровье кончилось. В обычной жизни ветка недостижима - <c>GuardDeath</c>
        /// гасит <see cref="GuardAI"/> целиком, и решение просто перестаёт запрашиваться.
        /// Флаг здесь не ради этого случая, а ради того, что <c>HealthComponent</c>
        /// смерть ПЕРЕЖИВАЕТ (фаза D §7.2): если объект однажды зажгут обратно,
        /// труп обязан остаться трупом, а не воскреснуть в погоню
        /// </param>
        /// <param name="isAggro">Стражник знает, что его бьют, и больше не занят обходом</param>
        /// <param name="isInAttackRange">Игрок ближе <c>AttackRange</c></param>
        /// <param name="isAttackCommitted">
        /// Удар уже начат и обязан доиграть. Это и есть коммит: без него стражник
        /// отменял бы собственный замах в тот кадр, когда игрок сделал шаг назад
        /// </param>
        public GuardContext(
            bool hasPatrolRoute,
            bool isStaggered = false,
            bool isDead = false,
            bool isAggro = false,
            bool isInAttackRange = false,
            bool isAttackCommitted = false)
        {
            HasPatrolRoute = hasPatrolRoute;
            IsStaggered = isStaggered;
            IsDead = isDead;
            IsAggro = isAggro;
            IsInAttackRange = isInAttackRange;
            IsAttackCommitted = isAttackCommitted;
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

        /// <summary>Здоровье кончилось</summary>
        public bool IsDead { get; }

        /// <summary>
        /// Стражник взведён и занят игроком. В этой фазе агро НЕ затухает: включившись,
        /// оно держится до смерти стражника. Убежать нельзя, и это решение, а не недоделка
        /// </summary>
        public bool IsAggro { get; }

        /// <summary>
        /// Игрок в пределах удара. Считается сравнением дистанции, а не физикой:
        /// вопрос «пора ли бить» - про расстояние, а не про касание объёмов
        /// </summary>
        public bool IsInAttackRange { get; }

        /// <summary>
        /// Удар начат и обязан доиграть. Единственный флаг, который стражник ставит
        /// себе САМ (его пишет <c>GuardAttackState</c>), - остальные приходят снаружи
        /// </summary>
        public bool IsAttackCommitted { get; }
    }
}

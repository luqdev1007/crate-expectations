namespace CrateExpectations.Cargo
{
    /// <summary>Чем закончилось применение рецепта</summary>
    public enum DisguiseOutcome
    {
        /// <summary>Состояние изменилось</summary>
        Applied,

        /// <summary>Рецепт применим, но ящик уже в этом состоянии - делать нечего</summary>
        AlreadyApplied,

        /// <summary>Рецепт применить нельзя, причина - в <see cref="DisguiseResult.Rejection"/></summary>
        Rejected,
    }

    /// <summary>Причина отказа. Текст для игрока подставляет слой представления, не ядро</summary>
    public enum DisguiseRejection
    {
        None,

        /// <summary>Рецепт не доукомплектован: у действия нет цели (краски/печати/содержимого)</summary>
        IncompleteRecipe,

        /// <summary>Не выполнено условие "ящик должен быть покрашен такой-то краской"</summary>
        PaintPrerequisite,
    }

    /// <summary>
    /// Результат применения рецепта. Содержит состояние, которое нужно записать в ящик
    /// (при отказе - исходное, без изменений), исход и причину отказа
    /// </summary>
    public readonly struct DisguiseResult
    {
        private DisguiseResult(
            DisguiseOutcome outcome,
            in CargoState state,
            bool divergesFromTruth,
            DisguiseRejection rejection)
        {
            Outcome = outcome;
            State = state;
            DivergesFromTruth = divergesFromTruth;
            Rejection = rejection;
        }

        /// <summary>Исход применения</summary>
        public DisguiseOutcome Outcome { get; }

        /// <summary>Состояние после применения (при отказе - прежнее)</summary>
        public CargoState State { get; }

        /// <summary>Причина отказа или <see cref="DisguiseRejection.None"/></summary>
        public DisguiseRejection Rejection { get; }

        /// <summary>
        /// Заявленное содержимое разошлось с истинным - единственное, для чего процессору
        /// нужна <see cref="CargoIdentity"/>: прочитать правду и сообщить, что появилась улика
        /// </summary>
        public bool DivergesFromTruth { get; }

        /// <summary>Состояние ящика поменялось и его нужно записать/отрисовать</summary>
        public bool Changed => Outcome == DisguiseOutcome.Applied;

        internal static DisguiseResult Applied(in CargoState state, bool diverges) =>
            new(DisguiseOutcome.Applied, state, diverges, DisguiseRejection.None);

        internal static DisguiseResult AlreadyApplied(in CargoState state, bool diverges) =>
            new(DisguiseOutcome.AlreadyApplied, state, diverges, DisguiseRejection.None);

        internal static DisguiseResult Rejected(in CargoState state, bool diverges, DisguiseRejection rejection) =>
            new(DisguiseOutcome.Rejected, state, diverges, rejection);
    }
}

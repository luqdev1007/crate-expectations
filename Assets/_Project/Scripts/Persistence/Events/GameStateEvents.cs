namespace CrateExpectations.Persistence.Events
{
    /// <summary>Прогресс записан</summary>
    public readonly struct GameSaved
    {
        public GameSaved(string slotName) => SlotName = slotName;

        /// <summary>Как слот называется для игрока</summary>
        public string SlotName { get; }
    }

    /// <summary>
    /// Прогресс восстановлен и уже применён к системам. Подписчикам - сигнал перечитать
    /// состояние: восстановленный баланс не публикует "изменение баланса", потому что
    /// у него нет причины, которую можно было бы показать игроку
    /// </summary>
    public readonly struct GameLoaded
    {
        public GameLoaded(string slotName) => SlotName = slotName;

        /// <summary>Как слот называется для игрока</summary>
        public string SlotName { get; }
    }

    /// <summary>
    /// Сохранить или загрузить не вышло. Игра при этом продолжается в том состоянии,
    /// в каком была: неудачная загрузка не оставляет мир наполовину восстановленным
    /// </summary>
    public readonly struct GameStateFailed
    {
        public GameStateFailed(bool wasSaving, string reason)
        {
            WasSaving = wasSaving;
            Reason = reason;
        }

        /// <summary>Сорвалось сохранение (иначе - загрузка)</summary>
        public bool WasSaving { get; }

        /// <summary>Что именно пошло не так - текст для игрока</summary>
        public string Reason { get; }
    }
}

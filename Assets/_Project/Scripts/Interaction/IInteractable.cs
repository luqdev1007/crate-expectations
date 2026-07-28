namespace CrateExpectations.Interaction
{
    /// <summary>Контракт объекта, с которым можно взаимодействовать по прицелу</summary>
    public interface IInteractable
    {
        /// <summary>
        /// Текст подсказки для игрока (например, "Осмотреть ящик"). Может быть контекстным.
        /// Опрашивается каждый кадр, пока объект под прицелом, - возвращать нужно готовую строку,
        /// а не собирать её на месте
        /// </summary>
        string Prompt { get; }

        /// <summary>Можно ли сейчас взаимодействовать</summary>
        bool CanInteract { get; }

        /// <summary>Прицел навёлся на объект - включить хайлайт/подсказку</summary>
        void OnFocused();

        /// <summary>Прицел ушёл с объекта - выключить хайлайт</summary>
        void OnUnfocused();

        /// <summary>Игрок нажал "взаимодействовать"</summary>
        void Interact(Interactor source);
    }
}

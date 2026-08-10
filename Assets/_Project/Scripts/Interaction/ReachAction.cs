namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Что сделает кнопка руки прямо сейчас. Значение ровно одно за раз - в этом весь смысл:
    /// клавиша одна, подсказка одна, и промахнуться между «покрасить» и «взять» игрок
    /// не должен
    /// </summary>
    public enum ReachAction
    {
        /// <summary>Дотянуться не до чего</summary>
        None,

        /// <summary>Нажать на то, что под прицелом</summary>
        Interact,

        /// <summary>Поднять груз, который под прицелом</summary>
        Grab,

        /// <summary>Положить груз, который уже в руках</summary>
        Drop,
    }
}

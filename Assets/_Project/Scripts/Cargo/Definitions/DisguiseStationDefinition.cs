using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Настройка станции маскировки: какой рецепт она применяет и какими словами о себе говорит
    /// (станции различаются данными, а не классами, - новый верстак заводится ассетом)
    /// </summary>
    [CreateAssetMenu(
        fileName = "DisguiseStationDefinition",
        menuName = "CrateExpectations/Cargo/Disguise Station")]
    public sealed class DisguiseStationDefinition : ScriptableObject
    {
        [Tooltip("Рецепт, который станция применяет к грузу в зоне")]
        [field: SerializeField] public DisguiseRecipe Recipe { get; private set; }

        [Header("Подсказки")]
        [Tooltip("Зона пуста")]
        [field: SerializeField] public string EmptyZonePrompt { get; private set; } = "Поставьте груз на стол";

        [Tooltip("Готово к применению. {0} - описание рецепта")]
        [field: SerializeField] public string ReadyPromptFormat { get; private set; } = "[E] {0}";

        [Tooltip("Уже применено. {0} - описание рецепта")]
        [field: SerializeField] public string AlreadyDonePromptFormat { get; private set; } = "{0} - уже сделано";

        [Tooltip("Не выполнено условие. {0} - описание рецепта, {1} - требуемая окраска")]
        [field: SerializeField] public string BlockedPromptFormat { get; private set; } = "{0} - сначала нужна окраска '{1}'";

        [Header("Отклик")]
        [Tooltip("Цвет вспышки зоны при срабатывании")]
        [field: SerializeField] public Color FeedbackColor { get; private set; } = new(1f, 1f, 1f, 1f);

        [Tooltip("Длительность вспышки, секунды")]
        [field: SerializeField] public float FeedbackSeconds { get; private set; } = 0.2f;
    }
}

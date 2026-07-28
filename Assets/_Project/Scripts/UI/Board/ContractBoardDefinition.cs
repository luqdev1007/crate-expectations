using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>Что доска пишет в шапке и что подсказывает при наведении на листок.</summary>
    [CreateAssetMenu(
        fileName = "ContractBoard",
        menuName = "CrateExpectations/Contracts/Contract Board")]
    public sealed class ContractBoardDefinition : ScriptableObject
    {
        [Header("Шапка доски")]
        [Tooltip("Надпись на доске. Статичная: доска - это вывеска, а не строка состояния, " +
                 "что происходит с заказом, игрок читает на самих листках и в HUD")]
        [field: SerializeField] public string Header { get; private set; } = "ЗАКАЗЫ";

        [Header("Подсказки")]
        [Tooltip("Листок можно снять. {0} - название заказа")]
        [field: SerializeField] public string TakePromptFormat { get; private set; } = "Взять заказ: {0}";

        [Tooltip("Уже есть незакрытый заказ")]
        [field: SerializeField] public string BlockedPrompt { get; private set; } = "Сначала закройте текущий заказ";
    }
}

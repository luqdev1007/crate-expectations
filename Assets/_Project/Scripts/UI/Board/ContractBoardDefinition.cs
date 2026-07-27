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
        [Tooltip("Заказы есть, игрок свободен")]
        [field: SerializeField] public string HeaderIdle { get; private set; } = "ЗАКАЗЫ ПОРТА - выберите заказ";

        [Tooltip("Заказ на руках. {0} - его название")]
        [field: SerializeField] public string HeaderActive { get; private set; } = "ЗАКАЗЫ ПОРТА - выполняется '{0}'";

        [Tooltip("Все листки разобраны, доска пуста")]
        [field: SerializeField] public string HeaderEmpty { get; private set; } = "ЗАКАЗЫ ПОРТА - листков больше нет";

        [Header("Подсказки")]
        [Tooltip("Листок можно снять. {0} - название заказа")]
        [field: SerializeField] public string TakePromptFormat { get; private set; } = "Взять заказ: {0}";

        [Tooltip("Уже есть незакрытый заказ")]
        [field: SerializeField] public string BlockedPrompt { get; private set; } = "Сначала закройте текущий заказ";
    }
}

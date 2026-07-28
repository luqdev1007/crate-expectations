using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Шаблоны строк листка заказа. Одна вёрстка и один набор формулировок на все листки -
    /// и на доске, и в руках
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContractPaper",
        menuName = "CrateExpectations/Contracts/Contract Paper")]
    public sealed class ContractPaperDefinition : ScriptableObject
    {
        [Tooltip("Сколько ящиков нужно сдать. {0} - количество")]
        [field: SerializeField] public string CratesFormat { get; private set; } = "Ящиков: {0}";

        [Tooltip("Награда. {0} - выплата за ящик")]
        [field: SerializeField] public string RewardFormat { get; private set; } = "{0} дукатов за ящик";

        [Tooltip("Под каким видом сдавать. {0} - имя типа груза")]
        [field: SerializeField] public string DeclaredAsFormat { get; private set; } = "Выдать за: {0}";

        [Tooltip("Чем заполняем строку 'выдать за', если заказчику всё равно")]
        [field: SerializeField] public string DeclaredAsNothing { get; private set; } = "как есть";
    }
}

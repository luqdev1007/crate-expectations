using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Ритм подачи на экране: сколько держатся плашки, которыми игра отвечает игроку
    /// (числа собраны в один ассет, потому что настраиваются вместе - «заказ выполнен»
    /// и «сохранено» не должны спорить за экран)
    /// </summary>
    [CreateAssetMenu(
        fileName = "HudTimings",
        menuName = "CrateExpectations/UI/HUD Timings")]
    public sealed class HudTimingsDefinition : ScriptableObject
    {
        [Tooltip("Сколько секунд держится итог заказа: момент, ради которого игрок всё и делал, " +
                 "он должен успеть прочитаться")]
        [field: SerializeField][Min(0.5f)] public float ContractOutcomeSeconds { get; private set; } = 4f;

        [Tooltip("Сколько секунд держится строка «сохранено» / «загружено»")]
        [field: SerializeField][Min(0.5f)] public float SaveStatusSeconds { get; private set; } = 3.5f;

        [Tooltip("Пауза между всплывшей суммой и началом прокрутки: сначала игрок читает «+N», " +
                 "и только потом за ней трогается счётчик")]
        [field: SerializeField][Min(0f)] public float BalanceLeadSeconds { get; private set; } = 0.2f;

        [Tooltip("Сколько секунд счётчик кошелька прокручивается к новому значению")]
        [field: SerializeField][Min(0.1f)] public float BalanceRollSeconds { get; private set; } = 0.8f;

        [Tooltip("Сколько секунд живёт всплывающая сумма «+N» над кошельком, считая с появления")]
        [field: SerializeField][Min(0.1f)] public float BalanceDeltaSeconds { get; private set; } = 1.6f;
    }
}

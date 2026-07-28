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

        [Tooltip("Сколько секунд держится вспышка изменения баланса вместе со строкой разбивки")]
        [field: SerializeField][Min(0.1f)] public float BalanceFlashSeconds { get; private set; } = 2.5f;
    }
}

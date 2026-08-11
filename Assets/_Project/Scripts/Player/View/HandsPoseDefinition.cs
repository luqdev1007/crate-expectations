using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Небоевые позы рук в кадре: что руки делают, когда в них не сабля.
    /// Собрана по образцу боевых наборов - клипы приходят из ассета, а не из путей
    /// в коде, потому что подбор позы это работа автора, а не программиста.
    /// <para>
    /// Отсюда их берёт генератор графа вьюмодели. Подменить позу - это заменить ссылку
    /// в этом ассете и пересобрать граф; ни одна строка кода при этом меняться не должна.
    /// Пустое поле - не «поза по умолчанию», а ошибка сборки: молча собранный граф
    /// с дырой на месте стейта ищется дольше, чем читается сообщение в консоли.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "HandsPoseDefinition",
        menuName = "CrateExpectations/Player/Hands Pose Definition")]
    public sealed class HandsPoseDefinition : ScriptableObject
    {
        [Header("Переноска")]
        [Tooltip("Проводка руками к грузу. Одноразовая, играется на входе в переноску " +
                 "и ужимается до CarryDefinition.GrabDuration множителем скорости")]
        [field: SerializeField] public AnimationClip CarryGrab { get; private set; }

        [Tooltip("Груз в руках, замаха нет. Нижний край блендтри переноски (ChargeT = 0)")]
        [field: SerializeField] public AnimationClip CarryIdle { get; private set; }

        [Tooltip("Конец замаха перед броском. Верхний край блендтри (ChargeT = 1). " +
                 "Между этими двумя позами игрок и видит набор заряда")]
        [field: SerializeField] public AnimationClip CarryWindup { get; private set; }

        [Tooltip("Выброс. Одноразовый: играется по событию броска, а не по занятости - " +
                 "занятость всё это время одна и та же")]
        [field: SerializeField] public AnimationClip CarryThrow { get; private set; }

        [Header("Листок заказа")]
        [Tooltip("Листок поднят и читается. Одна поза без blend tree: заряжать тут нечего")]
        [field: SerializeField] public AnimationClip ContractHold { get; private set; }

        /// <summary>
        /// Все клипы назначены. Проверка живёт здесь, а не в генераторе: это условие
        /// целостности САМИХ ДАННЫХ, и знать его должен тот, кто их держит
        /// </summary>
        public bool IsComplete =>
            CarryGrab != null &&
            CarryIdle != null &&
            CarryWindup != null &&
            CarryThrow != null &&
            ContractHold != null;
    }
}

using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Настройки листка заказа в руках. Позы здесь больше нет: листок висит на сокете под
    /// костью кисти, и куда именно он там встал - вопрос сцены, а не ассета. Осталось то,
    /// чего сцена знать не может: слой отрисовки и задержки показа
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContractView",
        menuName = "CrateExpectations/Contracts/Contract View")]
    public sealed class ContractViewDefinition : ScriptableObject
    {
        [Header("Слой")]
        [Tooltip("Слой ViewModel: тот же, что у рук, и рисует его та же камера. Листок должен " +
                 "жить в одной проекции с рукой, которая его держит, иначе он поедет " +
                 "относительно неё тем сильнее, чем дальше от центра экрана")]
        [field: SerializeField] public int ViewModelLayer { get; private set; } = 14;

        [Header("Задержки видимости")]
        [Tooltip("Через сколько листок появится после нажатия. Рука доезжает до позы не мгновенно, " +
                 "и без задержки листок повиснет в воздухе раньше, чем она за ним придёт")]
        [field: SerializeField][Min(0f)] public float ShowDelay { get; private set; } = 0.12f;

        [Tooltip("Через сколько листок исчезнет после нажатия. Рука сначала уводит его из кадра, " +
                 "и только потом он гаснет")]
        [field: SerializeField][Min(0f)] public float HideDelay { get; private set; } = 0.12f;
    }
}

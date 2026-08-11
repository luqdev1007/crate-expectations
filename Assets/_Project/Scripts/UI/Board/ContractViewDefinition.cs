using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Настройки листка заказа в руках. Позы здесь по-прежнему нет - листок висит на сокете
    /// под костью кисти и едет вместе с ней, - но САМ СОКЕТ настраивается отсюда.
    /// <para>
    /// Почему не трансформом в сцене, где он лежит: посадку листка в кисть подбирают на глаз
    /// и в play mode, а правка трансформа сцены выход из play mode не переживает. Ассет -
    /// переживает. Сокет в сцене остаётся (иерархия костей - это её дело), но числа его
    /// локального трансформа приезжают отсюда
    /// </para>
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

        [Header("Посадка в кисть")]
        [Tooltip("Локальная позиция сокета в кости кисти, м. Сокет стоит РОВНО в центре листка: " +
                 "собственного сдвига у листка нет, и поворот ниже крутит его вокруг себя, " +
                 "а не по дуге вокруг запястья")]
        [field: SerializeField]
        public Vector3 SocketPosition { get; private set; } = new Vector3(-0.05744f, 0.00884f, 0.00744f);

        [Tooltip("Локальный поворот сокета в кости кисти, углы Эйлера. Ноль - листок " +
                 "наследует ориентацию кисти как есть")]
        [field: SerializeField] public Vector3 SocketRotation { get; private set; }

        [Header("Задержки видимости")]
        [Tooltip("Через сколько листок появится после нажатия. Рука доезжает до позы не мгновенно, " +
                 "и без задержки листок повиснет в воздухе раньше, чем она за ним придёт")]
        [field: SerializeField][Min(0f)] public float ShowDelay { get; private set; } = 0.12f;

        [Tooltip("Через сколько листок исчезнет после нажатия. Рука сначала уводит его из кадра, " +
                 "и только потом он гаснет")]
        [field: SerializeField][Min(0f)] public float HideDelay { get; private set; } = 0.12f;
    }
}

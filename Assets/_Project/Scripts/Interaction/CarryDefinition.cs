using UnityEngine;

namespace CrateExpectations.Interaction
{
    /// <summary>Режим удержания переносимого объекта. Меняется в ассете без правок кода</summary>
    public enum HoldMode
    {
        /// <summary>Ведём rigidbody к точке удержания через скорость (стабильно)</summary>
        VelocityHold,

        /// <summary>Присоединяем объект ConfigurableJoint'ом к кинематическому якорю</summary>
        ConfigurableJoint,
    }

    /// <summary>Параметры захвата/переноса/броска. Все числа - здесь</summary>
    [CreateAssetMenu(
        fileName = "CarryDefinition",
        menuName = "CrateExpectations/Interaction/Carry Definition")]
    public sealed class CarryDefinition : ScriptableObject
    {
        [Tooltip("Дистанция точки удержания перед камерой, м")]
        [field: SerializeField] public float HoldDistance { get; private set; } = 2f;

        [Tooltip("Ближе этого к камере точку удержания не подтягиваем, м")]
        [field: SerializeField] public float MinHoldDistance { get; private set; } = 0.6f;

        [Tooltip("Что перекрывает точку удержания: всё твёрдое, кроме игрока и слоя переноса. " +
                 "Без этого точка уезжает в пол при взгляде вниз, и груз дребезжит о землю.")]
        [field: SerializeField] public LayerMask HoldBlockingMask { get; private set; } = ~0;

        [Tooltip("Дальность луча захвата, м")]
        [field: SerializeField] public float GrabDistance { get; private set; } = 3f;

        [Tooltip("Насколько объект может отстать от точки удержания, прежде чем выпасть из рук, м")]
        [field: SerializeField] public float BreakDistance { get; private set; } = 3f;

        [Tooltip("Скорость следования объекта за точкой удержания (VelocityHold)")]
        [field: SerializeField] public float FollowSpeed { get; private set; } = 12f;

        [Tooltip("Ограничение линейной скорости удерживаемого объекта, м/с")]
        [field: SerializeField] public float MaxVelocity { get; private set; } = 15f;

        [Tooltip("Сила броска вдоль forward камеры")]
        [field: SerializeField] public float ThrowForce { get; private set; } = 8f;

        [Tooltip("Угловое демпфирование удерживаемого объекта - гасит вращение при переносе")]
        [field: SerializeField] public float CarriedAngularDamping { get; private set; } = 8f;

        [Tooltip("Механика удержания. Переключается без правок кода")]
        [field: SerializeField] public HoldMode HoldMode { get; private set; } = HoldMode.VelocityHold;

        [Header("ConfigurableJoint (если выбран режим сустава)")]
        [Tooltip("Жёсткость пружины сустава, тянущей объект к точке удержания")]
        [field: SerializeField] public float JointSpring { get; private set; } = 1000f;

        [Tooltip("Демпфирование пружины сустава")]
        [field: SerializeField] public float JointDamper { get; private set; } = 50f;

        [Tooltip("Слои объектов, которые можно поднимать (грубый фильтр луча; " +
                 "решает всё равно наличие компонента Carriable).")]
        [field: SerializeField] public LayerMask CarriableMask { get; private set; } = ~0;

        [Header("Слой переноса")]
        [Tooltip("Индекс слоя (Layer), на который переводится объект в руках. " +
                 "Этот слой должен быть исключён из столкновений со слоем игрока " +
                 "в матрице коллизий (Project Settings → Physics), иначе груз будет толкать игрока.")]
        [field: SerializeField] public int CarriedLayer { get; private set; }
    }
}

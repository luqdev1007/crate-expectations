using UnityEngine;

namespace CrateExpectations.Player
{
    /// <summary>Числа геймплея FPS-контроллера. Ни одно из них не хардкодится в коде</summary>
    [CreateAssetMenu(
        fileName = "PlayerMovementDefinition",
        menuName = "CrateExpectations/Player/Movement Definition")]
    public sealed class PlayerMovementDefinition : ScriptableObject
    {
        [Header("Движение")]
        [Tooltip("Целевая горизонтальная скорость, м/с")]
        [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;

        [Tooltip("Как быстро скорость приближается к целевой, м/с²")]
        [field: SerializeField] public float Acceleration { get; private set; } = 40f;

        [Header("Локомоция в анимации")]
        [Tooltip("За сколько примерно секунд ноги догоняют скорость, с. Разгон физики " +
                 "и разгон походки - разные вещи: скорость набирается за доли кадра " +
                 "по Acceleration, и без этого сглаживания ноги переключались бы " +
                 "с покоя на полный бег одним кадром")]
        [field: SerializeField][Min(0f)] public float LocomotionSmoothTime { get; private set; } = 0.12f;

        [Tooltip("Ниже какой доли от MoveSpeed скорость считается нулевой. Стоящего " +
                 "игрока толкают ящики и сползание с уклона; без мёртвой зоны ноги " +
                 "перебирали бы на месте всё время, пока он просто стоит. " +
                 "Живёт рядом с MoveSpeed намеренно: это доля именно от него")]
        [field: SerializeField][Range(0f, 0.5f)] public float LocomotionDeadband { get; private set; } = 0.05f;

        [Header("Прыжок")]
        [Tooltip("Высота прыжка в метрах (импульс считается из неё и гравитации)")]
        [field: SerializeField] public float JumpHeight { get; private set; } = 1.2f;

        [Tooltip("Сколько после прыжка считать игрока в воздухе, даже если сфера проверки " +
                 "земли ещё цепляет пол, с. Сфера отлипает не сразу - игрок должен успеть " +
                 "подняться на её радиус, - и всё это время бой видел бы удар наземным. " +
                 "Держать чуть больше этого подъёма и заметно меньше длины прыжка")]
        [field: SerializeField][Range(0f, 0.5f)] public float JumpAirborneGrace { get; private set; } = 0.2f;

        [Header("Обзор")]
        [Tooltip("Чувствительность мыши, градусов на единицу дельты")]
        [field: SerializeField] public float LookSensitivity { get; private set; } = 0.1f;

        [Tooltip("Минимальный угол наклона камеры (вниз)")]
        [field: SerializeField] public float PitchMin { get; private set; } = -85f;

        [Tooltip("Максимальный угол наклона камеры (вниз). Не 85, как было: тело теперь видно " +
                 "из собственных глаз, и ниже этого угла игрок начинает заглядывать " +
                 "в собственную горловину - голова скрыта, отверстие под неё открыто, " +
                 "и сквозь него виден торс изнутри. Замер на худшей фазе цикла при высоте " +
                 "глаза 1.70: 58° и 64° чисто, на 70° горловина просматривается. " +
                 "Число связано с CameraHeight у тела: опустите глаз к анатомическим 1.55 - " +
                 "и никакой предел уже не спасёт, там камера внутри самой ткани")]
        [field: SerializeField] public float PitchMax { get; private set; } = 65f;

        [Header("Скольжение вдоль стен")]
        [Tooltip("Максимальный |y| нормали контакта, при котором поверхность считается стеной, " +
                 "а не полом или потолком. 0 - только строго вертикальные стены, " +
                 "0.4 ≈ всё круче ~66°.")]
        [field: SerializeField] public float WallNormalMaxY { get; private set; } = 0.4f;

        [Header("Проверка земли")]
        [Tooltip("Радиус сферы проверки земли")]
        [field: SerializeField] public float GroundCheckRadius { get; private set; } = 0.3f;

        [Tooltip("Смещение центра сферы вниз от точки проверки")]
        [field: SerializeField] public float GroundCheckDistance { get; private set; } = 0.25f;

        [Tooltip("Слои, считающиеся землёй")]
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = ~0;
    }
}

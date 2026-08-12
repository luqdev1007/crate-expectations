using System;
using CrateExpectations.Core.Hands;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Все числа кадрирования вьюмодели в одном ассете: где под камерой стоит модель, каким
    /// объективом её снимают и как довёрнуты кости руки.
    /// <para>
    /// Почему ассет, а не поля на объектах сцены: кадрирование подбирается живьём, в play mode,
    /// а изменения компонентов сцены выход из play mode стирает. Правки ScriptableObject
    /// переживают его - подобранное число остаётся подобранным.
    /// </para>
    /// <para>
    /// Слои независимы и правятся по отдельности:
    /// <list type="number">
    /// <item>доворот костей выводит кисть в кадр, оставляя плечо за ближней плоскостью;</item>
    /// <item>посадка оружия в ладони (<see cref="Combat.WeaponDefinition"/>) поворачивает
    /// клинок вокруг собственной оси и кисть не двигает;</item>
    /// <item>корневой офсет - только мелкая доводка. Если ему снова нужны десятки градусов
    /// разворота, значит первый слой недоработан: разворот всей модели уводит дугу замаха
    /// туда, где её никто не считал.</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "ViewModelFramingDefinition",
        menuName = "CrateExpectations/Player/View Model Framing Definition")]
    public sealed class ViewModelFramingDefinition : ScriptableObject
    {
        /// <summary>
        /// Доворот одной кости поверх того, что записал в неё клип. Углы отдельными полями,
        /// а не <see cref="Vector3"/>, ровно ради слайдеров: <c>Range</c> на векторе не работает,
        /// а числа здесь подбираются мышью, а не вводятся с клавиатуры
        /// </summary>
        [Serializable]
        public struct BoneOverride
        {
            [Tooltip("Кость гуманоидного рига. Берётся через Animator, а не по имени в иерархии: " +
                     "имена костей зависят от того, кто экспортировал модель")]
            public HumanBodyBones Bone;

            [Range(-180f, 180f)] public float Pitch;
            [Range(-180f, 180f)] public float Yaw;
            [Range(-180f, 180f)] public float Roll;

            /// <summary>Доворот как углы Эйлера в локальном пространстве кости</summary>
            public Vector3 Euler => new(Pitch, Yaw, Roll);
        }

        /// <summary>
        /// Смещение корня для одной занятости рук. Оси отдельными полями по той же причине,
        /// что и углы в <see cref="BoneOverride"/>: <c>Range</c> на <see cref="Vector3"/>
        /// не работает, а эти числа подбирают мышью в play mode
        /// </summary>
        [Serializable]
        public struct StanceOffset
        {
            [Range(-1f, 1f)] public float X;
            [Range(-1f, 1f)] public float Y;
            [Range(-1f, 1f)] public float Z;

            /// <summary>Смещение в пространстве камеры: X вправо, Y вверх, Z вперёд от глаза</summary>
            public Vector3 Value => new(X, Y, Z);
        }

        [Header("Корень вьюмодели (мелкая доводка)")]
        [Tooltip("Смещение корня модели относительно камеры, м. " +
                 "X вправо, Y вверх, Z вперёд от глаза")]
        [SerializeField][Range(-1f, 1f)] private float _rootX;

        [SerializeField][Range(-2f, 0f)] private float _rootY = -1f;

        [SerializeField][Range(-1f, 1f)] private float _rootZ;

        [Tooltip("Доворот корня, градусы. Держим близким к нулю - композицию задаёт " +
                 "доворот костей, а не разворот всей модели")]
        [SerializeField][Range(-45f, 45f)] private float _rootPitch;

        [SerializeField][Range(-45f, 45f)] private float _rootYaw;

        [SerializeField][Range(-45f, 45f)] private float _rootRoll;

        [Header("Объектив вьюмодели")]
        [Tooltip("Угол обзора overlay-камеры, градусы. Свой, отдельный от мирового: " +
                 "им регулируется, насколько крупно руки лежат в кадре, " +
                 "без влияния на то, как широко видно порт")]
        [SerializeField][Range(20f, 90f)] private float _fieldOfView = 50f;

        [Header("Стойка по занятости рук")]
        [Tooltip("Добавка к корню, когда руки свободны. Ноль - базовая компоновка")]
        [SerializeField] private StanceOffset _freeStance;

        [Tooltip("Добавка к корню, когда в руках груз. Клипы переноски авторены для вида " +
                 "от третьего лица и держат кисти ниже нижней кромки кадра - поднимаем " +
                 "всю стойку. Ориентир 0.30 по Y: при нём кисть левой руки в кадре, " +
                 "а пальцы обеих - уверенно в кадре")]
        [SerializeField] private StanceOffset _carryingStance = new() { Y = 0.30f };

        [Tooltip("Добавка к корню, когда в руках листок заказа. Ноль - компоновка чтения " +
                 "подобрана без подъёма")]
        [SerializeField] private StanceOffset _readingStance;

        [Tooltip("Добавка к корню, когда в руке оружие. Ноль, и менять это без нужды нельзя: " +
                 "вся боевая композиция - дуги ударов, посадка клинка, блок - подобрана " +
                 "относительно базового корня")]
        [SerializeField] private StanceOffset _combatStance;

        [Tooltip("За сколько секунд стойка переезжает на новое место при смене занятости. " +
                 "Ноль дал бы скачок кадра ровно в тот момент, когда груз прилипает к рукам")]
        [SerializeField][Range(0f, 1f)] private float _stanceBlend = 0.15f;

        [Header("Доворот костей (основной инструмент)")]
        [Tooltip("Применяется поверх любого клипа, поэтому замах остаётся замахом - " +
                 "просто из смещённой стартовой позы")]
        [SerializeField] private BoneOverride[] _bones = Array.Empty<BoneOverride>();

        /// <summary>Смещение корня модели относительно камеры</summary>
        public Vector3 RootPosition => new(_rootX, _rootY, _rootZ);

        /// <summary>Доворот корня модели относительно камеры</summary>
        public Vector3 RootRotation => new(_rootPitch, _rootYaw, _rootRoll);

        /// <summary>Угол обзора overlay-камеры вьюмодели</summary>
        public float FieldOfView => _fieldOfView;

        /// <summary>Довороты костей в порядке применения</summary>
        public BoneOverride[] Bones => _bones;

        /// <summary>За сколько секунд стойка переезжает при смене занятости рук</summary>
        public float StanceBlend => _stanceBlend;

        /// <summary>
        /// Добавка к <see cref="RootPosition"/> для текущей занятости рук. Ассет только
        /// отвечает числом на вопрос «какая занятость» - решает, какая она, по-прежнему
        /// <see cref="HandsState"/>, а накладывает добавку слой вьюмодели.
        /// <para>
        /// Зачем это вообще нужно: <see cref="RootPosition"/> и <see cref="Bones"/> одни на
        /// всю вьюмодель, то есть поднять руки «только на время переноски» ими нельзя -
        /// тот же подъём уехал бы в бой и сломал бы подобранные там дуги. Занятость - это
        /// первое, что у кадрирования вообще есть для различения состояний, и различать
        /// им надо ровно композицию, а не поведение.
        /// </para>
        /// <para>
        /// Неизвестная занятость - это ноль, а не исключение: новое значение перечисления
        /// не должно ронять кадр, оно должно означать «стойка не подобрана»
        /// </para>
        /// </summary>
        public Vector3 StanceOffsetFor(HandsOccupancy occupancy) => occupancy switch
        {
            HandsOccupancy.Carrying => _carryingStance.Value,
            HandsOccupancy.Reading => _readingStance.Value,
            HandsOccupancy.Combat => _combatStance.Value,
            HandsOccupancy.Free => _freeStance.Value,
            _ => Vector3.zero,
        };
    }
}

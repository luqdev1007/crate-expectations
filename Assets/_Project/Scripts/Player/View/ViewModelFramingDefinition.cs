using System;
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
    }
}

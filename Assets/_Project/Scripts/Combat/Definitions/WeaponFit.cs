using System;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Посадка оружия в одном сокете: где оно лежит относительно ладони, как довёрнуто и
    /// какого размера. Блок целиком, а не три поля россыпью, потому что таких блоков у оружия
    /// несколько - по одному на каждый вид сокета (<see cref="WeaponSocketSlot"/>).
    /// <para>
    /// Оси - отдельными float-полями, а не <see cref="Vector3"/>, ровно ради слайдеров:
    /// <c>Range</c> на векторе Unity не рисует, а числа здесь подбираются мышью в play mode.
    /// </para>
    /// </summary>
    [Serializable]
    public struct WeaponFit
    {
        [Tooltip("Смещение относительно сокета в ладони, м")]
        [Range(-0.5f, 0.5f)] public float PositionX;

        [Range(-0.5f, 0.5f)] public float PositionY;

        [Range(-0.5f, 0.5f)] public float PositionZ;

        [Tooltip("Доворот относительно сокета, градусы. Клинок модели смотрит вдоль +Y")]
        [Range(-180f, 180f)] public float RotationX;

        [Range(-180f, 180f)] public float RotationY;

        [Range(-180f, 180f)] public float RotationZ;

        [Tooltip("Равномерный масштаб в сокете. Модели Seven Swords авторены крупнее " +
                 "человеческого роста, для скелета 1.64 м нужно около 0.55")]
        [Range(0.05f, 2f)] public float Scale;

        /// <summary>Смещение относительно сокета</summary>
        public readonly Vector3 Position => new(PositionX, PositionY, PositionZ);

        /// <summary>Доворот относительно сокета как углы Эйлера</summary>
        public readonly Vector3 Rotation => new(RotationX, RotationY, RotationZ);
    }
}

using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Положение корня вьюмодели в пространстве камеры. Значение, а не <see cref="Transform"/>:
    /// слои кадрирования складываются между собой в чистой арифметике и трогают сцену
    /// один раз, готовым результатом
    /// </summary>
    public readonly struct ViewModelPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public ViewModelPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}

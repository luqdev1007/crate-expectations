using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Всё, что удар сообщает о себе цели. Значение, а не пять аргументов подряд:
    /// список того, что нужно знать о попадании, будет расти (тип поверхности, критический
    /// урон, чем ударили), и каждое добавление иначе переписывало бы сигнатуру всем
    /// реализациям <see cref="IDamageable"/> сразу.
    /// <para>
    /// Точка и нормаль здесь не ради урона, а ради картинки: искры ставятся в точку и
    /// разворачиваются по нормали, и считать их заново по коллайдеру - значит считать
    /// то, что уже посчитала физика.
    /// </para>
    /// </summary>
    public readonly struct HitInfo
    {
        /// <summary>Точка касания в мире</summary>
        public readonly Vector3 Point;

        /// <summary>Нормаль поверхности в точке касания</summary>
        public readonly Vector3 Normal;

        /// <summary>Куда шёл клинок, единичный вектор. Вдоль него же летит цель</summary>
        public readonly Vector3 Direction;

        /// <summary>Импульс отбрасывания, Н·с</summary>
        public readonly float Impulse;

        /// <summary>Урон</summary>
        public readonly int Damage;

        public HitInfo(Vector3 point, Vector3 normal, Vector3 direction, float impulse, int damage)
        {
            Point = point;
            Normal = normal;
            Direction = direction;
            Impulse = impulse;
            Damage = damage;
        }
    }
}

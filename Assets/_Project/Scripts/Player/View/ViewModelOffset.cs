using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Добавка одного слоя к позе вьюмодели: поворот вокруг точки плюс смещение.
    /// <para>
    /// Точка вращения здесь не для красоты. Слой, который умеет только домножать поворот,
    /// крутит модель вокруг её собственного начала - это выглядит как проворот предмета
    /// в пустоте. Живое движение руки - это поворот вокруг сустава, лежащего вне модели
    /// (плечо для замаха, шея для покачивания), и точку этого сустава слой обязан
    /// сообщать вместе с углом.
    /// </para>
    /// </summary>
    public readonly struct ViewModelOffset
    {
        /// <summary>Точка вращения в пространстве камеры</summary>
        public readonly Vector3 Pivot;

        /// <summary>Поворот вокруг <see cref="Pivot"/></summary>
        public readonly Quaternion Rotation;

        /// <summary>Смещение после поворота, в пространстве камеры</summary>
        public readonly Vector3 Translation;

        public ViewModelOffset(Vector3 pivot, Quaternion rotation, Vector3 translation)
        {
            Pivot = pivot;
            Rotation = rotation;
            Translation = translation;
        }

        /// <summary>Слой промолчал - поза проходит через него неизменной</summary>
        public static ViewModelOffset None => new(Vector3.zero, Quaternion.identity, Vector3.zero);

        /// <summary>
        /// Накладывает добавку на позу. Порядок наложения слоёв поэтому значим:
        /// поворот последующего слоя крутит уже смещённую позу, а не исходную
        /// </summary>
        public ViewModelPose ApplyTo(ViewModelPose pose) => new(
            Pivot + Rotation * (pose.Position - Pivot) + Translation,
            Rotation * pose.Rotation);
    }
}

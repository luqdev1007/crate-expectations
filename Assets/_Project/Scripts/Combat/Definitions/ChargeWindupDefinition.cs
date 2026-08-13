using System;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Подготовка заряженного удара: куда уводится вьюмодель, пока игрок копит удержание.
    /// Одна запись на направление - отвод у укола вперёд и у рубящего вбок разный,
    /// и общий набор чисел заставлял бы их быть одинаковыми.
    /// <para>
    /// Ассет отдельный от <see cref="SwingDefinition"/>, и переиспользовать его структуру
    /// нельзя: там пять фаз удара, разложенных по кривым, здесь одна фаза нарастания.
    /// Значения нарастают от нуля к записанным здесь линейно по заряду, поэтому кривая
    /// не нужна вовсе - её единственной ролью было бы описывать форму, которой тут нет.
    /// </para>
    /// <para>
    /// Числа - это ДАННЫЕ, а не движение: интерполяцию считает слой
    /// (<c>ViewModelChargeWindup</c>), и не от лени, а потому что стартовая точка у него
    /// не всегда нулевая - заряд, начатый посреди возврата, подхватывает позу с того места,
    /// где её застал. Ассет про это знать не должен.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "ChargeWindupDefinition",
        menuName = "CrateExpectations/Combat/Charge Windup Definition")]
    public sealed class ChargeWindupDefinition : ScriptableObject
    {
        /// <summary>
        /// Отвод для одного направления. Оси и углы отдельными полями, а не
        /// <see cref="Vector3"/>, ровно ради слайдеров: <c>Range</c> на векторе не работает,
        /// а эти числа подбирают мышью в play mode - тем же приёмом, что и стойку
        /// в <c>ViewModelFramingDefinition</c>
        /// </summary>
        [Serializable]
        public struct DirectionWindup
        {
            [Tooltip("Куда уводится вьюмодель на полном заряде, м. Пространство камеры: " +
                     "X вправо, Y вверх, Z вперёд от глаза")]
            [Range(-0.5f, 0.5f)] public float OffsetX;

            [Range(-0.5f, 0.5f)] public float OffsetY;

            [Range(-0.5f, 0.5f)] public float OffsetZ;

            [Tooltip("Доворот на полном заряде, градусы. Крутится вокруг локального начала " +
                     "вьюмодели, а не вокруг точки хвата: это изготовка, а не дуга удара")]
            [Range(-45f, 45f)] public float Pitch;

            [Range(-45f, 45f)] public float Yaw;

            [Range(-45f, 45f)] public float Roll;

            [Tooltip("За сколько секунд поза возвращается в ноль, когда заряд кончился, с. " +
                     "Ноль - мгновенно. Возврат идёт одинаково и на выстреле, и на отмене: " +
                     "клип удара стартует со своей позы и подхватывать отвод не обязан")]
            [Range(0f, 0.5f)] public float SnapBackTime;

            /// <summary>Смещение на полном заряде, в пространстве камеры</summary>
            public Vector3 Offset => new(OffsetX, OffsetY, OffsetZ);

            /// <summary>Доворот на полном заряде как углы Эйлера</summary>
            public Vector3 EulerAngles => new(Pitch, Yaw, Roll);
        }

        // Направления перечислены полями, а не массивом с полем Direction: заряженных
        // направлений ровно столько, сколько ступеней tier1 в раскладке, и массив здесь
        // добавил бы возможность записать одно направление дважды - без единого выигрыша

        [Header("Наступление")]
        [Tooltip("Укол вперёд. Отвод чистый назад: изготовка к выпаду - это замах локтем, " +
                 "а не увод клинка в сторону")]
        [SerializeField] private DirectionWindup _forward = new() { OffsetZ = -0.08f, SnapBackTime = 0.05f };

        [Header("Шаг вправо")]
        [Tooltip("Рубящий вправо. Отвод в противоход удару: клинок уходит влево-вверх, " +
                 "чтобы удар пришёл справа")]
        [SerializeField] private DirectionWindup _right = new()
        {
            OffsetX = -0.10f, OffsetY = 0.03f, OffsetZ = -0.03f, Roll = -8f, SnapBackTime = 0.05f,
        };

        [Header("Шаг влево")]
        [Tooltip("Рубящий влево. Зеркало правого")]
        [SerializeField] private DirectionWindup _left = new()
        {
            OffsetX = 0.10f, OffsetY = 0.03f, OffsetZ = -0.03f, Roll = 8f, SnapBackTime = 0.05f,
        };

        [Header("Удар с места")]
        [Tooltip("Рубящий сверху. Отвод вверх и назад - занос над головой. Ось Y здесь " +
                 "самая рискованная: подъём выносит кисть к верхней кромке кадра")]
        [SerializeField] private DirectionWindup _neutral = new()
        {
            OffsetY = 0.08f, OffsetZ = -0.05f, Pitch = -10f, SnapBackTime = 0.05f,
        };

        /// <summary>
        /// Отвод для направления. Незаряжаемые направления (отступление, воздух) сюда
        /// не записаны и отдают нули - это не ошибка, а «отводить нечего»: у них нет
        /// ступени заряда, и слой до этого вызова всё равно не доходит.
        /// <para>
        /// Новое значение перечисления тоже отдаёт нули, а не исключение: незаполненная
        /// раскладка не должна ронять кадр
        /// </para>
        /// </summary>
        public DirectionWindup For(AttackDirection direction) => direction switch
        {
            AttackDirection.Forward => _forward,
            AttackDirection.Right => _right,
            AttackDirection.Left => _left,
            AttackDirection.Neutral => _neutral,
            _ => default,
        };
    }
}

using UnityEngine;

namespace CrateExpectations.Trailer
{
    /// <summary>
    /// Качка корпуса для промо-сцены.
    /// <para>
    /// Высоту корабль берёт с настоящей волны (<c>StylizedWater2.FloatingTransform</c> на
    /// родителе), а вот наклон оттуда получается почти нулевой: корпус длиной полсотни метров
    /// на волне в полметра físически и не должен качаться заметно. Для кадра этого мало,
    /// поэтому крен и тангаж досыпаются здесь.
    /// </para>
    /// <para>
    /// Шум, а не синусы: у синуса период один и тот же, и на длинном плане корабль начинает
    /// читаться метрономом. Частоты по осям разные и намеренно не кратные - иначе крен
    /// и тангаж сходятся в одну диагональ и качка выглядит как один наклон туда-сюда.
    /// </para>
    /// </summary>
    public sealed class TrailerShipRock : MonoBehaviour
    {
        [Header("Крен (вокруг продольной оси)")]
        [SerializeField][Range(0f, 15f)] private float _rollAngle = 3.2f;
        [SerializeField][Range(0.01f, 2f)] private float _rollSpeed = 0.16f;

        [Header("Тангаж (нос-корма)")]
        [SerializeField][Range(0f, 15f)] private float _pitchAngle = 1.8f;
        [SerializeField][Range(0.01f, 2f)] private float _pitchSpeed = 0.23f;

        [Header("Вертикаль")]
        [Tooltip("Добавка к подъёму на волне, м. Волну корпус уже отрабатывает сам - " +
                 "это только чтобы движение читалось на длинном корабле")]
        [SerializeField][Range(0f, 2f)] private float _bob = 0.35f;
        [SerializeField][Range(0.01f, 2f)] private float _bobSpeed = 0.31f;

        private Vector3 _startPosition;

        private void Awake() => _startPosition = transform.localPosition;

        private void LateUpdate()
        {
            // Разные точки на одном поле шума, а не три поля: одна текстура шума,
            // три независимых обхода по ней
            float roll = Noise(0f, _rollSpeed) * _rollAngle;
            float pitch = Noise(37f, _pitchSpeed) * _pitchAngle;
            float bob = Noise(71f, _bobSpeed) * _bob;

            transform.localRotation = Quaternion.Euler(pitch, 0f, roll);
            transform.localPosition = _startPosition + new Vector3(0f, bob, 0f);
        }

        /// <summary>Шум в диапазоне -1..1: PerlinNoise даёт 0..1, а качка симметрична</summary>
        private static float Noise(float seed, float speed) =>
            (Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f) * 2f;
    }
}

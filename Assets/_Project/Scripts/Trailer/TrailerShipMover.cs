using UnityEngine;

namespace CrateExpectations.Trailer
{
    /// <summary>
    /// Крейсерский ход корабля для промо-сцены: ровное движение вперёд плюс еле заметный
    /// увод курса, чтобы прямая не читалась рельсой.
    /// <para>
    /// Только для трейлера, геймплея здесь нет. Качку этот компонент НЕ делает - её
    /// снимает <c>StylizedWater2.FloatingTransform</c> с самой волны, и вешать её сюда
    /// значило бы качать корабль отдельно от воды, по которой он идёт.
    /// </para>
    /// </summary>
    public sealed class TrailerShipMover : MonoBehaviour
    {
        [Tooltip("Скорость хода, м/с. Спокойный крейсерский - 2..4")]
        [SerializeField][Range(0f, 12f)] private float _speed = 3f;

        [Tooltip("Насколько градусов курс гуляет в стороны. Ноль - строго по прямой")]
        [SerializeField][Range(0f, 20f)] private float _driftAngle = 4f;

        [Tooltip("Как быстро гуляет курс. Медленнее, чем качка, иначе читается вилянием")]
        [SerializeField][Range(0.01f, 1f)] private float _driftSpeed = 0.05f;

        private float _startYaw;
        private float _seed;

        private void Awake()
        {
            _startYaw = transform.eulerAngles.y;

            // Своя точка на шуме у каждого экземпляра: два корабля в кадре не должны
            // вилять синхронно
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            // Шум, а не синус: синус даёт метроном, и на длинном плане это видно
            float drift = (Mathf.PerlinNoise(_seed, Time.time * _driftSpeed) - 0.5f) * 2f * _driftAngle;

            transform.rotation = Quaternion.Euler(0f, _startYaw + drift, 0f);
            transform.position += transform.forward * (_speed * Time.deltaTime);
        }
    }
}

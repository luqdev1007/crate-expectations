using UnityEngine;

namespace CrateExpectations.Trailer
{
    /// <summary>
    /// Везёт игрока вместе с кораблём.
    /// <para>
    /// Родителем корабль сделать не получилось: игрок - динамический <see cref="Rigidbody"/>,
    /// и когда родительский трансформ уезжает вперёд, физика на своём шаге тянет тело
    /// обратно в прежнюю мировую точку. Получается проскальзывание - за десять секунд
    /// игрок съезжал с юта за корму, хотя формально был ребёнком корабля.
    /// </para>
    /// <para>
    /// Поэтому горизонтальный перенос делается явно: сколько за шаг проехал корабль,
    /// на столько же двигается тело игрока. Вертикаль и наклон при этом не трогаем -
    /// их игрок получает обычной опорой на палубу, и подмешивать их сюда значило бы
    /// драться с той же физикой второй раз.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrailerRideAlong : MonoBehaviour
    {
        [Tooltip("За чем ехать. Обычно корень корабля, который двигает TrailerShipMover")]
        [SerializeField] private Transform _platform;

        private Rigidbody _body;
        private Vector3 _previousPlatformPosition;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();

            if (_platform == null)
            {
                Debug.LogError($"'{name}' не сказали, за какой платформой ехать.", this);
                enabled = false;
                return;
            }

            _previousPlatformPosition = _platform.position;
        }

        private void FixedUpdate()
        {
            Vector3 delta = _platform.position - _previousPlatformPosition;
            _previousPlatformPosition = _platform.position;

            // Только горизонталь: вертикаль - это качка, и её игрок отрабатывает опорой
            delta.y = 0f;

            if (delta.sqrMagnitude > 0f)
                _body.position += delta;
        }
    }
}

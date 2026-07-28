using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    /// <summary>
    /// Доводка груза до места: поставленный на платформу ящик встаёт в центр зоны, ровно,
    /// и замирает. Без этого каждый ящик стоит как попало - криво, на краю, а то и сползает
    /// со стола от толчка, и станция с инспектором работают с тем, что игрок не так и ставил.
    ///
    /// <para>Отдельный компонент рядом с <see cref="CargoPlacementZone"/>, а не её метод:
    /// зона отвечает за "кто здесь стоит", а доводка - за "как он стоит". Снимите этот
    /// компонент - зона продолжит работать, просто ящики останутся там, где их бросили.</para>
    /// </summary>
    [RequireComponent(typeof(CargoPlacementZone))]
    public sealed class CargoSnap : MonoBehaviour
    {
        [Tooltip("Ниже этой скорости груз считается улёгшимся и его можно ставить на место, м/с. " +
                 "Пока ящик летит или катится, доводка ждёт: иначе он замер бы в воздухе")]
        [SerializeField][Min(0.01f)] private float _restSpeed = 0.1f;

        [Tooltip("Сколько секунд ящик должен простоять смирно, прежде чем его зафиксируют. " +
                 "Нужны, чтобы поставленный на весу груз успел упасть на платформу, " +
                 "а не застыл в паре сантиметров над ней")]
        [SerializeField][Min(0f)] private float _settleSeconds = 0.15f;

        private CargoPlacementZone _zone;
        private Collider _bounds;

        // Ящик, который ждёт своей очереди или уже стоит - всегда один и тот же
        private CargoBox _cargo;
        private Rigidbody _body;

        // Тело ведём мы, а не физика: снято на время стоянки, возвращается при отпускании
        private bool _holdsBody;
        private bool _wasKinematic;

        private float _atRest;

        private Phase _phase;

        private enum Phase
        {
            Empty,
            WaitingToSettle,
            Docked,
        }

        private void Awake()
        {
            _zone = GetComponent<CargoPlacementZone>();
            _bounds = GetComponent<Collider>();
        }

        private void OnEnable() => _zone.OccupantChanged += OnOccupantChanged;

        private void OnDisable()
        {
            _zone.OccupantChanged -= OnOccupantChanged;

            Release();
        }

        private void FixedUpdate()
        {
            if (_phase == Phase.Empty)
                return;

            // Груз забрали (или он не пережил доставку) - отпускаем его физике,
            // пока он ещё существует
            if (_cargo == null || _cargo.IsCarried || !ReferenceEquals(_zone.Occupant, _cargo))
            {
                Release();
                return;
            }

            if (_phase != Phase.WaitingToSettle)
                return;

            _atRest = _body.linearVelocity.sqrMagnitude <= _restSpeed * _restSpeed
                ? _atRest + Time.fixedDeltaTime
                : 0f;

            if (_atRest >= _settleSeconds)
                Dock();
        }

        private void OnOccupantChanged(CargoBox occupant)
        {
            Release();

            if (occupant == null || !occupant.TryGetComponent(out Rigidbody body))
                return;

            _cargo = occupant;
            _body = body;
            _atRest = 0f;
            _phase = Phase.WaitingToSettle;
        }

        /// <summary>
        /// Поставить и держать. Ящик переносится в центр зоны одним кадром: доводка - это
        /// не отдельное событие на экране, а то, чего игрок не должен замечать вовсе.
        /// Высоту ящик оставляет свою - он уже лежит на платформе, и поднимать его над ней
        /// (или топить в ней) незачем
        /// </summary>
        private void Dock()
        {
            Vector3 center = _bounds.bounds.center;
            Vector3 position = _body.position;

            // Кинематическим - до перестановки: так тело телепортируется без промежуточных
            // столкновений, а не проталкивается сквозь платформу и соседний груз
            _wasKinematic = _body.isKinematic;
            _holdsBody = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.isKinematic = true;

            _body.position = new Vector3(center.x, position.y, center.z);
            _body.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            // Пока ящик на платформе, его нельзя ни сбить плечом, ни столкнуть следующим грузом
            _phase = Phase.Docked;
        }

        /// <summary>Вернуть ящик физике таким, каким он к нам попал</summary>
        private void Release()
        {
            // Ящик, который так и не улёгся, мы не трогали - и возвращать ему нечего
            if (_holdsBody && _body != null)
            {
                _body.isKinematic = _wasKinematic;

                // Тело, вернувшееся из кинематики, помнит скорость, с которой его схватили:
                // обнуляем, чтобы поднятый со стола ящик не прыгнул в руках
                if (!_wasKinematic)
                {
                    _body.linearVelocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                }
            }

            _holdsBody = false;
            _cargo = null;
            _body = null;
            _phase = Phase.Empty;
        }
    }
}

using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Подъём, перенос и бросок физических объектов с <see cref="Carriable"/>: точка удержания
    /// считается от прицела, а способ удержания задаётся <see cref="HoldMode"/>
    /// в <see cref="CarryDefinition"/> (смена режима не требует правок кода)
    /// </summary>
    public sealed class Carrier : MonoBehaviour
    {
        [SerializeField] private CarryDefinition _definition;

        [Tooltip("Начало луча захвата и опора точки удержания (обычно камера)")]
        [SerializeField] private Transform _rayOrigin;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private IInputReader _input;
        private Carriable _held;
        private Rigidbody _heldBody;
        private Rigidbody _anchor;
        private ConfigurableJoint _joint;

        // Режим фиксируется на момент захвата, чтобы правка ассета на лету не рассинхронила состояние
        private HoldMode _activeMode;

        // Половина габарита груза: на столько точка удержания держится от препятствий
        private float _heldRadius;

        private CollisionDetectionMode _previousCollisionMode;
        private RigidbodyInterpolation _previousInterpolation;
        private float _previousAngularDamping;

        public bool IsCarrying => _held != null;

        /// <summary>Что именно в руках или <c>null</c> (нужен тем, кто показывает груз игроку)</summary>
        public Carriable Held => _held;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Start()
        {
            _input.Grab += OnGrabPressed;
            _input.Throw += OnThrowPressed;

            ValidateCarriedLayer();
        }

        /// <summary>
        /// Слой переноса бесполезен, если он не разведён с игроком в матрице коллизий: груз начнёт
        /// толкать капсулу и трясти камеру, поэтому ругаемся сразу, а не «когда-нибудь заметим»
        /// </summary>
        private void ValidateCarriedLayer()
        {
            int carried = _definition.CarriedLayer;

            if (carried < 0 || carried > 31)
            {
                Debug.LogError($"CarryDefinition.CarriedLayer = {carried} - это не индекс слоя.", this);
                return;
            }

            int playerLayer = gameObject.layer;

            if (!Physics.GetIgnoreLayerCollision(carried, playerLayer))
            {
                Debug.LogError(
                    $"Слой переноса '{LayerMask.LayerToName(carried)}' не исключён из столкновений " +
                    $"со слоем игрока '{LayerMask.LayerToName(playerLayer)}'. Гружёный объект будет " +
                    "толкать игрока - исправить матрицу в Project Settings → Physics.", this);
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Grab -= OnGrabPressed;
                _input.Throw -= OnThrowPressed;
            }

            if (_anchor != null)
                Destroy(_anchor.gameObject);
        }

        private void OnGrabPressed()
        {
            if (IsCarrying)
                Release(Vector3.zero);
            else
                TryGrab();
        }

        private void OnThrowPressed()
        {
            if (!IsCarrying)
                return;

            Release(_rayOrigin.forward * _definition.ThrowForce);
        }

        private void FixedUpdate()
        {
            if (!IsCarrying)
                return;

            Vector3 holdPoint = GetHoldPoint();
            Vector3 toTarget = holdPoint - _heldBody.position;

            // Объект застрял за геометрией или улетел - отпускаем, чтобы не тянуть его сквозь стены
            if (toTarget.sqrMagnitude > _definition.BreakDistance * _definition.BreakDistance)
            {
                Release(Vector3.zero);
                return;
            }

            if (_activeMode == HoldMode.ConfigurableJoint)
            {
                _anchor.MovePosition(holdPoint);
                _anchor.MoveRotation(_rayOrigin.rotation);
                return;
            }

            Vector3 velocity = toTarget * _definition.FollowSpeed;
            float maxVelocity = _definition.MaxVelocity;

            if (velocity.sqrMagnitude > maxVelocity * maxVelocity)
                velocity = velocity.normalized * maxVelocity;

            _heldBody.linearVelocity = velocity;
        }

        private void TryGrab()
        {
            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);

            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.GrabDistance,
                _definition.CarriableMask, QueryTriggerInteraction.Ignore);

            Carriable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance)
                    continue;

                Carriable candidate;

                if (!TryResolveCarriable(_hits[i].collider, out candidate))
                    continue;

                nearest = candidate;
                nearestDistance = _hits[i].distance;
            }

            if (nearest != null)
                Attach(nearest);
        }

        /// <summary>
        /// Поднимать можно только помеченное <see cref="Carriable"/>: маска слоёв - лишь грубый
        /// фильтр луча, решает наличие компонента (на самом коллайдере или выше по иерархии)
        /// </summary>
        private static bool TryResolveCarriable(Collider hit, out Carriable carriable)
        {
            if (hit.TryGetComponent(out carriable))
                return true;

            carriable = hit.GetComponentInParent<Carriable>();

            return carriable != null;
        }

        /// <summary>
        /// Точка удержания перед камерой, подтянутая до первого препятствия: без этой проверки
        /// взгляд вниз уводит её под пол, груз получает вечную команду «вниз», упирается в землю
        /// и дребезжит прямо посреди экрана
        /// </summary>
        private Vector3 GetHoldPoint()
        {
            Vector3 origin = _rayOrigin.position;
            Vector3 direction = _rayOrigin.forward;
            float distance = _definition.HoldDistance;

            RaycastHit hit;

            if (_heldRadius > 0f && Physics.SphereCast(
                    origin, _heldRadius, direction, out hit, distance,
                    _definition.HoldBlockingMask, QueryTriggerInteraction.Ignore))
            {
                // hit.distance - путь центра сферы, то есть ровно то место,
                // где груз касается препятствия, но ещё не продавливает его
                distance = Mathf.Max(hit.distance, _definition.MinHoldDistance);
            }

            return origin + direction * distance;
        }

        /// <summary>Половина наименьшего габарита груза - радиус для проверки точки удержания</summary>
        private static float MeasureRadius(Carriable carriable)
        {
            Collider[] colliders = carriable.GetComponentsInChildren<Collider>();

            if (colliders.Length == 0)
                return 0f;

            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            Vector3 extents = bounds.extents;

            return Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z));
        }

        private void Attach(Carriable carriable)
        {
            _held = carriable;
            _heldBody = carriable.Rigidbody;
            _activeMode = _definition.HoldMode;
            _heldRadius = MeasureRadius(carriable);

            _previousCollisionMode = _heldBody.collisionDetectionMode;
            _previousInterpolation = _heldBody.interpolation;
            _previousAngularDamping = _heldBody.angularDamping;

            _heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _heldBody.interpolation = RigidbodyInterpolation.Interpolate;
            _heldBody.angularVelocity = Vector3.zero;
            _heldBody.angularDamping = _definition.CarriedAngularDamping;

            // Слой переноса разведён с игроком в матрице коллизий: груз физически не может толкнуть
            // капсулу, сколько бы коллайдеров у него ни было и куда бы ни смотрел игрок
            carriable.OverrideLayers(_definition.CarriedLayer);
            carriable.MarkCarried(true);

            if (_activeMode == HoldMode.ConfigurableJoint)
                AttachJoint();
        }

        private void Release(Vector3 impulse)
        {
            // Destroy отложен до конца кадра, поэтому якорь не выключаем и не двигаем:
            // сустав должен дожить кадр со всё ещё валидным connectedBody
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }

            _held.RestoreLayers();
            _held.MarkCarried(false);

            _heldBody.collisionDetectionMode = _previousCollisionMode;
            _heldBody.interpolation = _previousInterpolation;
            _heldBody.angularDamping = _previousAngularDamping;

            if (impulse != Vector3.zero)
                _heldBody.AddForce(impulse, ForceMode.Impulse);

            _held = null;
            _heldBody = null;
        }

        private void AttachJoint()
        {
            EnsureAnchor();
            _anchor.transform.SetPositionAndRotation(_heldBody.position, _rayOrigin.rotation);

            var drive = new JointDrive
            {
                positionSpring = _definition.JointSpring,
                positionDamper = _definition.JointDamper,
                maximumForce = float.MaxValue,
            };

            _joint = _heldBody.gameObject.AddComponent<ConfigurableJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedBody = _anchor;
            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = Vector3.zero;

            _joint.xMotion = ConfigurableJointMotion.Free;
            _joint.yMotion = ConfigurableJointMotion.Free;
            _joint.zMotion = ConfigurableJointMotion.Free;
            _joint.angularXMotion = ConfigurableJointMotion.Free;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Free;

            _joint.xDrive = drive;
            _joint.yDrive = drive;
            _joint.zDrive = drive;
            _joint.rotationDriveMode = RotationDriveMode.Slerp;
            _joint.slerpDrive = drive;
            _joint.targetRotation = Quaternion.identity;
        }

        /// <summary>
        /// Якорь сустава - голое кинематическое тело без коллайдеров: оно ведёт груз,
        /// но само ни с чем не сталкивается и не может толкнуть игрока
        /// </summary>
        private void EnsureAnchor()
        {
            if (_anchor != null)
                return;

            var anchorObject = new GameObject($"{name}_CarryAnchor");
            _anchor = anchorObject.AddComponent<Rigidbody>();
            _anchor.isKinematic = true;
            _anchor.useGravity = false;
        }
    }
}

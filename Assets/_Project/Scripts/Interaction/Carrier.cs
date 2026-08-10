using CrateExpectations.Core.Hands;
using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Подъём, перенос и бросок физических объектов с <see cref="Carriable"/>: точка удержания
    /// считается от прицела, а способ удержания задаётся <see cref="HoldMode"/>
    /// в <see cref="CarryDefinition"/> (смена режима не требует правок кода).
    /// <para>
    /// Он же держит замах: с грузом в руках кнопка удара копит заряд броска вместо удара.
    /// Разводка этого нажатия НЕ вынесена в отдельного посредника - посреднику пришлось бы
    /// знать и про бой, и про переноску сразу, а знать тут нечего: занятость рук уже
    /// вычисляется в <c>HandsState</c>, и «бить» с «бросать» разведены её же значениями.
    /// Бой пропускает нажатие, когда занятость Carrying, переноска отзывается на него
    /// ровно тогда, когда груз в руках, - одно и то же условие с двух сторон,
    /// а не две копии одной проверки
    /// </para>
    /// <para>
    /// А вот КОГДА поднять и когда положить, решает не переноска. Кнопка руки одна на
    /// всё - ею же жмут станции, - и выбирать цель под прицелом должен тот, кто пускает
    /// луч, то есть <see cref="Interactor"/>. Здесь остаётся исполнение: взять, держать,
    /// отпустить. Своего луча захвата у переноски больше нет
    /// </para>
    /// </summary>
    public sealed class Carrier : MonoBehaviour, ICarryStateSource
    {
        [SerializeField] private CarryDefinition _definition;

        [Tooltip("Опора точки удержания и направление броска (обычно камера)")]
        [SerializeField] private Transform _rayOrigin;

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

        // Замах. Тот же класс, которым бой копит заряженный удар, но ЭКЗЕМПЛЯР свой:
        // общий сделал бы замах ящиком боевой занятостью, и руки посреди броска
        // уехали бы из Carrying в Combat
        private readonly HoldCharge _throwCharge = new();

        /// <inheritdoc />
        public bool IsCarrying => _held != null;

        /// <summary>
        /// Замах уже копится (мёртвая зона пройдена). Пригодится аниматору,
        /// когда у броска появится анимация
        /// </summary>
        public bool IsCharging => _throwCharge.IsCharging;

        /// <summary>Насколько замах полон, 0..1. Тоже под будущую анимацию</summary>
        public float ChargeT => _throwCharge.ChargeT;

        /// <summary>Что именно в руках или <c>null</c> (нужен тем, кто показывает груз игроку)</summary>
        public Carriable Held => _held;

        /// <summary>
        /// С какого расстояния игрок дотягивается до груза. Наружу - потому что луч
        /// теперь чужой: цель под прицелом ищет <see cref="Interactor"/>, а вот НА КАКОЙ
        /// дистанции груз считается досягаемым, знает ассет переноски
        /// </summary>
        public float GrabDistance => _definition.GrabDistance;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Start()
        {
            _input.Attack += OnAttackPressed;
            _input.AttackReleased += OnAttackReleased;

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
                _input.Attack -= OnAttackPressed;
                _input.AttackReleased -= OnAttackReleased;
            }

            if (_anchor != null)
                Destroy(_anchor.gameObject);
        }

        /// <summary>
        /// Взять груз в руки. Решение уже принято снаружи - здесь только исполнение,
        /// поэтому и проверок тут ровно одна: две ноши сразу физически не бывает
        /// </summary>
        public void Grab(Carriable carriable)
        {
            if (IsCarrying || carriable == null)
                return;

            Attach(carriable);
        }

        /// <summary>Положить груз там, где он висит. Без импульса - это не бросок</summary>
        public void Drop()
        {
            if (!IsCarrying)
                return;

            Release(Vector3.zero);
        }

        /// <summary>
        /// Годится ли то, во что упёрся чужой луч, чтобы это поднять. Знание «что можно
        /// поднять» остаётся здесь, у ассета переноски: <see cref="Interactor"/> владеет
        /// лучом, но не должен заводить вторую копию маски и дистанции захвата.
        /// <para>
        /// Маска слоёв - лишь грубый фильтр; решает наличие <see cref="Carriable"/>
        /// на самом коллайдере или выше по иерархии
        /// </para>
        /// </summary>
        /// <param name="hit">Коллайдер под прицелом</param>
        /// <param name="distance">Дистанция до него по лучу, м</param>
        /// <param name="carriable">Что именно поднимется</param>
        public bool TryResolveGrabTarget(Collider hit, float distance, out Carriable carriable)
        {
            carriable = null;

            if (hit == null || distance > _definition.GrabDistance)
                return false;

            if ((_definition.CarriableMask.value & (1 << hit.gameObject.layer)) == 0)
                return false;

            return TryResolveCarriable(hit, out carriable);
        }

        /// <summary>
        /// Кнопку удара нажали. С грузом в руках она копит замах, а не бьёт: удар при
        /// занятости Carrying не пускает бой у себя, и вторую копию этого правила
        /// здесь заводить нечего - переноске достаточно знать, что груз у неё
        /// </summary>
        private void OnAttackPressed()
        {
            if (!IsCarrying)
                return;

            _throwCharge.Begin(_definition.ChargeDuration, _definition.ActivationThreshold);
        }

        /// <summary>
        /// Кнопку удара отпустили. Замах короче мёртвой зоны не делает НИЧЕГО: короткий
        /// клик с грузом в руках не должен ни ронять его, ни толкать слабым тычком, -
        /// иначе игрок терял бы ящик каждый раз, когда промахнулся кнопкой
        /// </summary>
        private void OnAttackReleased()
        {
            if (!IsCarrying || !_throwCharge.IsCharging)
            {
                _throwCharge.Cancel();
                return;
            }

            // Второго пути броска нет: сила считается по заряду, а дальше это тот же
            // Release, что и у броска с руки. Заряд гасится внутри него - вместе
            // со всеми остальными способами лишиться груза
            Release(_rayOrigin.forward * _definition.ThrowForceAt(_throwCharge.ChargeT));
        }

        private void Update()
        {
            // Возврат Tick тут не нужен: полный замах никуда сам не улетает, он ждёт
            // отпускания кнопки, стоя на единице. Этим бросок и отличается от удара
            _throwCharge.Tick(Time.deltaTime);
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

            Vector3 point = origin + direction * distance;

            if (!_throwCharge.IsCharging)
                return point;

            // Замах прибавляется ПОСЛЕ подтяжки и кламба, а не к дистанции до них: иначе
            // кламп минимальной дистанции дрался бы с замахом и на близких стенах ящик
            // дёргался бы вместо того, чтобы уехать назад.
            // Смещение камерное, а не мировое: замах должен уводить груз к плечу игрока,
            // куда бы он ни смотрел. Груз при этом едет НА игрока и может влезть
            // в геометрию за спиной - это принято осознанно
            return point + _rayOrigin.rotation * _definition.WindupOffsetAt(_throwCharge.ChargeT);
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

            // Единственное место, где гасится замах, - и именно поэтому оно здесь: через
            // Release проходят ВСЕ способы лишиться груза (кнопка, бросок, отрыв
            // по BreakDistance), и заряд не может пережить ни один из них
            _throwCharge.Cancel();
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

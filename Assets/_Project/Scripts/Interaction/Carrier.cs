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
        // Короче этого вектор «до точки удержания» считается вырожденным: направления у него
        // уже нет, а нормализация даёт NaN
        private const float MinHoldDirectionLength = 0.0001f;

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

        // Груз уже отпущен кнопкой, но ещё не вылетел: ждём, пока руки дойдут до апекса
        private Vector3 _pendingImpulse;
        private float _releaseTimer;
        private bool _throwPending;

        // Остаток хвоста занятости после вылета груза
        private float _throwTail;

        /// <summary>
        /// Груз в руках ФИЗИЧЕСКИ. Не то же самое, что <see cref="IsCarrying"/>: после
        /// вылета груза руки ещё доигрывают бросок, и заняты они, а держат - уже ничего
        /// </summary>
        private bool IsHolding => _held != null;

        /// <summary>
        /// Руки заняты переноской. Шире, чем «груз в руках»: сюда входит и хвост после
        /// броска.
        /// <para>
        /// Хвост нужен не переноске, а КАДРУ. Руки вьюмодели видно ровно пока занятость
        /// не <c>Free</c>, а груз улетает мгновенно - без хвоста руки гасли бы в тот же
        /// кадр, в который стартует проводка броска, и саму проводку не увидели бы ни разу.
        /// Занятость, а не отдельный флаг «идёт анимация»: всё, что запрещено с грузом
        /// в руках, должно быть запрещено и посреди броска, а это уже описано занятостью -
        /// заводить второе правило значило бы держать две копии одного запрета
        /// </para>
        /// </summary>
        public bool IsCarrying => IsHolding || _throwTail > 0f;

        /// <summary>
        /// Груз пошёл в бросок: кнопку отпустили, анимация выброса должна стартовать.
        /// Момент НЕ совпадает с вылетом груза - тот отложен на <see cref="CarryDefinition.ReleaseDelay"/>,
        /// потому что руки к апексу выброса приходят не сразу
        /// </summary>
        public event System.Action Thrown;

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

        /// <summary>
        /// Сколько длится проводка взятия, с. Наружу по той же причине, что и
        /// <see cref="GrabDistance"/>: число живёт в ассете переноски, а нужно оно тому,
        /// кто ужимает клип взятия до этой длительности
        /// </summary>
        public float GrabDuration => _definition.GrabDuration;

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
            // Проверка идёт по IsCarrying, а не по «груз в руках»: посреди хвоста броска
            // руки ещё доигрывают выброс, и подсунуть в них следующий ящик нельзя
            if (IsCarrying || carriable == null)
                return;

            Attach(carriable);
        }

        /// <summary>Положить груз там, где он висит. Без импульса - это не бросок</summary>
        public void Drop()
        {
            if (!IsHolding)
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
            // Копить замах можно только тем, что реально в руках. Посреди уже начатого
            // броска - нельзя: там груз либо ждёт вылета, либо уже улетел, и второй
            // замах поверх первого означал бы бросок неизвестно чего
            if (!IsHolding || _throwPending)
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
            if (!IsHolding || _throwPending || !_throwCharge.IsCharging)
            {
                _throwCharge.Cancel();
                return;
            }

            Vector3 impulse = _rayOrigin.forward * _definition.ThrowForceAt(_throwCharge.ChargeT);

            // Анимация выброса стартует ЗДЕСЬ, а груз улетает позже: событие поднимается
            // до вылета, иначе клип начинался бы с уже пустых рук
            Thrown?.Invoke();

            // Замах отыгран. Гасим его сразу, не дожидаясь вылета: точка удержания
            // возвращается из-за плеча вперёд, и груз едет туда сам, обычным следованием.
            // Именно это и читается как «руки вынесли ящик вперёд» - отдельной проводки
            // для груза не нужно
            _throwCharge.Cancel();

            if (_definition.ReleaseDelay <= 0f)
            {
                Release(impulse);
                return;
            }

            _pendingImpulse = impulse;
            _releaseTimer = _definition.ReleaseDelay;
            _throwPending = true;
        }

        private void Update()
        {
            // Возврат Tick тут не нужен: полный замах никуда сам не улетает, он ждёт
            // отпускания кнопки, стоя на единице. Этим бросок и отличается от удара
            _throwCharge.Tick(Time.deltaTime);

            // Хвост тикает ПЕРЕД отложенным вылетом: иначе хвост, заведённый вылетом
            // в этом же кадре, тут же потерял бы свой первый кадр
            TickThrowTail(Time.deltaTime);
            TickPendingThrow(Time.deltaTime);
        }

        /// <summary>
        /// Отложенный вылет груза. Груз всё это время ещё в руках и едет за точкой
        /// удержания - к моменту импульса он уже движется вперёд, а не стартует с места
        /// </summary>
        private void TickPendingThrow(float deltaTime)
        {
            if (!_throwPending)
                return;

            _releaseTimer -= deltaTime;

            if (_releaseTimer > 0f)
                return;

            // Груз могли отобрать за время задержки - положить кнопкой или оторвать
            // по BreakDistance. Тогда бросать уже нечего, и Release это уже сделал
            if (IsHolding)
                Release(_pendingImpulse);
        }

        /// <summary>
        /// Хвост занятости после вылета. Обычное поле со временем, а не отложенная задача:
        /// уничтожение объекта не требует отмены, а выход из play mode не оставляет
        /// висящего продолжения
        /// </summary>
        private void TickThrowTail(float deltaTime)
        {
            if (_throwTail <= 0f)
                return;

            _throwTail = Mathf.Max(0f, _throwTail - deltaTime);
        }

        private void FixedUpdate()
        {
            // Хвост броска сюда не пускаем: вести за точкой удержания уже нечего
            if (!IsHolding)
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
        /// и дребезжит прямо посреди экрана.
        /// <para>
        /// Доводка <see cref="CarryDefinition.HoldOffset"/> входит ДО подтяжки, а замах - ПОСЛЕ.
        /// Разница принципиальная: доводка задаёт, где груз висит в спокойном состоянии, и
        /// обязана считаться «нормальным местом», от которого стены его отодвигают; замах же
        /// уводит груз к плечу намеренно, и подтягивать его обратно к стене нельзя
        /// </para>
        /// <para>
        /// Числа читаются из ассета каждый физшаг и нигде не кэшируются - иначе правку
        /// в play mode пришлось бы ждать до следующего захвата
        /// </para>
        /// </summary>
        private Vector3 GetHoldPoint()
        {
            Vector3 origin = _rayOrigin.position;
            Quaternion rotation = _rayOrigin.rotation;

            // Доводка камерная, поэтому она меняет не только длину, но и НАПРАВЛЕНИЕ луча
            // подтяжки: проверять препятствия вдоль forward, а держать груз сбоку от него -
            // значит проверять не то место, где груз висит
            Vector3 toHold = rotation * (Vector3.forward * _definition.HoldDistance + _definition.HoldOffset);
            float distance = toHold.magnitude;

            // Доводка может погасить дистанцию целиком - направления у нулевого вектора нет,
            // и падать в NaN из-за подобранного «в ноль» смещения точка удержания не должна
            Vector3 direction = distance > MinHoldDirectionLength
                ? toHold / distance
                : _rayOrigin.forward;

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
            return point + rotation * _definition.WindupOffsetAt(_throwCharge.ChargeT);
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

            // По той же причине здесь снимается и отложенный вылет: груз, отобранный
            // за время задержки, не должен получить импульс задним числом
            _throwPending = false;
            _releaseTimer = 0f;

            // Хвост занятости - только настоящему броску. Положенный на пол ящик
            // никакой анимации не доигрывает, и держать под него руки в кадре незачем
            _throwTail = impulse != Vector3.zero ? _definition.ThrowTailDuration : 0f;
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

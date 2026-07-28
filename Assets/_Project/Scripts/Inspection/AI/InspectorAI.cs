using System;
using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Stations;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.StateMachine;
using CrateExpectations.Inspection.Events;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Инспектор порта. Сам он почти ничего не делает: держит generic
    /// <see cref="StateMachine"/> из <c>Core</c> (второе её применение после game flow),
    /// тикает её и раздаёт состояниям то, что им нужно, через <see cref="IInspectorContext"/>.
    /// Поведение живёт в состояниях, правила улик - в <see cref="ClueEvaluator"/>,
    /// числа и тексты - в ассетах. Тонкий слой ввода/вывода, как и положено
    /// <see cref="MonoBehaviour"/>.
    /// </summary>
    public sealed class InspectorAI : MonoBehaviour, IInspectorContext
    {
        [SerializeField] private InspectionDefinition _definition;

        [Tooltip("Характер инспектора: что проверяет, когда задерживает. Меняется без правок кода")]
        [SerializeField] private InspectorProfile _profile;

        [Tooltip("Регламент порта: как должен выглядеть ящик с заявленным содержимым")]
        [SerializeField] private PortRegulationsDefinition _regulations;

        [Tooltip("Зона досмотра - та же, что и на станциях маскировки")]
        [SerializeField] private CargoPlacementZone _zone;

        [Tooltip("Пост: место и поза, в которые инспектор возвращается между досмотрами")]
        [SerializeField] private Transform _post;

        [Tooltip("Точка, с которой инспектор осматривает груз")]
        [SerializeField] private Transform _examinePoint;

        [Tooltip("Точки обхода поста по порядку. Пусто - инспектор просто стоит на посту")]
        [SerializeField] private Transform[] _patrolPoints = Array.Empty<Transform>();

        private readonly StateMachine _machine = new();

        private ClueEvaluator _evaluator;
        private CarriedCargoSensor _sensor;
        private InspectorIdleState _idle;
        private InspectorPatrolState _patrol;
        private InspectorNoticeState _notice;
        private InspectorReturnState _return;
        private InspectorApproachState _approach;
        private InspectorExamineState _examine;
        private InspectorVerdictState _verdict;

        private CancellationTokenSource _stateCts;
        private IInspectorVoice _voice;
        private IEventBus _bus;

        // Ящик, по которому вердикт уже вынесен: пока он стоит в зоне, второй раз
        // его не досматривают. Сбрасывается, как только зона опустеет
        private CargoBox _judged;

        private bool _started;

        /// <summary>Текущая фаза - для отладки и будущего UI</summary>
        public InspectorPhase Phase { get; private set; } = InspectorPhase.Idle;

        /// <summary>
        /// Фаза сменилась. Наружу отдаётся именно уведомление, а не право вмешаться:
        /// представление (анимация, звук) подписывается сюда и не заводит своих условий,
        /// а состояния по-прежнему не знают, что их кто-то отыгрывает
        /// </summary>
        public event Action<InspectorPhase> PhaseChanged;

        /// <inheritdoc />
        public InspectionDefinition Definition => _definition;

        /// <inheritdoc />
        public InspectorProfile Profile => _profile;

        /// <inheritdoc />
        public InspectorLinesDefinition Lines => _profile.Lines;

        /// <inheritdoc />
        public CargoPlacementZone Zone => _zone;

        /// <inheritdoc />
        public InspectorMotor Motor { get; private set; }

        /// <inheritdoc />
        public IInspectorVoice Voice => _voice;

        /// <inheritdoc />
        public Transform Post => _post;

        /// <inheritdoc />
        public Transform ExaminePoint => _examinePoint;

        /// <inheritdoc />
        public IReadOnlyList<Transform> PatrolPoints => _patrolPoints;

        /// <inheritdoc />
        public CancellationToken StateToken =>
            _stateCts != null ? _stateCts.Token : CancellationToken.None;

        /// <inheritdoc />
        public bool AwaitsInspection
        {
            get
            {
                CargoBox occupant = _zone.Occupant;
                return occupant != null && !ReferenceEquals(occupant, _judged);
            }
        }

        /// <inheritdoc />
        public CargoBox ApproachingCargo => _sensor.FindNearest();

        /// <inheritdoc />
        public InspectionCase Case { get; private set; }

        [Inject]
        public void Construct(IInspectorVoice voice, IEventBus bus)
        {
            _voice = voice;
            _bus = bus;
        }

        private void Awake()
        {
            if (!IsWiredUp())
            {
                enabled = false;
                return;
            }

            // Невнимательность инспектора - единственная случайность в досмотре, и она
            // воспроизводима: seed фиксируется ассетом, а не берётся из воздуха
            int seed = _definition.OverlookSeed != 0
                ? _definition.OverlookSeed
                : Random.Range(int.MinValue, int.MaxValue);
            _evaluator = new ClueEvaluator(new SeededChanceSource(seed));

            Motor = new InspectorMotor(transform, _definition);
            _sensor = new CarriedCargoSensor(transform, _definition);

            _idle = new InspectorIdleState(this);
            _patrol = new InspectorPatrolState(this);
            _notice = new InspectorNoticeState(this);
            _return = new InspectorReturnState(this);
            _approach = new InspectorApproachState(this);
            _examine = new InspectorExamineState(this);
            _verdict = new InspectorVerdictState(this);
        }

        private void OnEnable()
        {
            _zone.OccupantChanged += OnOccupantChanged;

            // На первом включении машину заводит Start: до него VContainer ещё не
            // проставил зависимости, а состояния сразу лезут в вывод реплик
            if (_started) GoTo(InspectorPhase.Idle);
        }

        private void Start()
        {
            _started = true;
            GoTo(InspectorPhase.Idle);
        }

        private void OnDisable()
        {
            _zone.OccupantChanged -= OnOccupantChanged;

            CancelState();
            _machine.ChangeState(null);

            // Инспектора выключили посреди досмотра - сцену за собой прибираем сами
            _zone.ClearColorOverride();
            _voice?.Clear();
        }

        private void OnDestroy() => CancelState();

        private void Update() => _machine.Tick(Time.deltaTime);

        /// <inheritdoc />
        public void GoTo(InspectorPhase phase)
        {
            if (Phase == phase && _machine.Current != null) return;

            Phase = phase;

            // В Idle открытых дел не остаётся: осмотр, прерванный на полпути, не должен
            // потом "догнать" инспектора вердиктом по ящику, которого на столе давно нет
            if (phase == InspectorPhase.Idle) Case = default;

            // Новый токен на каждое состояние: всё асинхронное, что осталось от прежнего,
            // сворачивается прямо здесь и не догоняет нас репликами из прошлого
            ResetStateToken();

            // До ChangeState: Enter() состояния вправе тут же заказать другую фазу, и тогда
            // подписчик должен увидеть переходы в том порядке, в каком их заказывали
            PhaseChanged?.Invoke(phase);

            _machine.ChangeState(StateFor(phase));
        }

        /// <inheritdoc />
        public Verdict OpenCase(CargoBox cargo)
        {
            InspectionSubject subject = _regulations.CreateSubject(cargo.State, cargo.Identity);
            Verdict verdict = _evaluator.Evaluate(subject, _profile.Policy);

            Case = new InspectionCase(cargo, verdict);
            return verdict;
        }

        /// <inheritdoc />
        public void CloseCase()
        {
            if (!Case.IsOpen) return;

            _judged = Case.Cargo;

            // Дальше - не наше дело. Кто заплатит, кто оштрафует и что станет с ящиком,
            // решают подписчики; модуль досмотра про экономику и контракты не знает
            _bus.Publish(new CargoInspected(Case.Cargo, _profile, Case.Verdict));
        }

        private IState StateFor(InspectorPhase phase) => phase switch
        {
            InspectorPhase.Patrol => _patrol,
            InspectorPhase.Notice => _notice,
            InspectorPhase.Return => _return,
            InspectorPhase.Approach => _approach,
            InspectorPhase.Examine => _examine,
            InspectorPhase.Verdict => _verdict,
            _ => _idle,
        };

        /// <summary>Зона опустела - значит следующий поставленный ящик снова подлежит досмотру</summary>
        private void OnOccupantChanged(CargoBox occupant)
        {
            if (occupant == null) _judged = null;
        }

        private void ResetStateToken()
        {
            CancelState();
            _stateCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private void CancelState()
        {
            if (_stateCts == null) return;

            _stateCts.Cancel();
            _stateCts.Dispose();
            _stateCts = null;
        }

        /// <summary>
        /// Проверка ссылок на старте: инспектор без зоны или без регламента молча не работал бы,
        /// а так дыра в сцене видна первой же строкой в консоли
        /// </summary>
        private bool IsWiredUp()
        {
            if (_definition == null) return Missing("тайминги осмотра (InspectionDefinition)");
            if (_profile == null) return Missing("профиль (InspectorProfile)");
            if (_profile.Lines == null) return Missing("реплики: у профиля не заполнено поле Lines");
            if (_regulations == null) return Missing("регламент порта (PortRegulationsDefinition)");
            if (_zone == null) return Missing("зона досмотра (CargoPlacementZone)");
            if (_post == null) return Missing("пост (Post)");
            if (_examinePoint == null) return Missing("точка осмотра (ExaminePoint)");

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Инспектору '{name}' не назначено: {what}. Досмотра не будет.", this);
            return false;
        }
    }
}

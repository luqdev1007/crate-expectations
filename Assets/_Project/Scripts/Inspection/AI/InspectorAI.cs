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
    public sealed class InspectorAI : MonoBehaviour, IInspectorContext
    {
        [Tooltip("Тайминги и шаги осмотра")]
        [SerializeField] private InspectionDefinition _definition;

        [Tooltip("Характер инспектора: что проверяет, когда задерживает")]
        [SerializeField] private InspectorProfile _profile;

        [Tooltip("Регламент порта: как должен выглядеть ящик с заявленным содержимым")]
        [SerializeField] private PortRegulationsDefinition _regulations;

        [SerializeField] private CargoPlacementZone _zone;
        [SerializeField] private Transform _post;
        [SerializeField] private Transform _examinePoint;

        [SerializeField] private Transform[] _patrolPoints = Array.Empty<Transform>();

        [SerializeField] private ExamineFocusMarker _focus;

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

        private CargoBox _judged;

        private bool _started;

        public InspectorPhase Phase { get; private set; } = InspectorPhase.Idle;

        public event Action<InspectorPhase> PhaseChanged;

        public InspectionDefinition Definition => _definition;

        public InspectorProfile Profile => _profile;

        public InspectorLinesDefinition Lines => _profile.Lines;

        public CargoPlacementZone Zone => _zone;

        public InspectorMotor Motor { get; private set; }

        public IInspectorVoice Voice => _voice;

        public ExamineFocusMarker Focus => _focus;

        public Transform Post => _post;

        public Transform ExaminePoint => _examinePoint;

        public IReadOnlyList<Transform> PatrolPoints => _patrolPoints;

        public CancellationToken StateToken =>
            _stateCts != null ? _stateCts.Token : CancellationToken.None;

        public bool AwaitsInspection
        {
            get
            {
                CargoBox occupant = _zone.Occupant;
                return occupant != null && !ReferenceEquals(occupant, _judged);
            }
        }

        public CargoBox ApproachingCargo => _sensor.FindNearest();

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

            if (_started) 
                GoTo(InspectorPhase.Idle);
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

            _focus.Hide();
            _zone.ClearColorOverride();
            _voice?.Clear();
        }

        private void OnDestroy() => CancelState();

        private void Update() => _machine.Tick(Time.deltaTime);

        /// <inheritdoc />
        public void GoTo(InspectorPhase phase)
        {
            if (Phase == phase && _machine.Current != null)
                return;

            Phase = phase;

            if (phase == InspectorPhase.Idle) 
                Case = default;

            ResetStateToken();

            PhaseChanged?.Invoke(phase);

            _machine.ChangeState(StateFor(phase));
        }

        public Verdict OpenCase(CargoBox cargo)
        {
            InspectionSubject subject = _regulations.CreateSubject(cargo.State, cargo.Identity);
            Verdict verdict = _evaluator.Evaluate(subject, _profile.Policy);

            Case = new InspectionCase(cargo, verdict);

            return verdict;
        }

        public void CloseCase()
        {
            if (!Case.IsOpen) 
                return;

            _judged = Case.Cargo;

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

        private void OnOccupantChanged(CargoBox occupant)
        {
            if (occupant == null) 
                _judged = null;
        }

        private void ResetStateToken()
        {
            CancelState();
            _stateCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private void CancelState()
        {
            if (_stateCts == null)
                return;

            _stateCts.Cancel();
            _stateCts.Dispose();
            _stateCts = null;
        }

        private bool IsWiredUp()
        {
            if (_definition == null)
                return Missing("тайминги осмотра (InspectionDefinition)");

            if (_profile == null) 
                return Missing("профиль (InspectorProfile)");

            if (_profile.Lines == null)
                return Missing("реплики: у профиля не заполнено поле Lines");

            if (_regulations == null) 
                return Missing("регламент порта (PortRegulationsDefinition)");

            if (_zone == null)
                return Missing("зона досмотра (CargoPlacementZone)");

            if (_post == null) 
                return Missing("пост (Post)");

            if (_examinePoint == null) 
                return Missing("точка осмотра (ExaminePoint)");

            if (_focus == null) 
                return Missing("указка (ExamineFocusMarker)");

            // if test

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Инспектору '{name}' не назначено: {what}. Досмотра не будет.", this);

            return false;
        }
    }
}

using CrateExpectations.Inspection.AI;
using UnityEngine;

namespace CrateExpectations.Inspection.View
{
    /// <summary>
    /// Переводит фазы инспектора в параметры <see cref="Animator"/>. Именно адаптер, а не
    /// поведение: состояния FSM про аниматор не знают и знать не должны - они меняют фазу,
    /// а чем её отыграть, решает этот компонент. Поэтому анимацию можно снять с NPC целиком,
    /// удалив один объект, и досмотр продолжит работать
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class InspectorAnimationView : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ExaminingId = Animator.StringToHash("Examining");
        private static readonly int ExamineId = Animator.StringToHash("Examine");
        private static readonly int ApproveId = Animator.StringToHash("Approve");
        private static readonly int RejectId = Animator.StringToHash("Reject");
        private static readonly int NoticeId = Animator.StringToHash("Notice");

        [Tooltip("Чью фазу отыгрываем. Обычно - InspectorAI на родителе")]
        [SerializeField] private InspectorAI _inspector;    
        [SerializeField][Min(0.1f)] private float _speedSmoothing = 9f;

        private Animator _animator;
        private Transform _body;
        private Vector3 _previousPosition;
        private float _speed;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_inspector == null)
            {
                Debug.LogError($"Анимации инспектора '{name}' не назначен InspectorAI - играть будет нечего.", this);
                enabled = false;
                return;
            }
            // Тело двигает InspectorMotor через Transform. Root motion добавил бы к этому
            // ещё и смещение из клипа - NPC поехал бы сам по себе, мимо своих точек
            _animator.applyRootMotion = false;

            _body = _inspector.transform;
            _previousPosition = _body.position;
        }

        private void OnEnable()
        {
            _inspector.PhaseChanged += OnPhaseChanged;

            OnPhaseChanged(_inspector.Phase);
        }

        private void OnDisable() => _inspector.PhaseChanged -= OnPhaseChanged;

        /// <summary>
        /// Скорость берём из фактического смещения тела, а не из мотора: так представление
        /// остаётся односторонним - мотор не знает, что его кто-то анимирует, и любое
        /// будущее перемещение (патруль, отход, толчок) попадёт в ноги само
        /// </summary>
        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            Vector3 position = _body.position;
            float travelled = Vector3.Distance(position, _previousPosition);
            _previousPosition = position;

            _speed = Mathf.Lerp(_speed, travelled / deltaTime, _speedSmoothing * deltaTime);

            // В блендтри уходит доля от маршевой скорости, а не метры в секунду: тогда
            // WalkSpeed в ассете можно крутить, не трогая пороги в контроллере
            float reference = _inspector.Definition.WalkSpeed;
            _animator.SetFloat(SpeedId, reference > 0f ? Mathf.Clamp01(_speed / reference) : 0f);
        }

        private void OnPhaseChanged(InspectorPhase phase)
        {
            // Триггеры аниматора живут до первого срабатывания. Если фаза сменилась раньше,
            // чем жест успел начаться, невыстреливший триггер обязан умереть здесь - иначе
            // инспектор кивнёт "проезжай" через полминуты, посреди совсем другого дела
            _animator.ResetTrigger(ExamineId);
            _animator.ResetTrigger(ApproveId);
            _animator.ResetTrigger(RejectId);
            _animator.ResetTrigger(NoticeId);

            // Осмотр длится столько, сколько займут шаги профиля: у дотошного вчетверо
            // дольше, чем у ленивого. Поэтому не жест фиксированной длины, а флаг -
            // "раздумывает, пока не скажут обратное"
            _animator.SetBool(ExaminingId, phase == InspectorPhase.Examine);

            switch (phase)
            {
                case InspectorPhase.Examine:
                    _animator.SetTrigger(ExamineId);
                    break;

                case InspectorPhase.Notice:
                    _animator.SetTrigger(NoticeId);
                    break;

                case InspectorPhase.Verdict:
                    ShowVerdict();
                    break;
            }
        }

        private void ShowVerdict()
        {
            if (!_inspector.Case.IsOpen) 
                return;

            bool passed = _inspector.Case.Verdict.Outcome == VerdictOutcome.Pass;
            _animator.SetTrigger(passed ? ApproveId : RejectId);
        }
    }
}

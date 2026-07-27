using CrateExpectations.Inspection.AI;
using UnityEngine;

namespace CrateExpectations.Inspection.View
{
    [RequireComponent(typeof(Animator))]
    public sealed class InspectorAnimationView : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ExaminingId = Animator.StringToHash("Examining");
        private static readonly int ExamineId = Animator.StringToHash("Examine");
        private static readonly int ApproveId = Animator.StringToHash("Approve");
        private static readonly int RejectId = Animator.StringToHash("Reject");
        private static readonly int NoticeId = Animator.StringToHash("Notice");

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

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            Vector3 position = _body.position;
            float travelled = Vector3.Distance(position, _previousPosition);
            _previousPosition = position;

            _speed = Mathf.Lerp(_speed, travelled / deltaTime, _speedSmoothing * deltaTime);

            float reference = _inspector.Definition.WalkSpeed;
            _animator.SetFloat(SpeedId, reference > 0f ? Mathf.Clamp01(_speed / reference) : 0f);
        }

        private void OnPhaseChanged(InspectorPhase phase)
        {
            _animator.ResetTrigger(ExamineId);
            _animator.ResetTrigger(ApproveId);
            _animator.ResetTrigger(RejectId);
            _animator.ResetTrigger(NoticeId);

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

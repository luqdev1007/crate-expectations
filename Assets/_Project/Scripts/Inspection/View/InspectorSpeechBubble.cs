using CrateExpectations.Inspection.AI;
using TMPro;
using UnityEngine;

namespace CrateExpectations.Inspection.View
{
    /// <summary>
    /// Реплика инспектора в мировом пространстве: плашка над головой, всегда развёрнутая к камере.
    /// Текст приходит по тому же каналу, что и раньше - через <see cref="IInspectorVoice"/>,
    /// а видимость решает фаза FSM: говорит только на осмотре и вердикте.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InspectorSpeechBubble : MonoBehaviour, IInspectorVoice
    {
        [Tooltip("Чью фазу слушаем: в патруле и на подходе пузырь молчит")]
        [SerializeField] private InspectorAI _inspector;

        [Tooltip("Поле реплики")]
        [SerializeField] private TMP_Text _line;

        [Tooltip("Камера игрока. Пусто - возьмём Camera.main один раз в Awake")]
        [SerializeField] private Camera _camera;

        private Transform _transform;
        private Transform _eye;
        private Canvas _canvas;
        private CanvasGroup _group;

        private Color _speechColor;
        private bool _hasLine;
        private bool _phaseSpeaks;
        private bool _ready;

        private void Awake()
        {
            _transform = transform;
            _canvas = GetComponent<Canvas>();
            _group = GetComponent<CanvasGroup>();

            if (!IsWiredUp())
            {
                enabled = false;
                return;
            }

            _eye = _camera.transform;
            _speechColor = _line.color;
            _ready = true;

            Mute();
        }

        private void OnEnable()
        {
            _inspector.PhaseChanged += OnPhaseChanged;

            OnPhaseChanged(_inspector.Phase);
        }

        private void OnDisable()
        {
            _inspector.PhaseChanged -= OnPhaseChanged;

            _hasLine = false;
            Mute();
        }

        /// <inheritdoc />
        public void Say(string line)
        {
            if (!_ready)
                return;

            _line.text = line;
            _line.color = _speechColor;

            _hasLine = !string.IsNullOrEmpty(line);
        }

        /// <inheritdoc />
        public void ShowVerdict(in VerdictReport report)
        {
            Say(report.Speech);

            if (_ready)
                _line.color = report.Accent;
        }

        /// <inheritdoc />
        public void Clear() => Say(string.Empty);

        private void LateUpdate()
        {
            // Догоняем камеру именно в LateUpdate: к этому моменту Cinemachine уже подвинул её
            // за этот кадр, иначе пузырь отставал бы на кадр и дрожал при повороте головы.
            if (Fade(Time.deltaTime) <= 0f)
                return;

            // Не LookAt: тот целится осью Z в точку и оставляет up мировым, поэтому при взгляде
            // снизу или сверху плашку кренит, а над головой инспектора - переворачивает.
            // Копия ориентации камеры держит пузырь строго параллельным экрану с любого ракурса.
            _transform.rotation = _eye.rotation;
        }

        private float Fade(float deltaTime)
        {
            bool visible = _hasLine && _phaseSpeaks;
            float target = visible ? 1f : 0f;
            float alpha = _group.alpha;

            if (alpha == target)
                return alpha;

            InspectionDefinition definition = _inspector.Definition;

            float duration = visible
                ? definition.SpeechFadeInSeconds
                : definition.SpeechFadeOutSeconds;

            alpha = duration > 0f
                ? Mathf.MoveTowards(alpha, target, deltaTime / duration)
                : target;

            _group.alpha = alpha;
            _canvas.enabled = alpha > 0f;

            return alpha;
        }

        private void OnPhaseChanged(InspectorPhase phase) =>
            _phaseSpeaks = phase is InspectorPhase.Examine or InspectorPhase.Verdict;

        private void Mute()
        {
            _phaseSpeaks = false;
            _group.alpha = 0f;
            _canvas.enabled = false;
        }

        private bool IsWiredUp()
        {
            if (_inspector == null)
                return Missing("инспектор (InspectorAI)");

            if (_inspector.Definition == null)
                return Missing("тайминги осмотра: у инспектора не заполнен InspectionDefinition");

            if (_line == null)
                return Missing("поле реплики (TMP_Text)");

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return Missing("камера игрока, и Camera.main тоже не нашлась");

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Пузырю реплик '{name}' не назначено: {what}. Реплик над головой не будет.", this);

            return false;
        }
    }
}

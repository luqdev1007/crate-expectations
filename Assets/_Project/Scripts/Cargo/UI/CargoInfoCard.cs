using CrateExpectations.Core.View;
using CrateExpectations.Interaction;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Cargo.UI
{
    /// <summary>
    /// Карточка груза в мировом пространстве: висит над тем ящиком, на который игрок смотрит.
    /// Пока ящик в руках, карточек нет вовсе - ни над несомым, ни над остальными. Своего луча
    /// не пускает: цель берёт у <see cref="Interactor"/>, поэтому карточка всегда одна.
    /// Текст пересобирается только на смене цели и по <see cref="CargoBox.StateChanged"/>,
    /// в кадре остаются лишь позиция, масштаб и альфа
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(CameraBillboard))]
    public sealed class CargoInfoCard : MonoBehaviour
    {
        [Tooltip("Настройки и тексты карточки")]
        [SerializeField] private CargoOverlayDefinition _definition;

        [Tooltip("Заголовок: истинный тип груза")]
        [SerializeField] private TMP_Text _title;

        [Tooltip("Строки характеристик: окраска, печать, заявленное")]
        [SerializeField] private TMP_Text _stats;

        [Tooltip("Строка расхождения: подсвечивается цветом из настроек")]
        [SerializeField] private TMP_Text _divergence;

        private Transform _transform;
        private Canvas _canvas;
        private CanvasGroup _group;
        private CameraBillboard _billboard;

        private Interactor _interactor;
        private Carrier _carrier;

        private RectTransform _rect;

        private CargoBox _target;
        private Transform _targetTransform;
        private float _targetReach;
        private float _targetRadius;

        private CargoBox _aimed;

        [Inject]
        public void Construct(Interactor interactor, Carrier carrier)
        {
            _interactor = interactor;
            _carrier = carrier;
        }

        private void Awake()
        {
            _transform = transform;
            _canvas = GetComponent<Canvas>();
            _group = GetComponent<CanvasGroup>();
            _billboard = GetComponent<CameraBillboard>();

            _rect = (RectTransform)_transform;

            Hide();

            // Карточка - инструмент разработчика: выключенная настройками, она не должна
            // ни следить за прицелом, ни тратить кадр
            if (!IsWiredUp() || !_definition.Enabled)
                enabled = false;
        }

        private void Start() => _interactor.FocusChanged += OnFocusChanged;

        private void OnDestroy()
        {
            if (_interactor != null)
                _interactor.FocusChanged -= OnFocusChanged;

            Track(null);
        }

        private void Update()
        {
            // Пока груз в руках, карточек нет: несомый ящик игрок уже осмотрел, когда брал,
            // а соседние читать некогда (Carrier о захвате не сигналит, поэтому спрашиваем
            // его сами - это чтение флага, не аллокация)
            bool handsFull = _carrier != null && _carrier.IsCarrying;

            Track(handsFull ? null : _aimed);
        }

        private void LateUpdate()
        {
            // Позицию гоним в LateUpdate: к этому моменту физика уже подвинула ящик за кадр,
            // иначе карточка отставала бы и дрожала на том, что игрок несёт
            if (_targetTransform != null)
                Follow();

            Fade(Time.deltaTime);
        }

        private void OnFocusChanged(Transform focus)
        {
            _aimed = focus != null ? focus.GetComponentInParent<CargoBox>() : null;
        }

        private void Track(CargoBox box)
        {
            if (ReferenceEquals(box, _target))
                return;

            if (_target != null)
                _target.StateChanged -= OnStateChanged;

            _target = box;
            _targetTransform = box != null ? box.transform : null;

            if (_target == null)
                return;

            _target.StateChanged += OnStateChanged;
            Measure(_target);

            Redraw();

            // Новая цель - карточку сразу ставим на место и разворачиваем к игроку,
            // иначе первый кадр проявления она проведёт над прошлым ящиком
            Follow();
            _billboard.FaceCamera();
        }

        private void OnStateChanged(CargoBox box) => Redraw();

        private void Follow()
        {
            Vector3 position = _targetTransform.position
                               + Vector3.up * (_targetReach + _definition.HeightPadding);

            Camera eye = _billboard.Eye;

            if (eye == null)
            {
                _transform.position = position;
                return;
            }

            position = PullInFront(eye, position);
            position = KeepInFrame(eye, position);

            _transform.position = position;

            Resize(eye, position);
        }

        /// <summary>
        /// Выносит карточку вперёд на радиус цели, чтобы ящик не срезал её кромку ни при каком
        /// своём повороте. Смещение идёт по лучу на камеру, поэтому место карточки в кадре
        /// не меняется - только глубина.
        /// <para>
        /// Вынос ограничен <see cref="CargoOverlayDefinition.MinDistance"/>: с ящиком в руках,
        /// когда игрок смотрит под ноги, до точки над ящиком остаётся меньше метра, и честный
        /// вынос на радиус перебрасывал карточку за камеру - там она не рисовалась вовсе.
        /// </para>
        /// </summary>
        private Vector3 PullInFront(Camera eye, Vector3 position)
        {
            Transform lens = eye.transform;
            Vector3 toEye = lens.position - position;
            float span = toEye.magnitude;
            float depth = Vector3.Dot(position - lens.position, lens.forward);

            if (span <= Mathf.Epsilon || depth <= _definition.MinDistance)
                return position;

            // Шаг d по лучу на камеру умножает глубину на (1 - d/span), отсюда и предел
            float limit = span * (1f - _definition.MinDistance / depth);
            float pull = Mathf.Min(_targetRadius, limit);

            return position + toEye / span * pull;
        }

        /// <summary>
        /// Ящик в руках стоит почти вплотную к лицу, и честный подъём над ним уводит карточку
        /// за верхний край кадра. Прижимаем её ровно настолько, чтобы верхняя кромка осталась
        /// видна: над ящиком карточка от этого быть не перестаёт
        /// </summary>
        private Vector3 KeepInFrame(Camera eye, Vector3 position)
        {
            Vector3 viewport = eye.WorldToViewportPoint(position);

            if (viewport.z <= 0f)
                return position;

            float ceiling = 1f - _definition.ScreenHeight * 0.5f - _definition.ScreenMargin;

            if (viewport.y <= ceiling)
                return position;

            viewport.y = ceiling;

            return eye.ViewportToWorldPoint(viewport);
        }

        /// <summary>
        /// Держит карточку постоянного размера на экране. В мире цель бывает и в двух шагах,
        /// и вплотную к лицу: с фиксированным масштабом карточка была бы то нечитаемой,
        /// то на весь кадр - и тогда никакой запас по высоте её в кадре не удержал бы
        /// </summary>
        private void Resize(Camera eye, Vector3 position)
        {
            Transform lens = eye.transform;
            float depth = Vector3.Dot(position - lens.position, lens.forward);

            if (depth <= 0f)
                return;

            float visibleHeight = 2f * depth * Mathf.Tan(eye.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float scale = visibleHeight * _definition.ScreenHeight / _rect.rect.height;

            _transform.localScale = new Vector3(scale, scale, scale);
        }

        private void Fade(float deltaTime)
        {
            float target = _target != null ? 1f : 0f;
            float alpha = _group.alpha;

            if (alpha == target)
                return;

            float duration = target > 0f
                ? _definition.FadeInSeconds
                : _definition.FadeOutSeconds;

            alpha = duration > 0f
                ? Mathf.MoveTowards(alpha, target, deltaTime / duration)
                : target;

            _group.alpha = alpha;
            _canvas.enabled = alpha > 0f;
        }

        private void Redraw()
        {
            CargoState state = _target.State;
            CargoIdentity identity = _target.Identity;

            _title.text = identity.TrueType != null
                ? identity.TrueType.DisplayName
                : _definition.UnknownLabel;

            _stats.text = string.Format(
                _definition.StatsFormat,
                Label(state.Paint != null ? state.Paint.DisplayName : null),
                Label(state.Stamp != null ? state.Stamp.DisplayName : null),
                Label(state.DeclaredType != null ? state.DeclaredType.DisplayName : null));

            bool diverged = !state.MatchesTruth(identity);

            _divergence.text = diverged ? _definition.DivergedLabel : _definition.MatchesLabel;
            _divergence.color = diverged ? _definition.DivergedColor : _definition.MatchesColor;
        }

        private string Label(string value) =>
            string.IsNullOrEmpty(value) ? _definition.NoneLabel : value;

        /// <summary>
        /// Замеряет цель один раз на её смене - в кадре габариты уже не считаем.
        /// <see cref="_targetReach"/> - наибольший габарит, на него поднимаем карточку:
        /// по высоте мало, потому что ящик в руках крутится и подставляет узкую сторону.
        /// <see cref="_targetRadius"/> - радиус описанной сферы: на него выносим карточку вперёд,
        /// и тогда ящик не срежет её ни при каком повороте
        /// </summary>
        private void Measure(CargoBox box)
        {
            Renderer[] renderers = box.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                _targetReach = 0f;
                _targetRadius = 0f;
                return;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 extents = bounds.extents;

            _targetReach = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
            _targetRadius = extents.magnitude;
        }

        private void Hide()
        {
            _group.alpha = 0f;
            _canvas.enabled = false;
        }

        private bool IsWiredUp()
        {
            if (_definition == null)
                return Missing("настройки карточки (CargoOverlayDefinition)");

            if (_title == null)
                return Missing("заголовок (TMP_Text)");

            if (_stats == null)
                return Missing("строки характеристик (TMP_Text)");

            if (_divergence == null)
                return Missing("строка расхождения (TMP_Text)");

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Карточке груза '{name}' не назначено: {what}. Карточки над ящиком не будет.", this);

            return false;
        }
    }
}

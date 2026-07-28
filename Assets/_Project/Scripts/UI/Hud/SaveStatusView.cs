using CrateExpectations.Core.Events;
using CrateExpectations.Persistence.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Строка "Сохранено" / "Загружено" на экране. Без неё F5 не отличить от нажатия
    /// в пустоту: игра сохраняется мгновенно и молча, а на записи демо это выглядит так,
    /// будто ничего не произошло
    /// </summary>
    public sealed class SaveStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        [Header("Показ")]
        [Tooltip("Ритм подачи. Сколько держится сообщение - оттуда")]
        [SerializeField] private HudTimingsDefinition _timings;

        [Tooltip("Удачная операция")]
        [SerializeField] private Color _successColor = new(0.55f, 0.85f, 0.95f);

        [Tooltip("Не вышло")]
        [SerializeField] private Color _failureColor = new(0.90f, 0.45f, 0.35f);

        private IEventBus _bus;
        private float _remaining;
        private float _holdSeconds;

        [Inject]
        public void Construct(IEventBus bus) => _bus = bus;

        private void Awake() => _holdSeconds = _timings != null ? _timings.SaveStatusSeconds : 3.5f;

        private void Start()
        {
            _bus.Subscribe<GameSaved>(OnSaved);
            _bus.Subscribe<GameLoaded>(OnLoaded);
            _bus.Subscribe<GameStateFailed>(OnFailed);

            _label.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (_bus == null) return;

            _bus.Unsubscribe<GameSaved>(OnSaved);
            _bus.Unsubscribe<GameLoaded>(OnLoaded);
            _bus.Unsubscribe<GameStateFailed>(OnFailed);
        }

        private void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;

            Color color = _label.color;
            color.a = Mathf.Clamp01(_remaining / _holdSeconds);
            _label.color = color;

            if (_remaining <= 0f) _label.text = string.Empty;
        }

        private void OnSaved(GameSaved saved) => Show($"{saved.SlotName}: сохранено", _successColor);

        private void OnLoaded(GameLoaded loaded) => Show($"{loaded.SlotName}: загружено", _successColor);

        private void OnFailed(GameStateFailed failed) => Show(failed.Reason, _failureColor);

        private void Show(string message, Color color)
        {
            _label.text = message;
            _label.color = color;
            _remaining = _holdSeconds;
        }
    }
}

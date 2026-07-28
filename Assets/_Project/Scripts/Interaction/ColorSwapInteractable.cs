using UnityEngine;

namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Тестовый grey-box: проверяет петлю "фокус → подсказка → взаимодействие".
    /// По <see cref="Interact"/> перебирает цвета из списка. Настоящие интерактивные объекты
    /// (ящик, станок покраски) приедут в следующих фазах
    /// </summary>
    [RequireComponent(typeof(InteractableHighlight))]
    public sealed class ColorSwapInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Текст подсказки на экране при наведении")]
        [SerializeField] private string _prompt = "[E] Перекрасить";

        [Tooltip("Цвета, которые перебираются по нажатию")]
        [SerializeField] private Color[] _colors =
        {
            new(0.75f, 0.75f, 0.75f),
            new(0.85f, 0.35f, 0.25f),
            new(0.25f, 0.55f, 0.8f),
        };

        private InteractableHighlight _highlight;
        private int _colorIndex;

        public string Prompt => _prompt;

        public bool CanInteract => _colors.Length > 0;

        private void Awake() => _highlight = GetComponent<InteractableHighlight>();

        public void OnFocused() => _highlight.SetHighlighted(true);

        public void OnUnfocused() => _highlight.SetHighlighted(false);

        public void Interact(Interactor source)
        {
            _colorIndex = (_colorIndex + 1) % _colors.Length;
            _highlight.SetBaseColor(_colors[_colorIndex]);
        }
    }
}

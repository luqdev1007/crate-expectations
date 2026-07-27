using UnityEngine;

namespace CrateExpectations.Interaction
{
    [RequireComponent(typeof(InteractableHighlight))]
    public sealed class ColorSwapInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "[E] Перекрасить";
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

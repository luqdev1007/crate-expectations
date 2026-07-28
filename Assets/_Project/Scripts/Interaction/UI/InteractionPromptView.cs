using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction.UI
{
    /// <summary>
    /// Минимальный экранный вывод подсказки взаимодействия: подписан на
    /// <see cref="Interactor.PromptChanged"/> и просто печатает текст. Полноценный UI-слой -
    /// отдельный модуль в следующих фазах
    /// </summary>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        private Interactor _interactor;

        [Inject]
        public void Construct(Interactor interactor) => _interactor = interactor;

        private void Start()
        {
            _interactor.PromptChanged += OnPromptChanged;
            OnPromptChanged(string.Empty);
        }

        private void OnDestroy()
        {
            if (_interactor != null) _interactor.PromptChanged -= OnPromptChanged;
        }

        private void OnPromptChanged(string prompt)
        {
            _label.text = prompt;
            _label.enabled = !string.IsNullOrEmpty(prompt);
        }
    }
}

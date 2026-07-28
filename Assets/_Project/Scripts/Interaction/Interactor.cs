using System;
using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction
{
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] private InteractionDefinition _definition;
        [SerializeField] private Transform _rayOrigin;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private IInputReader _input;
        private IInteractable _current;
        private string _currentPrompt = string.Empty;

        private Collider _interactableCollider;
        private IInteractable _resolvedInteractable;

        private Collider _focusCollider;
        private Transform _focus;

        public event Action<string> PromptChanged;

        /// <summary>
        /// Предмет под прицелом - не обязательно интерактивный: так о нём узнают мировые плашки,
        /// не пуская собственный луч. Пусто, когда игрок не смотрит ни на что из <see cref="InteractionDefinition.FocusMask"/>
        /// </summary>
        public event Action<Transform> FocusChanged;

        /// <inheritdoc cref="FocusChanged"/>
        public Transform Focus => _focus;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Start() => _input.Interact += OnInteractPressed;

        private void OnDestroy()
        {
            if (_input != null)
                _input.Interact -= OnInteractPressed;
        }

        private void Update()
        {
            Scan();

            if (!ReferenceEquals(_resolvedInteractable, _current))
            {
                _current?.OnUnfocused();
                _current = _resolvedInteractable;
                _current?.OnFocused();
            }

            string prompt = _current != null ? _current.Prompt : string.Empty;

            if (string.Equals(prompt, _currentPrompt))
                return;

            _currentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }

        /// <summary>
        /// Один луч на двух потребителей: ближайший интерактивный объект для подсказки и
        /// ближайший предмет под прицелом для мировых плашек. Разбираем их по маскам раздельно,
        /// поэтому ящик, попавший в кадр перед станцией, не отбирает у неё подсказку
        /// </summary>
        private void Scan()
        {
            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);

            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.ScanDistance,
                _definition.ScanMask, QueryTriggerInteraction.Ignore);

            Collider interactable = null;
            float interactableDistance = float.MaxValue;

            Collider focus = null;
            float focusDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                float distance = _hits[i].distance;

                if (distance < interactableDistance
                    && distance <= _definition.MaxDistance
                    && IsInMask(collider, _definition.InteractableMask))
                {
                    interactable = collider;
                    interactableDistance = distance;
                }

                if (distance < focusDistance
                    && distance <= _definition.FocusDistance
                    && IsInMask(collider, _definition.FocusMask))
                {
                    focus = collider;
                    focusDistance = distance;
                }
            }

            TakeInteractable(interactable);
            TakeFocus(focus);
        }

        private static bool IsInMask(Collider collider, LayerMask mask) =>
            (mask.value & (1 << collider.gameObject.layer)) != 0;

        private void TakeInteractable(Collider collider)
        {
            // Тот же коллайдер - тот же компонент: GetComponentInParent зовём только на смене цели
            if (ReferenceEquals(collider, _interactableCollider))
                return;

            _interactableCollider = collider;
            _resolvedInteractable = collider != null
                ? collider.GetComponentInParent<IInteractable>()
                : null;
        }

        private void TakeFocus(Collider collider)
        {
            if (ReferenceEquals(collider, _focusCollider))
                return;

            _focusCollider = collider;
            _focus = collider != null ? collider.transform : null;

            FocusChanged?.Invoke(_focus);
        }

        private void OnInteractPressed()
        {
            if (_current != null && _current.CanInteract)
                _current.Interact(this);
        }
    }
}

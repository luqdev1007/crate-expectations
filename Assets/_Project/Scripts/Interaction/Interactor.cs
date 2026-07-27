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

        private Collider _lastCollider;
        private IInteractable _lastResolved;

        public event Action<string> PromptChanged;

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
            IInteractable target = FindTarget();

            if (!ReferenceEquals(target, _current))
            {
                _current?.OnUnfocused();
                _current = target;
                _current?.OnFocused();
            }

            string prompt = _current != null ? _current.Prompt : string.Empty;

            if (string.Equals(prompt, _currentPrompt))
                return;

            _currentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }

        private IInteractable FindTarget()
        {
            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);
            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.MaxDistance,
                _definition.InteractableMask, QueryTriggerInteraction.Ignore);

            Collider nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance)
                    continue;

                nearest = _hits[i].collider;
                nearestDistance = _hits[i].distance;
            }

            if (ReferenceEquals(nearest, _lastCollider)) 
                return _lastResolved;

            _lastCollider = nearest;
            _lastResolved = nearest != null ? nearest.GetComponentInParent<IInteractable>() : null;

            return _lastResolved;
        }

        private void OnInteractPressed()
        {
            if (_current != null && _current.CanInteract)
                _current.Interact(this);
        }
    }
}

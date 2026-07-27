using CrateExpectations.Interaction;
using TMPro;
using UnityEngine;

namespace CrateExpectations.Cargo.UI
{
    public sealed class CargoStateOverlay : MonoBehaviour
    {
        [Tooltip("Настройки и тексты оверлея")]
        [SerializeField] private CargoOverlayDefinition _definition;

        [Tooltip("Поле, в которое пишется состояние")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("Начало луча (камера)")]
        [SerializeField] private Transform _rayOrigin;

        [Tooltip("Руки игрока")]
        [SerializeField] private Carrier _carrier;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private CargoBox _observed;

        private void Awake()
        {
            if (_definition == null || _text == null)
            {
                enabled = false;
                return;
            }

            if (!_definition.Enabled)
            {
                _text.gameObject.SetActive(false);
                enabled = false;
            }
        }

        private void OnDisable() => Observe(null);

        private void Update()
        {
            CargoBox box = FindBox();

            if (ReferenceEquals(box, _observed)) 
                return;

            Observe(box);
            Redraw();
        }

        private CargoBox FindBox()
        {
            Carriable held = _carrier != null ? _carrier.Held : null;

            if (held != null)
                return held.TryGetComponent(out CargoBox carried) ? carried : null;

            if (_rayOrigin == null) 
                return null;

            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);

            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.MaxDistance,
                _definition.CargoMask, QueryTriggerInteraction.Ignore);

            CargoBox nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance) 
                    continue;

                Collider collider = _hits[i].collider;
                CargoBox box = collider.TryGetComponent(out CargoBox own)
                    ? own
                    : collider.GetComponentInParent<CargoBox>();

                if (box == null) 
                    continue;

                nearest = box;
                nearestDistance = _hits[i].distance;
            }

            return nearest;
        }

        private void Observe(CargoBox box)
        {
            if (ReferenceEquals(_observed, box)) 
                return;

            if (_observed != null) 
                _observed.StateChanged -= OnStateChanged;

            _observed = box;

            if (_observed != null) 
                _observed.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(CargoBox box) => Redraw();

        private void Redraw()
        {
            if (_observed == null)
            {
                _text.text = string.Empty;
                return;
            }

            CargoState state = _observed.State;
            CargoIdentity identity = _observed.Identity;

            _text.text = string.Format(
                _definition.Format,
                identity.TrueType != null ? identity.TrueType.DisplayName : _definition.NoneLabel,
                state.Paint != null ? state.Paint.DisplayName : _definition.NoneLabel,
                state.Stamp != null ? state.Stamp.DisplayName : _definition.NoneLabel,
                state.DeclaredType != null ? state.DeclaredType.DisplayName : _definition.NoneLabel,
                state.MatchesTruth(identity) ? _definition.MatchesLabel : _definition.DivergedLabel);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    [RequireComponent(typeof(Collider))]
    public sealed class CargoPlacementZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Плоскость подсветки зоны, если пусто, то зона будет невидима")]
        [SerializeField] private Renderer _highlight;

        [Tooltip("Цвет пустой зоны")]
        [SerializeField] private Color _emptyColor = new(0.9f, 0.85f, 0.4f, 1f);

        [Tooltip("Цвет зоны, когда в ней стоит груз")]
        [SerializeField] private Color _occupiedColor = new(0.4f, 0.85f, 0.45f, 1f);

        private readonly Dictionary<CargoBox, int> _overlaps = new();

        private MaterialPropertyBlock _propertyBlock;
        private Collider _trigger;
        private bool _hasColorOverride;

        private CargoBox _deadOverlap;
        public CargoBox Occupant { get; private set; }

        public event Action<CargoBox> OccupantChanged;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            _propertyBlock = new MaterialPropertyBlock();

            if (!_trigger.isTrigger)
            {
                Debug.LogError(
                    $"Коллайдер зоны '{name}' не помечен как Trigger - зона не увидит груз " +
                    "и будет мешать его ставить", this);
            }

            ApplyHighlight();
        }

        private void FixedUpdate()
        {
            CargoBox next = FindOccupant();

            if (!ReferenceEquals(_deadOverlap, null))
            {
                _overlaps.Remove(_deadOverlap);
                _deadOverlap = null;
            }

            if (ReferenceEquals(next, Occupant)) 
                return;

            Occupant = next;
            ApplyHighlight();
            OccupantChanged?.Invoke(next);
        }

        private void OnTriggerEnter(Collider other)
        {
            CargoBox box = ResolveBox(other);

            if (box == null) 
                return;

            _overlaps.TryGetValue(box, out int count);
            _overlaps[box] = count + 1;
        }

        private void OnTriggerExit(Collider other)
        {
            CargoBox box = ResolveBox(other);

            if (box == null) 
                return;

            if (!_overlaps.TryGetValue(box, out int count)) 
                return;

            if (count <= 1) 
                _overlaps.Remove(box);
            else 
                _overlaps[box] = count - 1;
        }

        private static CargoBox ResolveBox(Collider other) =>
            other.TryGetComponent(out CargoBox box) ? box : other.GetComponentInParent<CargoBox>();

        private CargoBox FindOccupant()
        {
            Vector3 center = _trigger.bounds.center;
            CargoBox nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (KeyValuePair<CargoBox, int> pair in _overlaps)
            {
                CargoBox box = pair.Key;

                if (box == null)
                {
                    _deadOverlap = box;
                    continue;
                }

                if (box.IsCarried) 
                    continue;

                float distance = (box.transform.position - center).sqrMagnitude;

                if (distance >= nearestDistance) 
                    continue;

                nearest = box;
                nearestDistance = distance;
            }

            return nearest;
        }

        public void SetColorOverride(Color color)
        {
            _hasColorOverride = true;
            SetHighlightColor(color);
        }

        public void ClearColorOverride()
        {
            _hasColorOverride = false;
            ApplyHighlight();
        }

        private void ApplyHighlight()
        {
            if (_hasColorOverride) 
                return;

            SetHighlightColor(Occupant != null ? _occupiedColor : _emptyColor);
        }

        private void SetHighlightColor(Color color)
        {
            if (_highlight == null) 
                return;

            _highlight.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _highlight.SetPropertyBlock(_propertyBlock);
        }
    }
}

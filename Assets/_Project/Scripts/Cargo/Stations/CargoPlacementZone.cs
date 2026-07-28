using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    /// <summary>
    /// Зона размещения груза: триггер над столом, который знает, какой ящик на нём стоит.
    /// Вынесена отдельным компонентом - "следить за зоной" и "что-то с грузом делать"
    /// это разные обязанности. Ровно поэтому её же, без единой правки, использует и стол
    /// досмотра в порту: станции важно, что на ней можно применить рецепт, инспектору -
    /// что появился груз; сама зона не знает ни про то, ни про другое
    /// </summary>
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

        // Счётчик перекрытий на ящик: один груз может задевать зону несколькими коллайдерами,
        // и выход одного из них ещё не значит, что ящик со стола убрали
        private readonly Dictionary<CargoBox, int> _overlaps = new();

        private MaterialPropertyBlock _propertyBlock;
        private Collider _trigger;
        private bool _hasColorOverride;

        // Ключ уничтоженного ящика, замеченный при последнем переборе: удалить его прямо
        // в цикле нельзя, поэтому убираем на следующем шаге
        private CargoBox _deadOverlap;
        /// <summary>Ящик, который сейчас стоит в зоне, или <c>null</c></summary>
        public CargoBox Occupant { get; private set; }

        /// <summary>Содержимое зоны сменилось (в том числе на <c>null</c>)</summary>
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

        /// <summary>
        /// Занятость пересчитывается каждый физический шаг, а не по триггерам: груз может
        /// висеть в зоне в руках игрока, а "поставленным" он становится в момент отпускания -
        /// события коллизий об этом не сообщают
        /// </summary>
        private void FixedUpdate()
        {
            CargoBox next = FindOccupant();

            // Уничтоженный груз не шлёт OnTriggerExit - ключ на него остался бы в словаре
            // навсегда. Чистим по одному за шаг: перебор идёт по тому же словарю.
            // Сравнение только через ReferenceEquals: для уничтоженного объекта
            // перегруженный `!=` врёт, а ключом в словаре он остаётся настоящим
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

        /// <summary>Ближайший к центру зоны поставленный ящик. Груз в руках не считается</summary>
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

        /// <summary>Временно перекрасить зону - визуальный отклик станции на срабатывание</summary>
        public void SetColorOverride(Color color)
        {
            _hasColorOverride = true;
            SetHighlightColor(color);
        }

        /// <summary>Вернуть зоне обычный цвет</summary>
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

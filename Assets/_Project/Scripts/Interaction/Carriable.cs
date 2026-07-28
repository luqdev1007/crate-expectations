using UnityEngine;

namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Маркер: этот Rigidbody-объект можно поднимать и переносить (на время переноса уводит
    /// всю свою иерархию на отдельный слой и возвращает исходные, поэтому груз перестаёт
    /// сталкиваться с игроком независимо от того, сколько у него коллайдеров)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Carriable : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform[] _hierarchy;
        private int[] _originalLayers;

        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// Объект прямо сейчас в руках игрока: по слою этого не понять - слой на время переноса
        /// подменён, а следящим за объектом (например, зоне размещения) нужен именно факт «в руках»
        /// </summary>
        public bool IsCarried { get; private set; }

        private void Awake() => _rigidbody = GetComponent<Rigidbody>();

        /// <summary>Пометить объект как поднятый или отпущенный (зовёт только <see cref="Carrier"/>)</summary>
        public void MarkCarried(bool carried) => IsCarried = carried;

        /// <summary>
        /// Перевести объект и всех его детей на слой переноса, запомнив исходные слои: иерархия
        /// снимается в момент захвата, а не кэшируется, потому что груз обрастает детьми
        /// по ходу игры - печатями, крышками, наклейками
        /// </summary>
        public void OverrideLayers(int layer)
        {
            _hierarchy = GetComponentsInChildren<Transform>(true);

            if (_originalLayers == null || _originalLayers.Length < _hierarchy.Length)
                _originalLayers = new int[_hierarchy.Length];

            for (int i = 0; i < _hierarchy.Length; i++)
            {
                _originalLayers[i] = _hierarchy[i].gameObject.layer;
                _hierarchy[i].gameObject.layer = layer;
            }
        }

        /// <summary>Вернуть слои, снятые в <see cref="OverrideLayers"/></summary>
        public void RestoreLayers()
        {
            if (_hierarchy == null) 
                return;

            for (int i = 0; i < _hierarchy.Length; i++)
                if (_hierarchy[i] != null)
                    _hierarchy[i].gameObject.layer = _originalLayers[i];

            _hierarchy = null;
        }
    }
}

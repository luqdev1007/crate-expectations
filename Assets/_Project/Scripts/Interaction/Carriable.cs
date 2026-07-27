using UnityEngine;

namespace CrateExpectations.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Carriable : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private Transform[] _hierarchy;
        private int[] _originalLayers;

        public Rigidbody Rigidbody => _rigidbody;

        public bool IsCarried { get; private set; }

        private void Awake() => _rigidbody = GetComponent<Rigidbody>();

        public void MarkCarried(bool carried) => IsCarried = carried;

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

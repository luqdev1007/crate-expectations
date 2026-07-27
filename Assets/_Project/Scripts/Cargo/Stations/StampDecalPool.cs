using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    public sealed class StampDecalPool : MonoBehaviour
    {
        [SerializeField] private StampDecal _decalPrefab;
        [SerializeField] private int _prewarmCount = 4;

        private readonly Stack<StampDecal> _free = new();
        private readonly Dictionary<CargoBox, StampDecal> _attached = new();

        private void Awake()
        {
            if (_decalPrefab == null)
            {
                Debug.LogError($"Пулу декалей '{name}' не назначен префаб, печати не появятся", this);
                return;
            }

            for (int i = 0; i < _prewarmCount; i++) 
                _free.Push(Create());
        }

        public void Sync(CargoBox box, StampDefinition stamp)
        {
            if (box == null)
                return;

            _attached.TryGetValue(box, out StampDecal decal);

            if (stamp == null)
            {
                if (decal != null) 
                    ReturnToPool(box, decal);

                return;
            }

            if (decal == null)
            {
                decal = _free.Count > 0 ? _free.Pop() : Create();
                _attached[box] = decal;
            }

            Transform anchor = box.StampAnchor;
            decal.transform.SetParent(anchor, false);
            decal.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            decal.gameObject.layer = anchor.gameObject.layer;
            decal.gameObject.SetActive(true);
            decal.Show(stamp);
        }

        public void Release(CargoBox box)
        {
            if (box == null) 
                return;

            if (_attached.TryGetValue(box, out StampDecal decal)) 
                ReturnToPool(box, decal);
        }

        private void ReturnToPool(CargoBox box, StampDecal decal)
        {
            _attached.Remove(box);

            if (decal == null) 
                return;

            decal.gameObject.SetActive(false);
            decal.transform.SetParent(transform, false);
            _free.Push(decal);
        }

        private StampDecal Create()
        {
            StampDecal decal = Instantiate(_decalPrefab, transform);
            decal.gameObject.SetActive(false);

            return decal;
        }
    }
}

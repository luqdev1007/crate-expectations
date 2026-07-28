using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    /// <summary>
    /// Пул декалей-пломб. Декали - общий ресурс сцены, а ящик - префаб, который про сцену
    /// ничего не знает: поэтому владеет ими пул, а не <see cref="CargoBox"/>. Instantiate
    /// случается только при разогреве (и при исчерпании пула), применение печати -
    /// это переиспользование объекта, без создания и уничтожения
    /// </summary>
    public sealed class StampDecalPool : MonoBehaviour
    {
        [SerializeField] private StampDecal _decalPrefab;
        [Tooltip("Сколько декалей создать заранее")]
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

        /// <summary>
        /// Привести декаль ящика в соответствие его состоянию: поставить, заменить или снять.
        /// Идемпотентно - вызывать можно после любого действия маскировки
        /// </summary>
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

        /// <summary>Снять печать с ящика и вернуть декаль в пул (например, перед выгрузкой груза)</summary>
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

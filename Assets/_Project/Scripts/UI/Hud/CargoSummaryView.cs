using CrateExpectations.Inventory;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    public sealed class CargoSummaryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _summary;

        private ICargoInventory _inventory;

        [Inject]
        public void Construct(ICargoInventory inventory) => _inventory = inventory;

        private void Start()
        {
            _inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null) 
                _inventory.Changed -= Refresh;
        }

        private void Refresh() =>
            _summary.SetText(
                "Груз - на доке: {0}   сдано: {1}   изъято: {2}",
                _inventory.OnDockCount, _inventory.DeliveredCount, _inventory.SeizedCount);
    }
}

using System.Collections.Generic;
using CrateExpectations.Contracts;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Core.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    public sealed class ContractBoard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _header;
        [SerializeField] private ContractSlot[] _slots;

        private IContractManager _manager;
        private IEventBus _bus;

        [Inject]
        public void Construct(IContractManager manager, IEventBus bus)
        {
            _manager = manager;
            _bus = bus;
        }

        private void Start()
        {
            _bus.Subscribe<ContractAccepted>(OnContractAccepted);
            _bus.Subscribe<ContractProgressed>(OnContractProgressed);
            _bus.Subscribe<ContractCompleted>(OnContractCompleted);
            _bus.Subscribe<ContractFailed>(OnContractFailed);

            Refresh();
        }

        private void OnDestroy()
        {
            if (_bus == null) 
                return;

            _bus.Unsubscribe<ContractAccepted>(OnContractAccepted);
            _bus.Unsubscribe<ContractProgressed>(OnContractProgressed);
            _bus.Unsubscribe<ContractCompleted>(OnContractCompleted);
            _bus.Unsubscribe<ContractFailed>(OnContractFailed);
        }

        private void OnContractAccepted(ContractAccepted _) => Refresh();

        private void OnContractProgressed(ContractProgressed _) => Refresh();

        private void OnContractCompleted(ContractCompleted _) => Refresh();

        private void OnContractFailed(ContractFailed _) => Refresh();

        private void Refresh()
        {
            IReadOnlyList<ContractDefinition> available = _manager.Available;
            ContractDefinition active = _manager.Active.Contract;

            for (int i = 0; i < _slots.Length; i++)
            {
                ContractDefinition contract = i < available.Count ? available[i] : null;
                _slots[i].Bind(contract, _manager, isActive: contract != null && contract == active);
            }

            if (_header != null)
            {
                _header.text = active != null
                    ? $"ЗАКАЗЫ ПОРТА - выполняется '{active.DisplayName}'"
                    : "ЗАКАЗЫ ПОРТА - выберите заказ";
            }
        }
    }
}

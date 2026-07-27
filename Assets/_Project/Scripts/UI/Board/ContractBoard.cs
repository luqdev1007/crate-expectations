using System.Collections.Generic;
using CrateExpectations.Contracts;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Core.Events;
using CrateExpectations.Persistence.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Доска заказов. Показывает только те листки, которые ещё никто не снимал:
    /// взятый заказ уходит с доски навсегда - и после провала, и после загрузки сейва.
    /// </summary>
    public sealed class ContractBoard : MonoBehaviour
    {
        [Tooltip("Формулировки шапки и подсказок")]
        [SerializeField] private ContractBoardDefinition _definition;

        [SerializeField] private TMP_Text _header;
        [SerializeField] private ContractSlot[] _slots;

        private IContractManager _manager;
        private IEventBus _bus;

        /// <summary>
        /// Какой заказ закреплён за каким гвоздём. Раскладку считаем один раз, чтобы снятый
        /// листок оставлял после себя пустое место, а соседние не перепрыгивали.
        /// </summary>
        private ContractDefinition[] _pinned;

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
            _bus.Subscribe<GameLoaded>(OnGameLoaded);

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
            _bus.Unsubscribe<GameLoaded>(OnGameLoaded);
        }

        private void OnContractAccepted(ContractAccepted _) => Refresh();

        private void OnContractProgressed(ContractProgressed _) => Refresh();

        private void OnContractCompleted(ContractCompleted _) => Refresh();

        private void OnContractFailed(ContractFailed _) => Refresh();

        private void OnGameLoaded(GameLoaded _) => Refresh();

        private void Refresh()
        {
            if (_pinned == null)
                Pin();

            int left = 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                    continue;

                ContractDefinition pinned = _pinned[i];
                bool hangs = pinned != null && !_manager.IsTaken(pinned);

                if (hangs)
                    left++;

                _slots[i].Bind(hangs ? pinned : null, _manager);
            }

            if (_header == null || _definition == null)
                return;

            _header.SetText(Headline(_manager.Active.Contract, left));
        }

        private void Pin()
        {
            _pinned = new ContractDefinition[_slots.Length];

            IReadOnlyList<ContractDefinition> onBoard = _manager.Available;

            for (int i = 0; i < _pinned.Length && i < onBoard.Count; i++)
                _pinned[i] = onBoard[i];
        }

        private string Headline(ContractDefinition active, int papersLeft)
        {
            if (active != null)
                return string.Format(_definition.HeaderActive, active.DisplayName);

            return papersLeft > 0 ? _definition.HeaderIdle : _definition.HeaderEmpty;
        }
    }
}

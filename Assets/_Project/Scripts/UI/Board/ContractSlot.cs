using CrateExpectations.Contracts;
using CrateExpectations.Interaction;
using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Гвоздь на доске: держит листок и отдаёт его игроку. Сам ничего не рисует -
    /// содержимое заказа целиком на <see cref="ContractPaperView"/>
    /// </summary>
    public sealed class ContractSlot : MonoBehaviour, IInteractable
    {
        [SerializeField] private ContractBoardDefinition _definition;

        [Tooltip("Листок, висящий на этом гвозде")]
        [SerializeField] private ContractPaperView _paper;

        private IContractManager _manager;
        private ContractDefinition _contract;
        private string _prompt = string.Empty;

        /// <inheritdoc />
        public string Prompt => _prompt;

        /// <inheritdoc />
        public bool CanInteract => _manager != null && _manager.CanAccept(_contract);

        private void Awake()
        {
            if (_definition != null && _paper != null)
                return;

            Debug.LogError($"Гвоздю '{name}' не назначен листок или формулировки.", this);
            enabled = false;
        }

        /// <summary>Вешает заказ на гвоздь. <c>null</c> - гвоздь пуст, листка на нём нет</summary>
        public void Bind(ContractDefinition contract, IContractManager manager)
        {
            _contract = contract;
            _manager = manager;

            gameObject.SetActive(contract != null);

            if (contract == null)
            {
                _prompt = string.Empty;
                return;
            }

            _paper.Bind(contract);

            _prompt = CanInteract
                ? string.Format(_definition.TakePromptFormat, contract.DisplayName)
                : _definition.BlockedPrompt;
        }

        /// <inheritdoc />
        public void OnFocused() { }

        /// <inheritdoc />
        public void OnUnfocused() { }

        /// <inheritdoc />
        public void Interact(Interactor source)
        {
            if (_contract != null)
                _manager.Accept(_contract);
        }
    }
}

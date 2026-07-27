using System.Text;
using CrateExpectations.Contracts;
using CrateExpectations.Interaction;
using TMPro;
using UnityEngine;

namespace CrateExpectations.UI
{
    public sealed class ContractSlot : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private TMP_Text _label;
        [SerializeField] private Renderer _plate;

        [Header("Цвета состояний")]
        [SerializeField] private Color _availableColor = new(0.16f, 0.18f, 0.22f);
        [SerializeField] private Color _activeColor = new(0.20f, 0.42f, 0.24f);
        [SerializeField] private Color _blockedColor = new(0.22f, 0.16f, 0.16f);

        private readonly StringBuilder _builder = new(192);

        private MaterialPropertyBlock _propertyBlock;
        private IContractManager _manager;
        private ContractDefinition _contract;
        private string _prompt = string.Empty;

        public string Prompt => _prompt;

        public bool CanInteract => _manager != null && _manager.CanAccept(_contract);

        private void Awake() => _propertyBlock = new MaterialPropertyBlock();

        public void Bind(ContractDefinition contract, IContractManager manager, bool isActive)
        {
            _contract = contract;
            _manager = manager;

            gameObject.SetActive(contract != null);

            if (contract == null) 
                return;

            _label.SetText(Describe(contract, isActive));

            _prompt = isActive
                ? string.Empty
                : CanInteract ? $"Взять заказ: {contract.DisplayName}" : "Сначала закройте текущий заказ";

            SetPlateColor(isActive ? _activeColor : CanInteract ? _availableColor : _blockedColor);
        }

        public void OnFocused() { }

        public void OnUnfocused() { }

        public void Interact(Interactor source)
        {
            if (_contract != null) 
                _manager.Accept(_contract);
        }

        private string Describe(ContractDefinition contract, bool isActive)
        {
            _builder.Clear();
            _builder.Append(isActive ? "► " : string.Empty).AppendLine(contract.DisplayName);

            if (!string.IsNullOrEmpty(contract.Description))
                _builder.AppendLine(contract.Description);

            _builder.Append("Ящиков: ").Append(contract.Crates);

            if (contract.DeclaredAs != null)
                _builder.Append("   выдать за: ").Append(contract.DeclaredAs.DisplayName);

            _builder.AppendLine();
            _builder.Append("Награда: ").Append(contract.RewardPerCrate).Append(" за ящик");

            if (contract.CleanBonus > 0)
                _builder.Append(" (+").Append(contract.CleanBonus).Append(" без придирок)");

            _builder.AppendLine();
            _builder.Append("Штраф: ").Append(contract.Penalty).Append(" за изъятый");

            if (contract.AllowedSeizures > 0)
                _builder.Append(", терпят до ").Append(contract.AllowedSeizures);

            return _builder.ToString();
        }

        private void SetPlateColor(Color color)
        {
            if (_plate == null) 
                return;

            _plate.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _plate.SetPropertyBlock(_propertyBlock);
        }
    }
}

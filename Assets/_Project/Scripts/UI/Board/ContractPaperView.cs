using CrateExpectations.Cargo;
using CrateExpectations.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Листок заказа: единственная вьюха контракта в игре. Один и тот же префаб висит на доске
    /// и поднимается к камере по Q, поэтому вся вёрстка живёт в префабе, а здесь только подстановка.
    /// </summary>
    public sealed class ContractPaperView : MonoBehaviour
    {
        [Tooltip("Шаблоны строк листка")]
        [SerializeField] private ContractPaperDefinition _definition;

        [Tooltip("Картинка товара")]
        [SerializeField] private Image _icon;

        [Tooltip("Название заказа")]
        [SerializeField] private TMP_Text _title;

        [Tooltip("Краткое описание от заказчика")]
        [SerializeField] private TMP_Text _description;

        [Tooltip("Сколько ящиков нужно сдать")]
        [SerializeField] private TMP_Text _crates;

        [Tooltip("Сколько платят за ящик")]
        [SerializeField] private TMP_Text _reward;

        [Tooltip("Под каким видом сдавать груз")]
        [SerializeField] private TMP_Text _declaredAs;

        /// <summary>Заказ, который сейчас нарисован на листке.</summary>
        public ContractDefinition Contract { get; private set; }

        private void Awake() => IsWiredUp();

        /// <summary>Перерисовывает листок. <c>null</c> очищает его.</summary>
        public void Bind(ContractDefinition contract)
        {
            Contract = contract;

            if (!IsWiredUp())
                return;

            if (contract == null)
            {
                Clear();
                return;
            }

            _title.SetText(contract.DisplayName);
            _description.SetText(contract.Description);

            _crates.SetText(_definition.CratesFormat, contract.Crates);
            _reward.SetText(_definition.RewardFormat, contract.RewardPerCrate);

            CargoTypeDefinition declared = contract.DeclaredAs;

            // Подстановка строки: у TMP.SetText аргументы только числовые
            _declaredAs.SetText(string.Format(
                _definition.DeclaredAsFormat,
                declared != null ? declared.DisplayName : _definition.DeclaredAsNothing));

            _icon.sprite = contract.Icon;
            _icon.enabled = contract.Icon != null;
        }

        private void Clear()
        {
            _title.SetText(string.Empty);
            _description.SetText(string.Empty);
            _crates.SetText(string.Empty);
            _reward.SetText(string.Empty);
            _declaredAs.SetText(string.Empty);

            _icon.sprite = null;
            _icon.enabled = false;
        }

        private bool IsWiredUp()
        {
            if (_definition != null && _icon != null && _title != null && _description != null &&
                _crates != null && _reward != null && _declaredAs != null)
            {
                return true;
            }

            Debug.LogError($"Листку заказа '{name}' не назначены поля - заполнять нечего.", this);

            return false;
        }
    }
}

using CrateExpectations.Cargo;
using CrateExpectations.Economy;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Заказ: что везём на самом деле, под что это положено выдавать, сколько ящиков и почём
    /// (всё, чем один контракт отличается от другого, заводится ассетом, а не строчкой кода)
    /// </summary>
    [CreateAssetMenu(
        fileName = "Contract",
        menuName = "CrateExpectations/Contracts/Contract")]
    public sealed class ContractDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Заказ";

        [Tooltip("Текст на доске: что от игрока хотят")]
        [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; } = string.Empty;

        [Tooltip("Картинка товара на листке заказа")]
        [field: SerializeField] public Sprite Icon { get; private set; }

        [Header("Груз")]
        [Tooltip("Что везём на самом деле. Именно по этому типу сдача засчитывается в контракт")]
        [field: SerializeField] public CargoTypeDefinition Cargo { get; private set; }

        [Tooltip("Под что это положено выдавать. Подсказка игроку. Вердикт считает регламент порта")]
        [field: SerializeField] public CargoTypeDefinition DeclaredAs { get; private set; }

        [Tooltip("Сколько ящиков нужно сдать, чтобы заказ был выполнен")]
        [Min(1)]
        [field: SerializeField] public int Crates { get; private set; } = 1;

        [Tooltip("Сколько задержанных ящиков заказчик стерпит, если больше заказ будет провален")]
        [Min(0)]
        [field: SerializeField] public int AllowedSeizures { get; private set; }

        [Header("Деньги")]
        [Tooltip("Плата за принятый ящик")]
        [field: SerializeField] public int RewardPerCrate { get; private set; } = 200;

        [Tooltip("Надбавка за ящик, к которому инспектор не придрался вовсе")]
        [field: SerializeField] public int CleanBonus { get; private set; } = 50;

        [Tooltip("Штраф за задержанный ящик, положительным числом: знак расставит расчёт")]
        [Min(0)]
        [field: SerializeField] public int Penalty { get; private set; } = 150;

        /// <summary>Денежные условия в виде значения - то, с чем работает <see cref="PayoutCalculator"/></summary>
        public PayoutTerms Terms => new(RewardPerCrate, Penalty, CleanBonus);

        /// <summary>Заказ заполнен настолько, что его можно предлагать игроку</summary>
        public bool IsPlayable => Cargo != null && Crates > 0;
    }
}

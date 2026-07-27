using CrateExpectations.Cargo;
using CrateExpectations.Economy;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    [CreateAssetMenu(
        fileName = "Contract",
        menuName = "CrateExpectations/Contracts/Contract")]
    public sealed class ContractDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Заказ";
        [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; } = string.Empty;

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
        [field: SerializeField] public int RewardPerCrate { get; private set; } = 200;

        [Tooltip("Надбавка за ящик, к которому инспектор не придрался вовсе")]
        [field: SerializeField] public int CleanBonus { get; private set; } = 50;

        [Tooltip("Штраф за задержанный ящик")]
        [Min(0)]
        [field: SerializeField] public int Penalty { get; private set; } = 150;

        public PayoutTerms Terms => new(RewardPerCrate, Penalty, CleanBonus);

        public bool IsPlayable => Cargo != null && Crates > 0;
    }
}

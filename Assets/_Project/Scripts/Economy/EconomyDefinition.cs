using UnityEngine;

namespace CrateExpectations.Economy
{
    public readonly struct EconomyRules
    {
        public EconomyRules(int startingBalance, int debtLimit)
        {
            StartingBalance = startingBalance;
            DebtLimit = debtLimit;
        }

        public int StartingBalance { get; }

        public int DebtLimit { get; }
    }

    [CreateAssetMenu(
        fileName = "EconomyDefinition",
        menuName = "CrateExpectations/Economy/Economy Definition")]
    public sealed class EconomyDefinition : ScriptableObject
    {
        [field: SerializeField] public int StartingBalance { get; private set; } = 500;    
        [field: SerializeField][Min(0)] public int DebtLimit { get; private set; } = 400;
        public EconomyRules Rules => new(StartingBalance, DebtLimit);
    }
}

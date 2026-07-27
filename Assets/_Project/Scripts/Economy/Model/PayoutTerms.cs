namespace CrateExpectations.Economy
{
    public readonly struct PayoutTerms
    {
        public PayoutTerms(int rewardPerCrate, int penaltyPerSeizure, int cleanBonus = 0)
        {
            RewardPerCrate = rewardPerCrate;
            PenaltyPerSeizure = penaltyPerSeizure;
            CleanBonus = cleanBonus;
        }

        public int RewardPerCrate { get; }

        public int PenaltyPerSeizure { get; }

        public int CleanBonus { get; }
    }
}

namespace CrateExpectations.Economy
{
    public interface IEconomyService
    {
        int Balance { get; }

        bool IsBankrupt { get; }

        void Apply(in PayoutResult payout);

        EconomySnapshot Capture();

        void Restore(in EconomySnapshot snapshot);
    }
}

namespace CrateExpectations.Economy.Events
{
    public readonly struct BalanceChanged
    {
        public BalanceChanged(int balance, in PayoutResult payout)
        {
            Balance = balance;
            Payout = payout;
        }

        public int Balance { get; }

        public PayoutResult Payout { get; }

        public int Delta => Payout.Amount;

        public bool InDebt => Balance < 0;
    }
}

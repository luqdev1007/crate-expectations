namespace CrateExpectations.Economy.Events
{
    public readonly struct WentBankrupt
    {
        public WentBankrupt(int balance, int debtLimit)
        {
            Balance = balance;
            DebtLimit = debtLimit;
        }

        public int Balance { get; }

        public int DebtLimit { get; }
    }
}

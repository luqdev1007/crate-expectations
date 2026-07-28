namespace CrateExpectations.Economy.Events
{
    /// <summary>
    /// Баланс изменился. Несёт не только новое значение, но и разбивку - за что именно:
    /// HUD показывает "+300 доставка", не запрашивая ни у кого подробности задним числом
    /// </summary>
    public readonly struct BalanceChanged
    {
        public BalanceChanged(int balance, in PayoutResult payout)
        {
            Balance = balance;
            Payout = payout;
        }

        /// <summary>Баланс после изменения</summary>
        public int Balance { get; }

        /// <summary>Что и за что начислили или сняли</summary>
        public PayoutResult Payout { get; }

        /// <summary>Насколько изменился баланс</summary>
        public int Delta => Payout.Amount;

        /// <summary>Игрок ушёл в минус</summary>
        public bool InDebt => Balance < 0;
    }
}

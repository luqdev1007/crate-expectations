using CrateExpectations.Core.Events;
using CrateExpectations.Economy.Events;

namespace CrateExpectations.Economy
{
    public sealed class EconomyService : IEconomyService
    {
        private readonly EconomyRules _rules;
        private readonly IEventBus _bus;

        public EconomyService(EconomyRules rules, IEventBus bus)
        {
            _rules = rules;
            _bus = bus;
            Balance = rules.StartingBalance;
        }

        public int Balance { get; private set; }

        public bool IsBankrupt => Balance < -_rules.DebtLimit;

        public void Apply(in PayoutResult payout)
        {
            if (payout.Amount == 0) 
                return;

            bool wasBankrupt = IsBankrupt;
            Balance += payout.Amount;

            _bus.Publish(new BalanceChanged(Balance, payout));

            if (!wasBankrupt && IsBankrupt)
                _bus.Publish(new WentBankrupt(Balance, _rules.DebtLimit));
        }

        public EconomySnapshot Capture() => new() { Balance = Balance };

        public void Restore(in EconomySnapshot snapshot) => Balance = snapshot.Balance;
    }
}

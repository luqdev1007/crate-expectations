using System.Collections.Generic;
using CrateExpectations.Core.Events;
using CrateExpectations.Economy.Events;
using NUnit.Framework;

namespace CrateExpectations.Economy.Tests
{
    public sealed class EconomyServiceTests
    {
        private EventBus _bus;
        private List<BalanceChanged> _balanceEvents;
        private List<WentBankrupt> _bankruptEvents;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _balanceEvents = new List<BalanceChanged>();
            _bankruptEvents = new List<WentBankrupt>();

            _bus.Subscribe<BalanceChanged>(e => _balanceEvents.Add(e));
            _bus.Subscribe<WentBankrupt>(e => _bankruptEvents.Add(e));
        }

        private EconomyService Service(int startingBalance = 500, int debtLimit = 400) =>
            new(new EconomyRules(startingBalance, debtLimit), _bus);

        private static PayoutResult Payout(int amount, PayoutReason reason) =>
            new(amount, new[] { new PayoutLine(reason, amount) });

        [Test]
        public void Starts_with_the_balance_from_the_rules()
        {
            Assert.That(Service(startingBalance: 750).Balance, Is.EqualTo(750));
        }

        [Test]
        public void A_reward_raises_the_balance()
        {
            EconomyService economy = Service();

            economy.Apply(Payout(300, PayoutReason.Delivery));

            Assert.That(economy.Balance, Is.EqualTo(800));
        }

        [Test]
        public void A_penalty_lowers_the_balance()
        {
            EconomyService economy = Service();

            economy.Apply(Payout(-220, PayoutReason.Seizure));

            Assert.That(economy.Balance, Is.EqualTo(280));
        }

        [Test]
        public void Payouts_accumulate_in_order()
        {
            EconomyService economy = Service();

            economy.Apply(Payout(-220, PayoutReason.Seizure));
            economy.Apply(Payout(400, PayoutReason.Delivery));

            Assert.That(economy.Balance, Is.EqualTo(680));
        }

        [Test]
        public void The_balance_is_allowed_to_go_below_zero_instead_of_being_clamped()
        {
            EconomyService economy = Service(startingBalance: 100, debtLimit: 400);

            economy.Apply(Payout(-250, PayoutReason.Seizure));

            Assert.That(economy.Balance, Is.EqualTo(-150));
            Assert.That(economy.IsBankrupt, Is.False, "долг в пределах лимита - это ещё не банкротство");
        }

        [Test]
        public void A_change_is_announced_with_the_new_balance_and_its_breakdown()
        {
            EconomyService economy = Service();

            economy.Apply(Payout(300, PayoutReason.Delivery));

            Assert.That(_balanceEvents.Count, Is.EqualTo(1));
            Assert.That(_balanceEvents[0].Balance, Is.EqualTo(800));
            Assert.That(_balanceEvents[0].Delta, Is.EqualTo(300));
            Assert.That(_balanceEvents[0].Payout.Has(PayoutReason.Delivery), Is.True);
        }

        [Test]
        public void A_zero_payout_changes_nothing_and_says_nothing()
        {
            EconomyService economy = Service();

            economy.Apply(default);

            Assert.That(economy.Balance, Is.EqualTo(500));
            Assert.That(_balanceEvents, Is.Empty);
        }

        [Test]
        public void Crossing_the_debt_limit_declares_bankruptcy()
        {
            EconomyService economy = Service(startingBalance: 0, debtLimit: 400);

            economy.Apply(Payout(-500, PayoutReason.Seizure));

            Assert.That(economy.IsBankrupt, Is.True);
            Assert.That(_bankruptEvents.Count, Is.EqualTo(1));
            Assert.That(_bankruptEvents[0].Balance, Is.EqualTo(-500));
            Assert.That(_bankruptEvents[0].DebtLimit, Is.EqualTo(400));
        }

        [Test]
        public void Bankruptcy_is_declared_once_not_on_every_later_penalty()
        {
            EconomyService economy = Service(startingBalance: 0, debtLimit: 400);

            economy.Apply(Payout(-500, PayoutReason.Seizure));
            economy.Apply(Payout(-100, PayoutReason.Seizure));

            Assert.That(_bankruptEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void Climbing_back_out_of_the_debt_arms_the_bankruptcy_again()
        {
            EconomyService economy = Service(startingBalance: 0, debtLimit: 400);

            economy.Apply(Payout(-500, PayoutReason.Seizure));
            economy.Apply(Payout(600, PayoutReason.Delivery));
            economy.Apply(Payout(-700, PayoutReason.Seizure));

            Assert.That(economy.IsBankrupt, Is.True);
            Assert.That(_bankruptEvents.Count, Is.EqualTo(2));
        }
    }
}

using NUnit.Framework;

namespace CrateExpectations.Economy.Tests
{
    public sealed class PayoutCalculatorTests
    {
        private PayoutCalculator _calculator;

        [SetUp]
        public void SetUp() => _calculator = new PayoutCalculator();

        [Test]
        public void Cleared_delivery_pays_the_contract_reward()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150);

            PayoutResult result = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Cleared));

            Assert.That(result.Amount, Is.EqualTo(200));
            Assert.That(result.IsPenalty, Is.False);
            Assert.That(result.Has(PayoutReason.Delivery), Is.True);
        }

        [Test]
        public void Spotless_delivery_adds_the_clean_bonus_on_top()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150, cleanBonus: 50);

            PayoutResult result = _calculator.Calculate(
                terms, new DeliveryReport(DeliveryOutcome.Cleared, spotless: true));

            Assert.That(result.Amount, Is.EqualTo(250));
            Assert.That(result.Has(PayoutReason.CleanBonus), Is.True);
        }

        [Test]
        public void Clean_bonus_is_not_paid_when_the_inspector_found_something()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150, cleanBonus: 50);

            PayoutResult result = _calculator.Calculate(
                terms, new DeliveryReport(DeliveryOutcome.Cleared, spotless: false));

            Assert.That(result.Amount, Is.EqualTo(200));
            Assert.That(result.Has(PayoutReason.CleanBonus), Is.False);
        }

        [Test]
        public void Seized_cargo_costs_the_penalty_and_pays_nothing()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150, cleanBonus: 50);

            PayoutResult result = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Seized));

            Assert.That(result.Amount, Is.EqualTo(-150));
            Assert.That(result.IsPenalty, Is.True);
            Assert.That(result.Has(PayoutReason.Seizure), Is.True);
            Assert.That(result.Has(PayoutReason.Delivery), Is.False);
        }

        [Test]
        public void Penalty_is_subtracted_even_if_the_asset_already_carries_a_minus()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: -150);

            PayoutResult result = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Seized));

            Assert.That(result.Amount, Is.EqualTo(-150));
        }

        [Test]
        public void Different_contracts_pay_different_money_for_the_same_delivery()
        {
            var cheap = new PayoutTerms(rewardPerCrate: 160, penaltyPerSeizure: 80);
            var rich = new PayoutTerms(rewardPerCrate: 500, penaltyPerSeizure: 400);
            var delivery = new DeliveryReport(DeliveryOutcome.Cleared);

            Assert.That(_calculator.Calculate(cheap, delivery).Amount, Is.EqualTo(160));
            Assert.That(_calculator.Calculate(rich, delivery).Amount, Is.EqualTo(500));
        }

        [Test]
        public void Zero_lines_are_not_written_into_the_breakdown()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150, cleanBonus: 0);

            PayoutResult result = _calculator.Calculate(
                terms, new DeliveryReport(DeliveryOutcome.Cleared, spotless: true));

            Assert.That(result.Lines.Count, Is.EqualTo(1));
            Assert.That(result.Has(PayoutReason.CleanBonus), Is.False);
        }

        [Test]
        public void An_earlier_result_survives_the_next_calculation()
        {
            var terms = new PayoutTerms(rewardPerCrate: 200, penaltyPerSeizure: 150);

            PayoutResult first = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Cleared));
            _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Seized));

            Assert.That(first.Amount, Is.EqualTo(200));
            Assert.That(first.Lines.Count, Is.EqualTo(1));
            Assert.That(first.Has(PayoutReason.Delivery), Is.True);
            Assert.That(first.Has(PayoutReason.Seizure), Is.False);
        }

        [Test]
        public void A_default_result_has_an_empty_breakdown_rather_than_null()
        {
            var empty = default(PayoutResult);

            Assert.That(empty.Lines, Is.Not.Null);
            Assert.That(empty.Lines.Count, Is.Zero);
        }
    }
}

using System;
using System.Collections.Generic;

namespace CrateExpectations.Economy
{
    public sealed class PayoutCalculator
    {
        private readonly List<PayoutLine> _lines = new(3);

        public PayoutResult Calculate(in PayoutTerms terms, in DeliveryReport delivery)
        {
            _lines.Clear();

            int total = 0;

            if (delivery.IsSeized)
            {
                total += Add(PayoutReason.Seizure, -Math.Abs(terms.PenaltyPerSeizure));
            }
            else
            {
                total += Add(PayoutReason.Delivery, terms.RewardPerCrate);

                if (delivery.Spotless) total += Add(PayoutReason.CleanBonus, terms.CleanBonus);
            }

            PayoutLine[] lines = _lines.Count > 0 ? _lines.ToArray() : Array.Empty<PayoutLine>();

            return new PayoutResult(total, lines);
        }

        private int Add(PayoutReason reason, int amount)
        {
            if (amount == 0) 
                return 0;

            _lines.Add(new PayoutLine(reason, amount));

            return amount;
        }
    }
}

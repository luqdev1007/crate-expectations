using System;
using System.Collections.Generic;

namespace CrateExpectations.Economy
{
    public enum PayoutReason
    {
        Delivery,
        CleanBonus,
        Seizure,
    }

    public readonly struct PayoutLine
    {
        public PayoutLine(PayoutReason reason, int amount)
        {
            Reason = reason;
            Amount = amount;
        }

        public PayoutReason Reason { get; }

        public int Amount { get; }

        public override string ToString() => $"{Reason} {Amount:+#;-#;0}";
    }

    public readonly struct PayoutResult
    {
        private readonly IReadOnlyList<PayoutLine> _lines;

        public PayoutResult(int amount, IReadOnlyList<PayoutLine> lines)
        {
            Amount = amount;
            _lines = lines;
        }

        public int Amount { get; }

        public IReadOnlyList<PayoutLine> Lines => _lines ?? Array.Empty<PayoutLine>();

        public bool IsPenalty => Amount < 0;

        public bool Has(PayoutReason reason)
        {
            IReadOnlyList<PayoutLine> lines = Lines;

            for (int i = 0; i < lines.Count; i++)
                if (lines[i].Reason == reason) 
                    return true;

            return false;
        }

        public override string ToString() => $"{Amount:+#;-#;0} ({Lines.Count} строк)";
    }
}

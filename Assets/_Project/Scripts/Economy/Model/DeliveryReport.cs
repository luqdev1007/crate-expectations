namespace CrateExpectations.Economy
{
    public enum DeliveryOutcome
    {
        Cleared,
        Seized,
    }

    public readonly struct DeliveryReport
    {
        public DeliveryReport(DeliveryOutcome outcome, bool spotless = false)
        {
            Outcome = outcome;
            Spotless = spotless;
        }

        public DeliveryOutcome Outcome { get; }

        public bool Spotless { get; }

        public bool IsSeized => Outcome == DeliveryOutcome.Seized;
    }
}

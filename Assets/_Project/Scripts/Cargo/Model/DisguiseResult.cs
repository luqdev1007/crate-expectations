namespace CrateExpectations.Cargo
{
    public enum DisguiseOutcome
    {
        Applied,
        AlreadyApplied,
        Rejected,
    }

    public enum DisguiseRejection
    {
        None,
        IncompleteRecipe,
        PaintPrerequisite,
    }

    public readonly struct DisguiseResult
    {
        private DisguiseResult(
            DisguiseOutcome outcome,
            in CargoState state,
            bool divergesFromTruth,
            DisguiseRejection rejection)
        {
            Outcome = outcome;
            State = state;
            DivergesFromTruth = divergesFromTruth;
            Rejection = rejection;
        }

        public DisguiseOutcome Outcome { get; }

        public CargoState State { get; }

        public DisguiseRejection Rejection { get; }

        public bool DivergesFromTruth { get; }

        public bool Changed => Outcome == DisguiseOutcome.Applied;

        internal static DisguiseResult Applied(in CargoState state, bool diverges) =>
            new(DisguiseOutcome.Applied, state, diverges, DisguiseRejection.None);

        internal static DisguiseResult AlreadyApplied(in CargoState state, bool diverges) =>
            new(DisguiseOutcome.AlreadyApplied, state, diverges, DisguiseRejection.None);

        internal static DisguiseResult Rejected(in CargoState state, bool diverges, DisguiseRejection rejection) =>
            new(DisguiseOutcome.Rejected, state, diverges, rejection);
    }
}

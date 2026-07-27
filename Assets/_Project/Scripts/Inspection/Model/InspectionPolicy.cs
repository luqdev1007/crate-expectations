namespace CrateExpectations.Inspection 
{
    public readonly struct ClueWeights
    {
        public ClueWeights(
            float declaredContraband,
            float contentMismatch,
            float paintMismatch,
            float missingStamp,
            float wrongStamp,
            float incompleteDisguise)
        {
            DeclaredContraband = declaredContraband;
            ContentMismatch = contentMismatch;
            PaintMismatch = paintMismatch;
            MissingStamp = missingStamp;
            WrongStamp = wrongStamp;
            IncompleteDisguise = incompleteDisguise;
        }

        public float DeclaredContraband { get; }
        public float ContentMismatch { get; }
        public float PaintMismatch { get; }
        public float MissingStamp { get; }
        public float WrongStamp { get; }
        public float IncompleteDisguise { get; }

        public float Of(ClueType type) => type switch
        {
            ClueType.DeclaredContraband => DeclaredContraband,
            ClueType.ContentMismatch => ContentMismatch,
            ClueType.PaintMismatch => PaintMismatch,
            ClueType.MissingStamp => MissingStamp,
            ClueType.WrongStamp => WrongStamp,
            ClueType.IncompleteDisguise => IncompleteDisguise,
            _ => 0f,
        };
    }

    public readonly struct InspectionPolicy
    {
        public InspectionPolicy(
            ClueChecks checks,
            in ClueWeights weights,
            float suspicionThreshold,
            float overlookChance = 0f)
        {
            Checks = checks;
            Weights = weights;
            SuspicionThreshold = suspicionThreshold;
            OverlookChance = overlookChance;
        }

        public ClueChecks Checks { get; }

        public ClueWeights Weights { get; }

        public float SuspicionThreshold { get; }

        public float OverlookChance { get; }

        public bool Performs(ClueChecks check) => (Checks & check) != 0;
    }
}

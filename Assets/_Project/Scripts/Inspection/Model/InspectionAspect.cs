namespace CrateExpectations.Inspection
{
    public enum InspectionAspect
    {
        Manifest,
        Paint,
        Stamp,
        Contents,
    }

    public static class InspectionAspects
    {
        public static InspectionAspect Of(ClueType clue) => clue switch
        {
            ClueType.DeclaredContraband => InspectionAspect.Manifest,
            ClueType.ContentMismatch => InspectionAspect.Contents,
            ClueType.PaintMismatch => InspectionAspect.Paint,
            ClueType.IncompleteDisguise => InspectionAspect.Paint,
            ClueType.MissingStamp => InspectionAspect.Stamp,
            ClueType.WrongStamp => InspectionAspect.Stamp,
            _ => InspectionAspect.Manifest,
        };

        public static ClueChecks ChecksOf(InspectionAspect aspect) => aspect switch
        {
            InspectionAspect.Manifest => ClueChecks.Manifest,
            InspectionAspect.Paint => ClueChecks.Paint | ClueChecks.Completeness,
            InspectionAspect.Stamp => ClueChecks.Stamp,
            InspectionAspect.Contents => ClueChecks.Contents,
            _ => ClueChecks.None,
        };
    }
}

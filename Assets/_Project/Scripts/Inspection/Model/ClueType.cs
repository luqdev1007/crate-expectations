using System;

namespace CrateExpectations.Inspection
{
    public enum ClueType
    {
        DeclaredContraband,
        ContentMismatch,
        PaintMismatch,
        MissingStamp,
        WrongStamp,
        IncompleteDisguise,
    }

    [Flags]
    public enum ClueChecks
    {
        None = 0,
        Manifest = 1 << 0,
        Contents = 1 << 1,
        Paint = 1 << 2,
        Stamp = 1 << 3,
        Completeness = 1 << 4,
        All = Manifest | Contents | Paint | Stamp | Completeness,
    }
}

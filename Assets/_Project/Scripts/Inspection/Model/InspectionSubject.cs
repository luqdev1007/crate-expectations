using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection
{
    public readonly struct InspectionSubject
    {
        public InspectionSubject(
            in CargoState declared,
            in CargoIdentity truth,
            PaintDefinition expectedPaint = null,
            StampDefinition requiredStamp = null)
        {
            Declared = declared;
            Truth = truth;
            ExpectedPaint = expectedPaint;
            RequiredStamp = requiredStamp;
        }

        public CargoState Declared { get; }

        public CargoIdentity Truth { get; }

        public PaintDefinition ExpectedPaint { get; }

        public StampDefinition RequiredStamp { get; }
    }
}

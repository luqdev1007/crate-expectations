using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection.AI
{
    public readonly struct InspectionCase
    {
        public InspectionCase(CargoBox cargo, in Verdict verdict)
        {
            Cargo = cargo;
            Verdict = verdict;
        }

        public CargoBox Cargo { get; }

        public Verdict Verdict { get; }

        public bool IsOpen => Cargo != null;
    }
}

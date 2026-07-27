using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection.Events
{
    public readonly struct CargoInspected
    {
        public CargoInspected(CargoBox cargo, InspectorProfile inspector, in Verdict verdict)
        {
            Cargo = cargo;
            Inspector = inspector;
            Verdict = verdict;
        }

        public CargoBox Cargo { get; }

        public InspectorProfile Inspector { get; }

        public Verdict Verdict { get; }
    }
}

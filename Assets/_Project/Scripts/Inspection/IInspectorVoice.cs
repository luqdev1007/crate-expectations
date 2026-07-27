namespace CrateExpectations.Inspection
{
    public interface IInspectorVoice
    {
        void Say(string line);

        void ShowVerdict(in VerdictReport report);

        void Clear();
    }
}

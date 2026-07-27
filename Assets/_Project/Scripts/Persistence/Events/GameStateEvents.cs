namespace CrateExpectations.Persistence.Events
{
    public readonly struct GameSaved
    {
        public GameSaved(string slotName) => SlotName = slotName;

        public string SlotName { get; }
    }

    public readonly struct GameLoaded
    {
        public GameLoaded(string slotName) => SlotName = slotName;
        public string SlotName { get; }
    }

    public readonly struct GameStateFailed
    {
        public GameStateFailed(bool wasSaving, string reason)
        {
            WasSaving = wasSaving;
            Reason = reason;
        }

        public bool WasSaving { get; }

        public string Reason { get; }
    }
}

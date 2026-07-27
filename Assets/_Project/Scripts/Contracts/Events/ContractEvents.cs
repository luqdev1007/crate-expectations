namespace CrateExpectations.Contracts.Events
{
    public readonly struct ContractAccepted
    {
        public ContractAccepted(in ContractProgress progress) => Progress = progress;
        public ContractProgress Progress { get; }
    }

    public readonly struct ContractProgressed
    {
        public ContractProgressed(in ContractProgress progress, bool seized)
        {
            Progress = progress;
            Seized = seized;
        }

        public ContractProgress Progress { get; }

        public bool Seized { get; }
    }

    public readonly struct ContractCompleted
    {
        public ContractCompleted(in ContractProgress progress) => Progress = progress;

        public ContractProgress Progress { get; }
    }

    public readonly struct ContractFailed
    {
        public ContractFailed(in ContractProgress progress) => Progress = progress;

        public ContractProgress Progress { get; }
    }
}

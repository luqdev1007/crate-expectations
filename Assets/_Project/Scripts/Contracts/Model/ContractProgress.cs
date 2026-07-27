namespace CrateExpectations.Contracts
{
    public readonly struct ContractProgress
    {
        public ContractProgress(ContractDefinition contract, int delivered = 0, int seized = 0)
        {
            Contract = contract;
            Delivered = delivered;
            Seized = seized;
        }

        public ContractDefinition Contract { get; }

        public int Delivered { get; }

        public int Seized { get; }

        public bool IsActive => Contract != null;

        public int Required => Contract != null ? Contract.Crates : 0;

        public bool IsComplete => Contract != null && Delivered >= Contract.Crates;

        public bool IsFailed => Contract != null && Seized > Contract.AllowedSeizures;

        public ContractProgress WithDelivery() => new(Contract, Delivered + 1, Seized);

        public ContractProgress WithSeizure() => new(Contract, Delivered, Seized + 1);

        public override string ToString() => Contract != null
            ? $"{Contract.DisplayName}: {Delivered}/{Required} сдано, задержано {Seized}"
            : "заказа нет";
    }
}

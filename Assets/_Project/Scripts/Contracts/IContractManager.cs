using System.Collections.Generic;

namespace CrateExpectations.Contracts
{
    public interface IContractManager
    {
        ContractProgress Active { get; }

        IReadOnlyList<ContractDefinition> Available { get; }

        bool CanAccept(ContractDefinition contract);

        bool Accept(ContractDefinition contract);

        ContractSnapshot Capture();

        void Restore(in ContractSnapshot snapshot);
    }
}

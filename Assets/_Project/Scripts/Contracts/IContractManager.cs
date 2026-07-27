using System.Collections.Generic;

namespace CrateExpectations.Contracts
{
    public interface IContractManager
    {
        ContractProgress Active { get; }

        /// <summary>Что ещё висит на доске: каталог за вычетом уже взятых заказов.</summary>
        IReadOnlyList<ContractDefinition> Available { get; }

        /// <summary>Листок этого заказа уже сняли с доски - обратно он не вернётся.</summary>
        bool IsTaken(ContractDefinition contract);

        bool CanAccept(ContractDefinition contract);

        bool Accept(ContractDefinition contract);

        ContractSnapshot Capture();

        void Restore(in ContractSnapshot snapshot);
    }
}

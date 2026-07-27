using System;

namespace CrateExpectations.Contracts
{
    [Serializable]
    public struct ContractSnapshot
    {
        public string ContractId;

        public int Delivered;

        public int Seized;
    }
}

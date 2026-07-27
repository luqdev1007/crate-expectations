using System;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Economy;

namespace CrateExpectations.Persistence
{
    [Serializable]
    public sealed class GameSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;

        public EconomySnapshot Economy;

        public ContractSnapshot Contract;

        public CargoSceneSnapshot Cargo;

        public bool IsReadable => Version == CurrentVersion;
    }
}

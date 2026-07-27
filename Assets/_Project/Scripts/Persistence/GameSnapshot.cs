using System;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Economy;

namespace CrateExpectations.Persistence
{
    [Serializable]
    public sealed class GameSnapshot
    {
        // 2: в снимок заказов добавлен список уже снятых с доски листков
        public const int CurrentVersion = 2;

        public int Version;

        public EconomySnapshot Economy;

        public ContractSnapshot Contract;

        public CargoSceneSnapshot Cargo;

        public bool IsReadable => Version == CurrentVersion;
    }
}

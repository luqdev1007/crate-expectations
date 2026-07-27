using System;

namespace CrateExpectations.Contracts
{
    [Serializable]
    public struct ContractSnapshot
    {
        public string ContractId;

        public int Delivered;

        public int Seized;

        /// <summary>
        /// Заказы, листки которых уже сняли с доски. Обратно они не возвращаются никогда,
        /// поэтому список переживает сохранение наравне с активным заказом.
        /// </summary>
        public string[] TakenIds;
    }
}

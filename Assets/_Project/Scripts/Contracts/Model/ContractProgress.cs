namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Взятый заказ и сколько по нему уже сделано. Неизменяемое значение: каждая сдача
    /// порождает новый прогресс, а не правит старый, - поэтому событие с прогрессом
    /// можно спокойно отдавать подписчикам, не боясь, что оно "протухнет" у них в руках
    /// </summary>
    public readonly struct ContractProgress
    {
        public ContractProgress(ContractDefinition contract, int delivered = 0, int seized = 0)
        {
            Contract = contract;
            Delivered = delivered;
            Seized = seized;
        }

        /// <summary>Взятый заказ. <c>null</c> - активного заказа нет</summary>
        public ContractDefinition Contract { get; }

        /// <summary>Сколько ящиков приняли</summary>
        public int Delivered { get; }

        /// <summary>Сколько ящиков задержали</summary>
        public int Seized { get; }

        /// <summary>Заказ взят и ещё не закрыт</summary>
        public bool IsActive => Contract != null;

        /// <summary>Сколько ящиков нужно сдать всего</summary>
        public int Required => Contract != null ? Contract.Crates : 0;

        /// <summary>Сдано столько, сколько просили</summary>
        public bool IsComplete => Contract != null && Delivered >= Contract.Crates;

        /// <summary>Задержано больше, чем заказчик готов стерпеть</summary>
        public bool IsFailed => Contract != null && Seized > Contract.AllowedSeizures;

        /// <summary>Ящик приняли</summary>
        public ContractProgress WithDelivery() => new(Contract, Delivered + 1, Seized);

        /// <summary>Ящик задержали</summary>
        public ContractProgress WithSeizure() => new(Contract, Delivered, Seized + 1);

        public override string ToString() => Contract != null
            ? $"{Contract.DisplayName}: {Delivered}/{Required} сдано, задержано {Seized}"
            : "заказа нет";
    }
}

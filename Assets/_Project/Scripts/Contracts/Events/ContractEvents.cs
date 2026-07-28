namespace CrateExpectations.Contracts.Events
{
    /// <summary>Игрок взял заказ у доски</summary>
    public readonly struct ContractAccepted
    {
        public ContractAccepted(in ContractProgress progress) => Progress = progress;

        /// <summary>Взятый заказ с нулевым прогрессом</summary>
        public ContractProgress Progress { get; }
    }

    /// <summary>
    /// По заказу сдали очередной ящик - приняли его или задержали. Публикуется на каждую
    /// засчитанную сдачу, в том числе на провальную: "задержали второй из трёх" - тоже прогресс
    /// </summary>
    public readonly struct ContractProgressed
    {
        public ContractProgressed(in ContractProgress progress, bool seized)
        {
            Progress = progress;
            Seized = seized;
        }

        /// <summary>Состояние заказа после сдачи</summary>
        public ContractProgress Progress { get; }

        /// <summary>Последний ящик задержали</summary>
        public bool Seized { get; }
    }

    /// <summary>Заказ закрыт: сдано столько ящиков, сколько просили</summary>
    public readonly struct ContractCompleted
    {
        public ContractCompleted(in ContractProgress progress) => Progress = progress;

        /// <summary>Прогресс на момент закрытия</summary>
        public ContractProgress Progress { get; }
    }

    /// <summary>Заказ провален: задержано больше ящиков, чем заказчик готов стерпеть</summary>
    public readonly struct ContractFailed
    {
        public ContractFailed(in ContractProgress progress) => Progress = progress;

        /// <summary>Прогресс на момент провала</summary>
        public ContractProgress Progress { get; }
    }
}

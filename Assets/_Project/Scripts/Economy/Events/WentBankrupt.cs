namespace CrateExpectations.Economy.Events
{
    /// <summary>
    /// Долг перевалил за предел, который порт готов терпеть. Событие одноразовое:
    /// публикуется в момент пересечения границы, а не каждый раз, пока баланс в минусе.
    /// Что с этим делать - конец смены, экран проигрыша, визит коллекторов - решает
    /// подписчик; <c>Economy</c> только считает
    /// </summary>
    public readonly struct WentBankrupt
    {
        public WentBankrupt(int balance, int debtLimit)
        {
            Balance = balance;
            DebtLimit = debtLimit;
        }

        /// <summary>Каким баланс стал</summary>
        public int Balance { get; }

        /// <summary>Предел долга, который был перейдён</summary>
        public int DebtLimit { get; }
    }
}

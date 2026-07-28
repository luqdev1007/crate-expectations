using CrateExpectations.Core.Events;
using CrateExpectations.Economy.Events;

namespace CrateExpectations.Economy
{
    /// <summary>
    /// Кошелёк игрока. Обычный C#-класс: ни <c>MonoBehaviour</c>, ни <c>ScriptableObject</c> -
    /// правила приходят значением, а наружу он говорит только через шину событий.
    ///
    /// <para><b>Про минус.</b> Баланс уходить в минус <i>разрешено</i>. Клампить в ноль
    /// было бы удобнее, но тогда штраф перестаёт что-либо значить ровно в тот момент,
    /// когда игрок и так на дне: разорившийся контрабандист платил бы за провалы нулём.
    /// Долг остаётся долгом, а когда он переваливает за
    /// <see cref="EconomyRules.DebtLimit"/> - один раз публикуется
    /// <see cref="WentBankrupt"/>, и что с этим делать, решает уже не экономика.</para>
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private readonly EconomyRules _rules;
        private readonly IEventBus _bus;

        public EconomyService(EconomyRules rules, IEventBus bus)
        {
            _rules = rules;
            _bus = bus;
            Balance = rules.StartingBalance;
        }

        /// <inheritdoc />
        public int Balance { get; private set; }

        /// <inheritdoc />
        public bool IsBankrupt => Balance < -_rules.DebtLimit;

        /// <inheritdoc />
        public void Apply(in PayoutResult payout)
        {
            if (payout.Amount == 0) return;

            bool wasBankrupt = IsBankrupt;
            Balance += payout.Amount;

            _bus.Publish(new BalanceChanged(Balance, payout));

            // Банкротство объявляется в момент пересечения границы, а не каждый раз,
            // пока баланс в минусе: иначе подписчик получал бы его после каждого штрафа
            if (!wasBankrupt && IsBankrupt)
                _bus.Publish(new WentBankrupt(Balance, _rules.DebtLimit));
        }

        /// <inheritdoc />
        public EconomySnapshot Capture() => new() { Balance = Balance };

        /// <summary>
        /// Загрузка - не начисление. Событие изменения баланса несёт разбивку "за что",
        /// а у восстановленного баланса причины нет: он просто такой. Поэтому здесь тихо,
        /// а перерисоваться подписчикам говорит тот, кто загрузку затеял
        /// </summary>
        public void Restore(in EconomySnapshot snapshot) => Balance = snapshot.Balance;
    }
}

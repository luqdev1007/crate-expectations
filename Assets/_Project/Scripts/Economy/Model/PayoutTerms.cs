namespace CrateExpectations.Economy
{
    /// <summary>
    /// Денежная сторона сделки: сколько платят за ящик, сколько снимают за провал
    /// и что причитается сверху за безупречную работу. Всё, что деньгам нужно знать
    /// о контракте, - здесь; про груз, маскировку и инспекторов они не знают ничего.
    /// Снимается с <c>ContractDefinition</c> так же, как политика снимается с профиля инспектора
    /// </summary>
    public readonly struct PayoutTerms
    {
        public PayoutTerms(int rewardPerCrate, int penaltyPerSeizure, int cleanBonus = 0)
        {
            RewardPerCrate = rewardPerCrate;
            PenaltyPerSeizure = penaltyPerSeizure;
            CleanBonus = cleanBonus;
        }

        /// <summary>Плата за принятый ящик</summary>
        public int RewardPerCrate { get; }

        /// <summary>Сколько снимут за задержанный ящик</summary>
        public int PenaltyPerSeizure { get; }

        /// <summary>Надбавка за ящик, к которому не придрались вовсе</summary>
        public int CleanBonus { get; }
    }
}

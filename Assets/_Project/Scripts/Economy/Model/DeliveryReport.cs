namespace CrateExpectations.Economy
{
    /// <summary>Чем кончилась сдача ящика</summary>
    public enum DeliveryOutcome
    {
        /// <summary>Ящик приняли - за него платят</summary>
        Cleared,

        /// <summary>Ящик задержали - за него отвечают деньгами</summary>
        Seized,
    }

    /// <summary>
    /// Итог одной сдачи глазами бухгалтерии. Намеренно беден: ни улик, ни порогов,
    /// ни имени инспектора - только "взяли или нет" и "придирались ли". Перевод вердикта
    /// в эти два факта делает верхний слой (контракты), и ровно поэтому
    /// <c>Economy</c> не зависит ни от <c>Inspection</c>, ни от <c>Cargo</c>.
    /// </summary>
    public readonly struct DeliveryReport
    {
        public DeliveryReport(DeliveryOutcome outcome, bool spotless = false)
        {
            Outcome = outcome;
            Spotless = spotless;
        }

        /// <summary>Приняли или задержали</summary>
        public DeliveryOutcome Outcome { get; }

        /// <summary>Придраться было не к чему - основание для надбавки</summary>
        public bool Spotless { get; }

        /// <summary>Ящик задержан</summary>
        public bool IsSeized => Outcome == DeliveryOutcome.Seized;
    }
}

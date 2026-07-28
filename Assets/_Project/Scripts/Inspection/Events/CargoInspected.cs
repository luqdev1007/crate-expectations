using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection.Events
{
    /// <summary>
    /// Досмотр закончился. Событие несёт всё, что понадобится дальше по цепочке -
    /// какой ящик смотрели, кто смотрел и что решил, - но ни слова про деньги, штрафы
    /// и контракты: <c>Inspection</c> о них не знает и знать не должен. Кто будет
    /// начислять выплату, а кто отбирать груз, решают подписчики
    /// </summary>
    public readonly struct CargoInspected
    {
        public CargoInspected(CargoBox cargo, InspectorProfile inspector, in Verdict verdict)
        {
            Cargo = cargo;
            Inspector = inspector;
            Verdict = verdict;
        }

        /// <summary>Досмотренный ящик: у него можно спросить и заявленное состояние, и истину</summary>
        public CargoBox Cargo { get; }

        /// <summary>Кто выносил решение</summary>
        public InspectorProfile Inspector { get; }

        /// <summary>Исход с причинами: <see cref="Verdict.Outcome"/> и список улик</summary>
        public Verdict Verdict { get; }
    }
}

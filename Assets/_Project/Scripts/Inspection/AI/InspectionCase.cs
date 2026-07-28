using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Разбираемый случай: конкретный ящик и уже вынесенный по нему вердикт. Заводится
    /// в начале осмотра и живёт до оглашения - благодаря этому оценщик вызывается ровно
    /// один раз, а показанное на экране и ушедшее в шину событие не могут разойтись
    /// </summary>
    public readonly struct InspectionCase
    {
        public InspectionCase(CargoBox cargo, in Verdict verdict)
        {
            Cargo = cargo;
            Verdict = verdict;
        }

        /// <summary>Осматриваемый ящик</summary>
        public CargoBox Cargo { get; }

        /// <summary>Что по нему решено</summary>
        public Verdict Verdict { get; }

        /// <summary>Случай заведён и ещё не закрыт</summary>
        public bool IsOpen => Cargo != null;
    }
}

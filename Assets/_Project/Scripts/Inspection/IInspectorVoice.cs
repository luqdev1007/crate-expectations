namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Куда инспектор говорит. Порт модуля: сам он не знает ни про Canvas, ни про TMP -
    /// сейчас за интерфейсом экранная плашка, позже туда встанет полноценный UI-слой
    /// или субтитры, и ни одно состояние FSM об этом не узнает
    /// </summary>
    public interface IInspectorVoice
    {
        /// <summary>Реплика по ходу осмотра. Пустая строка - убрать текст</summary>
        void Say(string line);

        /// <summary>Показать итог досмотра</summary>
        void ShowVerdict(in VerdictReport report);

        /// <summary>Убрать с экрана всё: инспектор закончил</summary>
        void Clear();
    }
}

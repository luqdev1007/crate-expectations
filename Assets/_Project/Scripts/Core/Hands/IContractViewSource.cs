namespace CrateExpectations.Core.Hands
{
    /// <summary>
    /// Кто отвечает на вопрос «листок заказа поднят?». Листок занимает руки так же,
    /// как ящик или сабля, но живёт в UI и про бой ничего не знает - поэтому связь
    /// идёт через интерфейс в Core, а не прямой ссылкой
    /// </summary>
    public interface IContractViewSource
    {
        /// <summary>Листок в руках или как раз поднимается</summary>
        bool IsRaised { get; }
    }
}

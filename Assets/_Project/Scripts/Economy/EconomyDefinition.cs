using UnityEngine;

namespace CrateExpectations.Economy
{
    /// <summary>
    /// Правила кошелька в виде значения: отдельно от ассета, чтобы <see cref="EconomyService"/>
    /// оставался обычным C#-классом (тест заводит его одной строкой, без сцены)
    /// </summary>
    public readonly struct EconomyRules
    {
        public EconomyRules(int startingBalance, int debtLimit)
        {
            StartingBalance = startingBalance;
            DebtLimit = debtLimit;
        }

        public int StartingBalance { get; }

        /// <summary>Насколько глубоко в минус порт пускает игрока, прежде чем считать банкротом</summary>
        public int DebtLimit { get; }
    }

    /// <summary>
    /// Стартовые деньги и глубина допустимого долга: чисел экономики в коде нет,
    /// баланс игры крутится здесь
    /// </summary>
    [CreateAssetMenu(
        fileName = "EconomyDefinition",
        menuName = "CrateExpectations/Economy/Economy Definition")]
    public sealed class EconomyDefinition : ScriptableObject
    {
        [Tooltip("Баланс в начале смены")]
        [field: SerializeField] public int StartingBalance { get; private set; } = 500;

        [Tooltip("Насколько глубоко можно уйти в минус, прежде чем сработает банкротство")]
        [field: SerializeField][Min(0)] public int DebtLimit { get; private set; } = 400;

        /// <summary>Правила в виде значения - то, с чем работает <see cref="EconomyService"/></summary>
        public EconomyRules Rules => new(StartingBalance, DebtLimit);
    }
}

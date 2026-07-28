using System;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Источник случайности для логики досмотра. Существует ради тестов: ядро не имеет права
    /// дёргать <c>UnityEngine.Random</c>, иначе прогон перестаёт быть воспроизводимым
    /// </summary>
    public interface IChanceSource
    {
        /// <summary>Следующее число в диапазоне [0, 1)</summary>
        float NextUnit();
    }

    /// <summary>
    /// Случайность от seed. Один и тот же seed даёт одну и ту же последовательность,
    /// поэтому досмотр можно воспроизвести в тесте или переиграть в отладке
    /// </summary>
    public sealed class SeededChanceSource : IChanceSource
    {
        private readonly Random _random;

        public SeededChanceSource(int seed) => _random = new Random(seed);

        /// <inheritdoc />
        public float NextUnit() => (float)_random.NextDouble();
    }
}

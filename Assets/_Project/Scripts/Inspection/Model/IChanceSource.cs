using System;

namespace CrateExpectations.Inspection
{
    public interface IChanceSource
    {
        float NextUnit();
    }

    public sealed class SeededChanceSource : IChanceSource
    {
        private readonly Random _random;

        public SeededChanceSource(int seed) => _random = new Random(seed);

        public float NextUnit() => (float)_random.NextDouble();
    }
}

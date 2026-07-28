namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Одна найденная улика: что не сошлось и насколько это подозрительно для конкретного
    /// инспектора. Вес уже посчитан по профилю - вердикт не нужно пересчитывать,
    /// чтобы его озвучить или показать
    /// </summary>
    public readonly struct Clue
    {
        public Clue(ClueType type, float weight)
        {
            Type = type;
            Weight = weight;
        }

        /// <summary>Что не сошлось</summary>
        public ClueType Type { get; }

        /// <summary>Вклад в уровень подозрения по профилю инспектора</summary>
        public float Weight { get; }

        public override string ToString() => $"{Type} (+{Weight:0.#})";
    }
}

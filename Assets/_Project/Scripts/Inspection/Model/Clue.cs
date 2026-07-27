namespace CrateExpectations.Inspection
{
    public readonly struct Clue
    {
        public Clue(ClueType type, float weight)
        {
            Type = type;
            Weight = weight;
        }

        public ClueType Type { get; }

        public float Weight { get; }

        public override string ToString() => $"{Type} (+{Weight:0.#})";
    }
}

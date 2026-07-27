using System;
using System.Collections.Generic;

namespace CrateExpectations.Inspection
{
    public enum VerdictOutcome
    {
        Pass,
        Bust,
    }

    public readonly struct Verdict
    {
        private readonly IReadOnlyList<Clue> _clues;

        public Verdict(
            VerdictOutcome outcome, float suspicion, float threshold, IReadOnlyList<Clue> clues)
        {
            Outcome = outcome;
            Suspicion = suspicion;
            Threshold = threshold;
            _clues = clues;
        }

        public VerdictOutcome Outcome { get; }

        public float Suspicion { get; }

        public float Threshold { get; }

        public IReadOnlyList<Clue> Clues => _clues ?? Array.Empty<Clue>();

        public bool IsBust => Outcome == VerdictOutcome.Bust;

        public bool Has(ClueType type)
        {
            IReadOnlyList<Clue> clues = Clues;

            for (int i = 0; i < clues.Count; i++)
            {
                if (clues[i].Type == type) 
                    return true;
            }

            return false;
        }

        public override string ToString() =>
            $"{Outcome}: подозрение {Suspicion:0.#}/{Threshold:0.#}, улик - {Clues.Count}";
    }
}

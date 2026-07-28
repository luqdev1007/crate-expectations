using System;
using System.Collections.Generic;

namespace CrateExpectations.Inspection
{
    /// <summary>Чем закончился досмотр</summary>
    public enum VerdictOutcome
    {
        /// <summary>Груз пропущен</summary>
        Pass,

        /// <summary>Инспектор набрал достаточно подозрений - груз задержан</summary>
        Bust,
    }

    /// <summary>
    /// Результат досмотра. Не булев флаг: вердикт несёт причины, чтобы инспектор мог их
    /// озвучить, а UI - показать, из чего сложилось решение
    /// </summary>
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

        /// <summary>Пропустить или задержать</summary>
        public VerdictOutcome Outcome { get; }

        /// <summary>Сколько подозрения набралось</summary>
        public float Suspicion { get; }

        /// <summary>Порог, с которого этот инспектор задерживает груз</summary>
        public float Threshold { get; }

        /// <summary>Найденные улики. Никогда не <c>null</c>; пустой список - груз чист</summary>
        public IReadOnlyList<Clue> Clues => _clues ?? Array.Empty<Clue>();

        /// <summary>Груз задержан</summary>
        public bool IsBust => Outcome == VerdictOutcome.Bust;

        /// <summary>Нашлась ли улика такого типа</summary>
        public bool Has(ClueType type)
        {
            IReadOnlyList<Clue> clues = Clues;
            for (int i = 0; i < clues.Count; i++)
            {
                if (clues[i].Type == type) return true;
            }

            return false;
        }

        public override string ToString() =>
            $"{Outcome}: подозрение {Suspicion:0.#}/{Threshold:0.#}, улик - {Clues.Count}";
    }
}

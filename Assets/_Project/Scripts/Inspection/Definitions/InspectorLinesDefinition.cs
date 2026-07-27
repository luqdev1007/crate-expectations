using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    [CreateAssetMenu(
        fileName = "InspectorLines",
        menuName = "CrateExpectations/Inspection/Inspector Lines")]
    public sealed class InspectorLinesDefinition : ScriptableObject
    {
        [Serializable]
        public struct AspectLine
        {
            public InspectionAspect Aspect;
            public string Line;
        }

        [Serializable]
        public struct ClueLine
        {
            public ClueType Clue;
            public string Line;
        }

        [Header("Ход осмотра")]
        [field: SerializeField] public string Greeting { get; private set; } = "Так, посмотрим.";

        [SerializeField] private AspectLine[] _probes = Array.Empty<AspectLine>();

        [Header("Улики")]
        [SerializeField] private ClueLine[] _reasons = Array.Empty<ClueLine>();

        [Header("Вердикт")]
        [field: SerializeField] public string PassHeadline { get; private set; } = "ПРОПУЩЕНО";
        [field: SerializeField] public string BustHeadline { get; private set; } = "ЗАДЕРЖАНО";
        [field: SerializeField] public string PassLine { get; private set; } = "Ладно, проезжайте.";
        [field: SerializeField] public string BustLine { get; private set; } = "Никуда вы это не повезёте.";
        [field: SerializeField] public string CleanLine { get; private set; } = "Придраться не к чему.";

        [Tooltip("Подпись шкалы: {0} - подозрение, {1} - порог задержания")]
        [field: SerializeField] public string SuspicionFormat { get; private set; } =
            "подозрение {0:0} из {1:0}";

        [Tooltip("Маркер строки в перечне улик")]
        [field: SerializeField] public string CluePrefix { get; private set; } = "• ";

        [Header("Цвета исхода")]
        [field: SerializeField] public Color PassColor { get; private set; } = new(0.35f, 0.85f, 0.45f, 1f);
        [field: SerializeField] public Color BustColor { get; private set; } = new(0.9f, 0.3f, 0.3f, 1f);
        [field: SerializeField] public string MissingLine { get; private set; } = "…";

        private readonly StringBuilder _builder = new(128);

        public string Probe(InspectionAspect aspect)
        {
            for (int i = 0; i < _probes.Length; i++)
                if (_probes[i].Aspect == aspect) 
                    return _probes[i].Line;

            return MissingLine;
        }

        public string Reason(ClueType clue)
        {
            for (int i = 0; i < _reasons.Length; i++)
                if (_reasons[i].Clue == clue) 
                    return _reasons[i].Line;

            return MissingLine;
        }

        public VerdictReport Narrate(in Verdict verdict)
        {
            bool found = TryFindHeaviest(verdict, out Clue heaviest);

            string reason = found ? Reason(heaviest.Type) : CleanLine;
            string closing = verdict.IsBust ? BustLine : PassLine;

            return new VerdictReport(
                verdict.IsBust ? BustHeadline : PassHeadline,
                reason + " " + closing,
                BuildClueList(verdict),
                string.Format(SuspicionFormat, verdict.Suspicion, verdict.Threshold),
                verdict.IsBust ? BustColor : PassColor,
                verdict);
        }

        private static bool TryFindHeaviest(in Verdict verdict, out Clue heaviest)
        {
            IReadOnlyList<Clue> clues = verdict.Clues;
            heaviest = default;

            if (clues.Count == 0) 
                return false;

            heaviest = clues[0];

            for (int i = 1; i < clues.Count; i++)
                if (clues[i].Weight > heaviest.Weight) 
                    heaviest = clues[i];

            return true;
        }

        private string BuildClueList(in Verdict verdict)
        {
            IReadOnlyList<Clue> clues = verdict.Clues;

            if (clues.Count == 0) 
                return string.Empty;

            _builder.Clear();

            for (int i = 0; i < clues.Count; i++)
            {
                if (i > 0) 
                    _builder.Append('\n');

                _builder.Append(CluePrefix).Append(Reason(clues[i].Type));
            }

            return _builder.ToString();
        }
    }
}

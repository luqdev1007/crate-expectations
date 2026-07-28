using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Голос инспектора: что он говорит на каждом шаге осмотра, как называет найденную улику
    /// и чем заканчивает досмотр. Отдельный ассет от <see cref="InspectorProfile"/>, потому что
    /// "насколько он строг" и "как он разговаривает" меняют разные люди: первое - геймдизайнер,
    /// второе - сценарист. Профиль ссылается на нужный голос, поэтому смена профиля меняет
    /// и характер реплик, и исход - без единой правки в коде
    /// </summary>
    [CreateAssetMenu(
        fileName = "InspectorLines",
        menuName = "CrateExpectations/Inspection/Inspector Lines")]
    public sealed class InspectorLinesDefinition : ScriptableObject
    {
        /// <summary>Реплика, с которой инспектор начинает шаг осмотра</summary>
        [Serializable]
        public struct AspectLine
        {
            [Tooltip("Шаг осмотра")]
            public InspectionAspect Aspect;
            [Tooltip("Что инспектор говорит, приступая к нему")]
            public string Line;
        }

        /// <summary>Как инспектор озвучивает конкретную улику</summary>
        [Serializable]
        public struct ClueLine
        {
            [Tooltip("Найденная улика")]
            public ClueType Clue;
            [Tooltip("Что инспектор говорит, наткнувшись на неё")]
            public string Line;
        }

        [Header("Ход осмотра")]
        [field: SerializeField] public string Greeting { get; private set; } = "Так, посмотрим.";

        [Tooltip("Реплики по шагам осмотра")]
        [SerializeField] private AspectLine[] _probes = Array.Empty<AspectLine>();

        [Header("Улики")]
        [SerializeField] private ClueLine[] _reasons = Array.Empty<ClueLine>();

        [Header("Вердикт")]
        [field: SerializeField] public string PassHeadline { get; private set; } = "ПРОПУЩЕНО";
        [Tooltip("Заголовок задержанного груза")]
        [field: SerializeField] public string BustHeadline { get; private set; } = "ЗАДЕРЖАНО";
        [Tooltip("Заключительная фраза, когда груз пропускают")]
        [field: SerializeField] public string PassLine { get; private set; } = "Ладно, проезжайте.";
        [Tooltip("Заключительная фраза, когда груз задерживают")]
        [field: SerializeField] public string BustLine { get; private set; } = "Никуда вы это не повезёте.";
        [Tooltip("Что он говорит, когда придраться не к чему")]
        [field: SerializeField] public string CleanLine { get; private set; } = "Придраться не к чему.";

        [Tooltip("Подпись шкалы: {0} - подозрение, {1} - порог задержания")]
        [field: SerializeField] public string SuspicionFormat { get; private set; } =
            "подозрение {0:0} из {1:0}";

        [Tooltip("Маркер строки в перечне улик")]
        [field: SerializeField] public string CluePrefix { get; private set; } = "• ";

        [Header("Цвета исхода")]
        [field: SerializeField] public Color PassColor { get; private set; } = new(0.35f, 0.85f, 0.45f, 1f);
        [Tooltip("Цвет задержанного груза")]
        [field: SerializeField] public Color BustColor { get; private set; } = new(0.9f, 0.3f, 0.3f, 1f);
        [Tooltip("Строка на месте пропущенного текста - чтобы дыра в данных была видна сразу")]
        [field: SerializeField] public string MissingLine { get; private set; } = "…";

        // Перечень улик собирается раз на досмотр, но буфер всё равно переиспользуется:
        // строить его заново на каждый вердикт незачем
        private readonly StringBuilder _builder = new(128);

        /// <summary>
        /// Реплика к шагу осмотра. Список короткий и обходится линейно - ни словаря,
        /// ни аллокаций
        /// </summary>
        public string Probe(InspectionAspect aspect)
        {
            for (int i = 0; i < _probes.Length; i++)
                if (_probes[i].Aspect == aspect) 
                    return _probes[i].Line;

            return MissingLine;
        }

        /// <summary>Как инспектор озвучивает эту улику</summary>
        public string Reason(ClueType clue)
        {
            for (int i = 0; i < _reasons.Length; i++)
                if (_reasons[i].Clue == clue) 
                    return _reasons[i].Line;

            return MissingLine;
        }

        /// <summary>
        /// Превратить вердикт в то, что можно показать и озвучить. Если улик несколько,
        /// вслух называется самая весомая - остальные остаются в перечне на экране
        /// </summary>
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

        /// <summary>Самая весомая улика - та, которую инспектор ставит игроку в упрёк</summary>
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

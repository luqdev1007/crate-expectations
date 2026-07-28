using System;
using System.Collections.Generic;
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
        [Tooltip("Заключительная фраза, когда груз пропускают")]
        [field: SerializeField] public string PassLine { get; private set; } = "Ладно, проезжайте.";
        [Tooltip("Заключительная фраза, когда груз задерживают")]
        [field: SerializeField] public string BustLine { get; private set; } = "Никуда вы это не повезёте.";
        [Tooltip("Что он говорит, когда придраться не к чему")]
        [field: SerializeField] public string CleanLine { get; private set; } = "Придраться не к чему.";

        [Header("Цвета исхода")]
        [field: SerializeField] public Color PassColor { get; private set; } = new(0.35f, 0.85f, 0.45f, 1f);
        [Tooltip("Цвет задержанного груза")]
        [field: SerializeField] public Color BustColor { get; private set; } = new(0.9f, 0.3f, 0.3f, 1f);
        [Tooltip("Строка на месте пропущенного текста - чтобы дыра в данных была видна сразу")]
        [field: SerializeField] public string MissingLine { get; private set; } = "…";

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
        /// Превратить вердикт в то, что можно озвучить. Если улик несколько, вслух называется
        /// самая весомая: экран показывает только печать, и перечислять остальные негде
        /// </summary>
        public VerdictReport Narrate(in Verdict verdict)
        {
            bool found = TryFindHeaviest(verdict, out Clue heaviest);

            string reason = found ? Reason(heaviest.Type) : CleanLine;
            string closing = verdict.IsBust ? BustLine : PassLine;

            return new VerdictReport(
                reason + " " + closing,
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
    }
}

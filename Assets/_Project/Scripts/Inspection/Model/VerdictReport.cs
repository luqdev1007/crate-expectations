using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Вердикт, приготовленный к показу: заголовок, реплика, перечень улик и шкала подозрения.
    /// Существует ради того, чтобы экранный слой оставался тупым - он только раскладывает
    /// готовые строки по полям и не знает ни про <see cref="ClueType"/>, ни про пороги.
    /// Собирается из <see cref="Verdict"/> и текстов в <see cref="InspectorLinesDefinition"/>
    /// </summary>
    public readonly struct VerdictReport
    {
        public VerdictReport(
            string headline, string speech, string clues, string scale, Color accent, in Verdict verdict)
        {
            Headline = headline;
            Speech = speech;
            Clues = clues;
            Scale = scale;
            Accent = accent;
            IsBust = verdict.IsBust;
            Pressure = verdict.Threshold > 0f
                ? Mathf.Clamp01(verdict.Suspicion / verdict.Threshold)
                : verdict.Suspicion > 0f ? 1f : 0f;
        }

        /// <summary>Исход одним словом: "пропущено" или "задержано"</summary>
        public string Headline { get; }

        /// <summary>Что инспектор говорит вслух: причина и заключительная фраза</summary>
        public string Speech { get; }

        /// <summary>Перечисление найденных улик. Пустая строка - придраться было не к чему</summary>
        public string Clues { get; }

        /// <summary>Подпись к шкале подозрения</summary>
        public string Scale { get; }

        /// <summary>Цвет исхода: им красятся заголовок, шкала и сама зона досмотра</summary>
        public Color Accent { get; }

        /// <summary>Груз задержан</summary>
        public bool IsBust { get; }

        /// <summary>Заполненность шкалы подозрения, 0..1 (подозрение относительно порога)</summary>
        public float Pressure { get; }
    }
}

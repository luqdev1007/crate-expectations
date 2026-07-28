using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Вердикт, приготовленный к показу: что инспектор говорит вслух и чем дело кончилось.
    /// Существует ради того, чтобы экранный слой оставался тупым - он берёт готовое
    /// и не знает ни про <see cref="ClueType"/>, ни про пороги. Собирается из
    /// <see cref="Verdict"/> и текстов в <see cref="InspectorLinesDefinition"/>
    /// </summary>
    public readonly struct VerdictReport
    {
        public VerdictReport(string speech, Color accent, in Verdict verdict)
        {
            Speech = speech;
            Accent = accent;
            IsBust = verdict.IsBust;
        }

        /// <summary>Что инспектор говорит вслух: причина и заключительная фраза</summary>
        public string Speech { get; }

        /// <summary>Цвет исхода: им красятся реплика инспектора и сама зона досмотра</summary>
        public Color Accent { get; }

        /// <summary>Груз задержан</summary>
        public bool IsBust { get; }
    }
}

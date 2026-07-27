using UnityEngine;

namespace CrateExpectations.Inspection
{
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

        public string Headline { get; }

        public string Speech { get; }

        public string Clues { get; }

        public string Scale { get; }

        public Color Accent { get; }

        public bool IsBust { get; }

        public float Pressure { get; }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.Inspection.Tests
{
    public sealed class InspectorLinesTests
    {
        private const string CleanLine = "Придраться не к чему.";
        private const string PassLine = "Проезжайте.";
        private const string BustLine = "Груз задержан.";
        private const string Placeholder = "…";

        private static readonly Color Pass = new(0.2f, 0.8f, 0.3f, 1f);
        private static readonly Color Bust = new(0.9f, 0.2f, 0.2f, 1f);

        private InspectorLinesDefinition _lines;

        [SetUp]
        public void SetUp() => _lines = CreateLines();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_lines);

        [Test]
        public void CleanVerdict_SaysThereIsNothingToComplainAbout()
        {
            VerdictReport report = _lines.Narrate(Clean());

            Assert.That(report.Speech, Is.EqualTo(CleanLine + " " + PassLine));
            Assert.That(report.IsBust, Is.False);
            Assert.That(report.Accent, Is.EqualTo(Pass));
        }

        [Test]
        public void HeaviestClue_IsTheOneSaidOutLoud()
        {
            Verdict verdict = Busted(
                new Clue(ClueType.MissingStamp, 20f),
                new Clue(ClueType.ContentMismatch, 70f),
                new Clue(ClueType.PaintMismatch, 25f));

            VerdictReport report = _lines.Narrate(verdict);

            Assert.That(report.Speech, Is.EqualTo(Reason(ClueType.ContentMismatch) + " " + BustLine));
            Assert.That(report.IsBust, Is.True);
            Assert.That(report.Accent, Is.EqualTo(Bust));
        }

        [Test]
        public void PassedCargoWithMinorClues_StillNamesWhatWasNoticed()
        {
            var verdict = new Verdict(VerdictOutcome.Pass, 25f, 40f,
                new[] { new Clue(ClueType.PaintMismatch, 25f) });

            VerdictReport report = _lines.Narrate(verdict);

            Assert.That(report.IsBust, Is.False, "груз пропустили, хоть и поворчали");
            Assert.That(report.Speech, Is.EqualTo(Reason(ClueType.PaintMismatch) + " " + PassLine));
        }

        [Test]
        public void MissingText_FallsBackToAPlaceholderInsteadOfEmptiness()
        {
            Assert.That(_lines.Reason(ClueType.DeclaredContraband), Is.EqualTo(Placeholder));
            Assert.That(_lines.Probe(InspectionAspect.Contents), Is.EqualTo(Placeholder));
        }

        [Test]
        public void ProbeLine_IsLookedUpByAspect()
        {
            Assert.That(_lines.Probe(InspectionAspect.Paint), Is.EqualTo("Окраска."));
            Assert.That(_lines.Probe(InspectionAspect.Stamp), Is.EqualTo("Пломба."));
        }

        private static Verdict Clean() =>
            new(VerdictOutcome.Pass, 0f, 40f, System.Array.Empty<Clue>());

        private static Verdict Busted(params Clue[] clues)
        {
            float suspicion = 0f;

            for (int i = 0; i < clues.Length; i++) 
                suspicion += clues[i].Weight;

            return new Verdict(VerdictOutcome.Bust, suspicion, 40f, clues);
        }

        private static string Reason(ClueType clue) => "причина:" + clue;

        private static InspectorLinesDefinition CreateLines()
        {
            var lines = ScriptableObject.CreateInstance<InspectorLinesDefinition>();
            var serialized = new SerializedObject(lines);

            SetProbe(serialized, 0, InspectionAspect.Manifest, "Документы.");
            SetProbe(serialized, 1, InspectionAspect.Paint, "Окраска.");
            SetProbe(serialized, 2, InspectionAspect.Stamp, "Пломба.");

            ClueType[] withReasons =
            {
                ClueType.ContentMismatch,
                ClueType.PaintMismatch,
                ClueType.MissingStamp,
                ClueType.WrongStamp,
                ClueType.IncompleteDisguise,
            };

            SerializedProperty reasons = serialized.FindProperty("_reasons");
            reasons.arraySize = withReasons.Length;
            for (int i = 0; i < withReasons.Length; i++)
            {
                SerializedProperty entry = reasons.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Clue").enumValueIndex = (int)withReasons[i];
                entry.FindPropertyRelative("Line").stringValue = Reason(withReasons[i]);
            }

            SetString(serialized, "PassLine", PassLine);
            SetString(serialized, "BustLine", BustLine);
            SetString(serialized, "CleanLine", CleanLine);
            SetString(serialized, "MissingLine", Placeholder);

            serialized.FindProperty("<PassColor>k__BackingField").colorValue = Pass;
            serialized.FindProperty("<BustColor>k__BackingField").colorValue = Bust;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return lines;
        }

        private static void SetProbe(
            SerializedObject serialized, int index, InspectionAspect aspect, string line)
        {
            SerializedProperty probes = serialized.FindProperty("_probes");
            if (probes.arraySize <= index) probes.arraySize = index + 1;

            SerializedProperty entry = probes.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Aspect").enumValueIndex = (int)aspect;
            entry.FindPropertyRelative("Line").stringValue = line;
        }

        private static void SetString(SerializedObject serialized, string property, string value) =>
            serialized.FindProperty($"<{property}>k__BackingField").stringValue = value;
    }
}

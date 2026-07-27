using System.Collections.Generic;
using CrateExpectations.Cargo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.Inspection.Tests
{
    public sealed class ClueEvaluatorTests
    {
        private const string MeticulousProfilePath =
            "Assets/_Project/Data/Inspection/InspectorProfile_Meticulous.asset";

        private const string LazyProfilePath =
            "Assets/_Project/Data/Inspection/InspectorProfile_Lazy.asset";

        private const string ContrabandCargoPath =
            "Assets/_Project/Data/Cargo/CargoType_Rum.asset";

        private ClueEvaluator _evaluator;

        private CargoTypeDefinition _rum;      // контрабанда
        private CargoTypeDefinition _spices;   // легальный груз
        private PaintDefinition _navy;         // окраска, положенная специям
        private PaintDefinition _bare;
        private StampDefinition _portSeal;     // пломба, положенная специям
        private StampDefinition _customsSeal;

        private CargoIdentity _rumInside;
        private CargoIdentity _spicesInside;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new ClueEvaluator();

            _rum = CreateCargoType(contraband: true);
            _spices = CreateCargoType(contraband: false);
            _navy = ScriptableObject.CreateInstance<PaintDefinition>();
            _bare = ScriptableObject.CreateInstance<PaintDefinition>();
            _portSeal = ScriptableObject.CreateInstance<StampDefinition>();
            _customsSeal = ScriptableObject.CreateInstance<StampDefinition>();

            _rumInside = new CargoIdentity(_rum);
            _spicesInside = new CargoIdentity(_spices);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rum);
            Object.DestroyImmediate(_spices);
            Object.DestroyImmediate(_navy);
            Object.DestroyImmediate(_bare);
            Object.DestroyImmediate(_portSeal);
            Object.DestroyImmediate(_customsSeal);
        }

        private static InspectionPolicy Meticulous(float threshold = 40f, float overlook = 0f) =>
            new(ClueChecks.All, new ClueWeights(100f, 70f, 25f, 20f, 30f, 20f), threshold, overlook);

        private static InspectionPolicy Lazy() =>
            new(ClueChecks.Stamp, new ClueWeights(100f, 70f, 10f, 20f, 15f, 10f), 60f);

        private InspectionSubject DressedAsSpices(in CargoIdentity truth) =>
            new(new CargoState(_navy, _portSeal, _spices), truth, _navy, _portSeal);

        [Test]
        public void PerfectDisguise_PassesEvenMeticulousInspector()
        {
            Verdict verdict = _evaluator.Evaluate(DressedAsSpices(_rumInside), Meticulous());

            Assert.That(verdict.Outcome, Is.EqualTo(VerdictOutcome.Pass));
            Assert.That(verdict.Clues, Is.Empty);
        }

        [Test]
        public void PlainContraband_IsBustedOnTheManifest()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, null, _rum), _rumInside);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            Assert.That(verdict.Outcome, Is.EqualTo(VerdictOutcome.Bust));
            Assert.That(verdict.Has(ClueType.DeclaredContraband), Is.True);
        }

        [Test]
        public void WrongPaint_IsTheOnlyClueFound()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, _portSeal, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            AssertSingleClue(verdict, ClueType.PaintMismatch);
        }

        [Test]
        public void MissingStamp_IsTheOnlyClueFound()
        {
            var subject = new InspectionSubject(
                new CargoState(_navy, null, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            AssertSingleClue(verdict, ClueType.MissingStamp);
        }

        [Test]
        public void WrongStamp_IsTheOnlyClueFound()
        {
            var subject = new InspectionSubject(
                new CargoState(_navy, _customsSeal, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            AssertSingleClue(verdict, ClueType.WrongStamp);
        }

        [Test]
        public void UnpaintedCrate_ReportsIncompleteDisguise()
        {
            var subject = new InspectionSubject(
                new CargoState(null, _portSeal, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            AssertSingleClue(verdict, ClueType.IncompleteDisguise);
            Assert.That(verdict.Has(ClueType.PaintMismatch), Is.False);
        }

        [Test]
        public void ContentMismatch_IsFoundOnlyWhenAppearanceBetraysIt()
        {
            Verdict hidden = _evaluator.Evaluate(DressedAsSpices(_rumInside), Meticulous());

            var sloppy = new InspectionSubject(
                new CargoState(_bare, _portSeal, _spices), _rumInside, _navy, _portSeal);
            Verdict caught = _evaluator.Evaluate(sloppy, Meticulous());

            Assert.That(hidden.Has(ClueType.ContentMismatch), Is.False, "инспектор не рентген: безупречную маскировку он вскрывать не станет");
            Assert.That(caught.Has(ClueType.ContentMismatch), Is.True);
            Assert.That(caught.Has(ClueType.PaintMismatch), Is.True);
        }

        [Test]
        public void LegalCargoInRegulationDress_Passes()
        {
            Verdict verdict = _evaluator.Evaluate(DressedAsSpices(_spicesInside), Meticulous());

            Assert.That(verdict.Outcome, Is.EqualTo(VerdictOutcome.Pass));
            Assert.That(verdict.Clues, Is.Empty);
        }

        [Test]
        public void SameCargo_DifferentProfiles_DifferentOutcome()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, _portSeal, _spices), _rumInside, _navy, _portSeal);

            Verdict strict = _evaluator.Evaluate(subject, Meticulous());
            Verdict lazy = _evaluator.Evaluate(subject, Lazy());

            Assert.That(strict.Outcome, Is.EqualTo(VerdictOutcome.Bust));
            Assert.That(lazy.Outcome, Is.EqualTo(VerdictOutcome.Pass));
            Assert.That(lazy.Clues, Is.Empty, "ленивый на окраску вообще не смотрит");
        }

        [Test]
        public void ShippedProfiles_DisagreeOnTheSameCrate()
        {
            var strict = AssetDatabase.LoadAssetAtPath<InspectorProfile>(MeticulousProfilePath);
            var lazy = AssetDatabase.LoadAssetAtPath<InspectorProfile>(LazyProfilePath);
            var contraband = AssetDatabase.LoadAssetAtPath<CargoTypeDefinition>(ContrabandCargoPath);

            Assert.That(strict, Is.Not.Null, MeticulousProfilePath);
            Assert.That(lazy, Is.Not.Null, LazyProfilePath);
            Assert.That(contraband, Is.Not.Null, ContrabandCargoPath);

            var subject = new InspectionSubject(
                new CargoState(null, null, contraband), new CargoIdentity(contraband));

            Verdict strictVerdict = _evaluator.Evaluate(subject, strict.Policy);
            Verdict lazyVerdict = _evaluator.Evaluate(subject, lazy.Policy);

            Assert.That(strictVerdict.Outcome, Is.EqualTo(VerdictOutcome.Bust));
            Assert.That(lazyVerdict.Outcome, Is.EqualTo(VerdictOutcome.Pass));
        }

        [Test]
        public void SmallClues_BelowThreshold_Pass()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, _portSeal, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            Assert.That(verdict.Suspicion, Is.EqualTo(25f).Within(0.001f));
            Assert.That(verdict.Outcome, Is.EqualTo(VerdictOutcome.Pass));
        }

        [Test]
        public void SmallClues_AboveThreshold_Bust()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, null, _spices), _spicesInside, _navy, _portSeal);

            Verdict verdict = _evaluator.Evaluate(subject, Meticulous());

            Assert.That(verdict.Suspicion, Is.EqualTo(45f).Within(0.001f));
            Assert.That(verdict.Outcome, Is.EqualTo(VerdictOutcome.Bust));
        }

        [Test]
        public void Overlooking_IsReproducibleWithTheSameSeed()
        {
            var subject = new InspectionSubject(
                new CargoState(null, _customsSeal, _spices), _rumInside, _navy, _portSeal);
            InspectionPolicy policy = Meticulous(overlook: 0.5f);

            Verdict first = new ClueEvaluator(new SeededChanceSource(4242)).Evaluate(subject, policy);
            Verdict second = new ClueEvaluator(new SeededChanceSource(4242)).Evaluate(subject, policy);

            Assert.That(ClueTypesOf(second), Is.EqualTo(ClueTypesOf(first)));
            Assert.That(second.Suspicion, Is.EqualTo(first.Suspicion).Within(0.001f));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        }

        [Test]
        public void InspectorWhoOverlooksEverything_FindsNothing()
        {
            var subject = new InspectionSubject(
                new CargoState(null, _customsSeal, _rum), _rumInside, _navy, _portSeal);

            Verdict attentive = _evaluator.Evaluate(subject, Meticulous());
            Verdict distracted = new ClueEvaluator(new SeededChanceSource(1))
                .Evaluate(subject, Meticulous(overlook: 1f));

            Assert.That(attentive.Outcome, Is.EqualTo(VerdictOutcome.Bust));
            Assert.That(attentive.Clues, Is.Not.Empty);
            Assert.That(distracted.Clues, Is.Empty);
            Assert.That(distracted.Outcome, Is.EqualTo(VerdictOutcome.Pass));
        }

        [Test]
        public void Evaluation_LeavesTruthUntouched()
        {
            var subject = new InspectionSubject(
                new CargoState(_bare, null, _spices), _rumInside, _navy, _portSeal);

            _evaluator.Evaluate(subject, Meticulous());

            Assert.That(subject.Truth.TrueType, Is.SameAs(_rum));
            Assert.That(_rumInside.TrueType, Is.SameAs(_rum));
        }

        [Test]
        public void PortRegulations_SupplyRequirementsForDeclaredCargo()
        {
            var regulations = ScriptableObject.CreateInstance<PortRegulationsDefinition>();
            try
            {
                var serialized = new SerializedObject(regulations);
                SerializedProperty entries = serialized.FindProperty("_requirements");
                entries.arraySize = 1;
                SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("CargoType").objectReferenceValue = _spices;
                entry.FindPropertyRelative("Paint").objectReferenceValue = _navy;
                entry.FindPropertyRelative("Stamp").objectReferenceValue = _portSeal;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                InspectionSubject known = regulations.CreateSubject(
                    new CargoState(_navy, _portSeal, _spices), _rumInside);
                InspectionSubject unknown = regulations.CreateSubject(
                    new CargoState(_navy, _portSeal, _rum), _rumInside);

                Assert.That(known.ExpectedPaint, Is.SameAs(_navy));
                Assert.That(known.RequiredStamp, Is.SameAs(_portSeal));
                Assert.That(unknown.ExpectedPaint, Is.Null, "груза нет в регламенте - требований к нему нет");
                Assert.That(unknown.RequiredStamp, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(regulations);
            }
        }

        private static void AssertSingleClue(in Verdict verdict, ClueType expected)
        {
            Assert.That(verdict.Clues.Count, Is.EqualTo(1), "ожидалась ровно одна улика, а найдено: " + string.Join(", ", ClueTypesOf(verdict)));
            Assert.That(verdict.Clues[0].Type, Is.EqualTo(expected));
        }

        private static List<ClueType> ClueTypesOf(in Verdict verdict)
        {
            var types = new List<ClueType>(verdict.Clues.Count);

            for (int i = 0; i < verdict.Clues.Count; i++) 
                types.Add(verdict.Clues[i].Type);

            return types;
        }

        private static CargoTypeDefinition CreateCargoType(bool contraband)
        {
            var type = ScriptableObject.CreateInstance<CargoTypeDefinition>();

            var serialized = new SerializedObject(type);
            serialized.FindProperty("<IsContraband>k__BackingField").boolValue = contraband;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return type;
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Cargo.Tests
{
    public sealed class DisguiseProcessorTests
    {
        private DisguiseProcessor _processor;

        private CargoTypeDefinition _rum;
        private CargoTypeDefinition _spices;
        private PaintDefinition _bare;
        private PaintDefinition _navy;
        private StampDefinition _portSeal;
        private StampDefinition _customsSeal;

        private CargoIdentity _rumIdentity;

        [SetUp]
        public void SetUp()
        {
            _processor = new DisguiseProcessor();

            _rum = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _spices = ScriptableObject.CreateInstance<CargoTypeDefinition>();
            _bare = ScriptableObject.CreateInstance<PaintDefinition>();
            _navy = ScriptableObject.CreateInstance<PaintDefinition>();
            _portSeal = ScriptableObject.CreateInstance<StampDefinition>();
            _customsSeal = ScriptableObject.CreateInstance<StampDefinition>();

            _rumIdentity = new CargoIdentity(_rum);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rum);
            Object.DestroyImmediate(_spices);
            Object.DestroyImmediate(_bare);
            Object.DestroyImmediate(_navy);
            Object.DestroyImmediate(_portSeal);
            Object.DestroyImmediate(_customsSeal);
        }

        private CargoState FreshRumCrate() => CargoState.Undisguised(_rumIdentity, _bare);

        [Test]
        public void Paint_ChangesDeclaredPaint()
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity, new DisguiseOperation(DisguiseAction.Paint, paint: _navy));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Applied));
            Assert.That(result.State.Paint, Is.SameAs(_navy));
        }

        [Test]
        public void Paint_DoesNotTouchTruth()
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity, new DisguiseOperation(DisguiseAction.Paint, paint: _navy));

            Assert.That(_rumIdentity.TrueType, Is.SameAs(_rum));
            Assert.That(result.State.DeclaredType, Is.SameAs(_rum));
            Assert.That(result.DivergesFromTruth, Is.False);
        }

        [Test]
        public void Paint_AppliedTwice_ReportsAlreadyAppliedAndKeepsState()
        {
            CargoState state = FreshRumCrate();
            var operation = new DisguiseOperation(DisguiseAction.Paint, paint: _navy);

            CargoState painted = _processor.Apply(state, _rumIdentity, operation).State;
            DisguiseResult again = _processor.Apply(painted, _rumIdentity, operation);

            Assert.That(again.Outcome, Is.EqualTo(DisguiseOutcome.AlreadyApplied));
            Assert.That(again.Changed, Is.False);
            Assert.That(again.State, Is.EqualTo(painted));
        }

        [Test]
        public void Stamp_PutsSealOnCrate()
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity, new DisguiseOperation(DisguiseAction.Stamp, stamp: _portSeal));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Applied));
            Assert.That(result.State.Stamp, Is.SameAs(_portSeal));
        }

        [Test]
        public void Stamp_WithAnotherSeal_ReplacesPrevious()
        {
            CargoState stamped = _processor.Apply(
                FreshRumCrate(), _rumIdentity,
                new DisguiseOperation(DisguiseAction.Stamp, stamp: _portSeal)).State;

            DisguiseResult result = _processor.Apply(
                stamped, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Stamp, stamp: _customsSeal));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Applied));
            Assert.That(result.State.Stamp, Is.SameAs(_customsSeal));
        }

        [Test]
        public void Pour_ChangesDeclaredTypeButNotTruth()
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Pour, declaredType: _spices));

            Assert.That(result.State.DeclaredType, Is.SameAs(_spices));
            Assert.That(_rumIdentity.TrueType, Is.SameAs(_rum));
            Assert.That(result.State.MatchesTruth(_rumIdentity), Is.False);
        }

        [Test]
        public void Pour_BackToTrueType_RemovesDivergence()
        {
            CargoState disguised = _processor.Apply(
                FreshRumCrate(), _rumIdentity,
                new DisguiseOperation(DisguiseAction.Pour, declaredType: _spices)).State;

            DisguiseResult result = _processor.Apply(
                disguised, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Pour, declaredType: _rum));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Applied));
            Assert.That(result.DivergesFromTruth, Is.False);
        }

        [TestCase(DisguiseAction.Paint)]
        [TestCase(DisguiseAction.Stamp)]
        [TestCase(DisguiseAction.Pour)]
        public void Recipe_WithoutTarget_IsRejectedAndKeepsState(DisguiseAction action)
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity, new DisguiseOperation(action));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Rejected));
            Assert.That(result.Rejection, Is.EqualTo(DisguiseRejection.IncompleteRecipe));
            Assert.That(result.State, Is.EqualTo(state));
        }

        [Test]
        public void PaintPrerequisite_NotMet_RejectsWithoutChangingState()
        {
            CargoState state = FreshRumCrate();

            DisguiseResult result = _processor.Apply(
                state, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Stamp, stamp: _portSeal, requiredPaint: _navy));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Rejected));
            Assert.That(result.Rejection, Is.EqualTo(DisguiseRejection.PaintPrerequisite));
            Assert.That(result.State, Is.EqualTo(state));
        }

        [Test]
        public void PaintPrerequisite_Met_Applies()
        {
            CargoState painted = _processor.Apply(
                FreshRumCrate(), _rumIdentity,
                new DisguiseOperation(DisguiseAction.Paint, paint: _navy)).State;

            DisguiseResult result = _processor.Apply(
                painted, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Stamp, stamp: _portSeal, requiredPaint: _navy));

            Assert.That(result.Outcome, Is.EqualTo(DisguiseOutcome.Applied));
            Assert.That(result.State.Stamp, Is.SameAs(_portSeal));
        }

        [Test]
        public void FullDisguise_LeavesDeclaredStateDivergedFromTruth()
        {
            CargoState state = FreshRumCrate();

            state = _processor.Apply(state, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Paint, paint: _navy)).State;
            state = _processor.Apply(state, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Stamp, stamp: _portSeal, requiredPaint: _navy)).State;
            DisguiseResult last = _processor.Apply(state, _rumIdentity,
                new DisguiseOperation(DisguiseAction.Pour, declaredType: _spices));

            state = last.State;

            Assert.That(state.Paint, Is.SameAs(_navy));
            Assert.That(state.Stamp, Is.SameAs(_portSeal));
            Assert.That(state.DeclaredType, Is.SameAs(_spices));
            Assert.That(last.DivergesFromTruth, Is.True);
            Assert.That(_rumIdentity.TrueType, Is.SameAs(_rum));
            Assert.That(_rumIdentity.IsContraband, Is.EqualTo(_rum.IsContraband));
        }
    }
}

using System;
using System.Collections.Generic;

namespace CrateExpectations.Inspection
{
    public sealed class ClueEvaluator
    {
        private readonly List<Clue> _found = new(6);

        private readonly IChanceSource _chance;

        public ClueEvaluator(IChanceSource chance = null) => _chance = chance;

        public Verdict Evaluate(in InspectionSubject subject, in InspectionPolicy policy)
        {
            _found.Clear();
            float suspicion = 0f;

            suspicion += CollectAppearanceClues(subject, policy);
            bool appearanceBetrayed = _found.Count > 0;

            if (policy.Performs(ClueChecks.Manifest) && IsDeclaredContraband(subject))
                suspicion += TryAdd(ClueType.DeclaredContraband, policy);

            if (policy.Performs(ClueChecks.Contents) && appearanceBetrayed &&
                !subject.Declared.MatchesTruth(subject.Truth))
            {
                suspicion += TryAdd(ClueType.ContentMismatch, policy);
            }

            VerdictOutcome outcome = suspicion >= policy.SuspicionThreshold
                ? VerdictOutcome.Bust
                : VerdictOutcome.Pass;

            Clue[] clues = _found.Count > 0 ? _found.ToArray() : Array.Empty<Clue>();

            return new Verdict(outcome, suspicion, policy.SuspicionThreshold, clues);
        }

        private float CollectAppearanceClues(in InspectionSubject subject, in InspectionPolicy policy)
        {
            float suspicion = 0f;

            bool paintRequired = subject.ExpectedPaint != null;
            bool paintMissing = paintRequired && subject.Declared.Paint == null;
            bool paintWrong = paintRequired && subject.Declared.Paint != null &&
                              subject.Declared.Paint != subject.ExpectedPaint;

            if (paintMissing && policy.Performs(ClueChecks.Completeness))
                suspicion += TryAdd(ClueType.IncompleteDisguise, policy);

            if (paintWrong && policy.Performs(ClueChecks.Paint))
                suspicion += TryAdd(ClueType.PaintMismatch, policy);

            bool stampRequired = subject.RequiredStamp != null;
            if (stampRequired && policy.Performs(ClueChecks.Stamp))
            {
                if (subject.Declared.Stamp == null)
                    suspicion += TryAdd(ClueType.MissingStamp, policy);
                else if (subject.Declared.Stamp != subject.RequiredStamp)
                    suspicion += TryAdd(ClueType.WrongStamp, policy);
            }

            return suspicion;
        }

        private static bool IsDeclaredContraband(in InspectionSubject subject) =>
            subject.Declared.DeclaredType != null && subject.Declared.DeclaredType.IsContraband;

        private float TryAdd(ClueType type, in InspectionPolicy policy)
        {
            if (IsOverlooked(policy)) 
                return 0f;

            float weight = policy.Weights.Of(type);
            _found.Add(new Clue(type, weight));

            return weight;
        }

        private bool IsOverlooked(in InspectionPolicy policy)
        {
            if (_chance == null || policy.OverlookChance <= 0f)
                return false;

            return _chance.NextUnit() < policy.OverlookChance;
        }
    }
}

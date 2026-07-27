using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorExamineState : IState
    {
        private readonly IInspectorContext _context;

        private CargoBox _subject;

        public InspectorExamineState(IInspectorContext context) => _context = context;

        public void Enter()
        {
            _subject = _context.Zone.Occupant;

            if (_subject == null)
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            Verdict verdict = _context.OpenCase(_subject);
            PlayAsync(_subject, verdict, _context.StateToken).Forget();
        }

        public void Tick(float deltaTime)
        {
            if (ReferenceEquals(_context.Zone.Occupant, _subject))
                return;

            _context.GoTo(InspectorPhase.Idle);
        }

        public void Exit() => _subject = null;

        private async UniTaskVoid PlayAsync(CargoBox cargo, Verdict verdict, CancellationToken token)
        {
            IReadOnlyList<ExamineStep> steps = _context.Definition.Steps;
            ClueChecks performed = _context.Profile.Checks;

            for (int i = 0; i < steps.Count; i++)
            {
                ExamineStep step = steps[i];
                if ((InspectionAspects.ChecksOf(step.Aspect) & performed) == 0) 
                    continue;

                if (await PlayStepAsync(cargo, verdict, step, token)) 
                    return;
            }

            _context.Focus.Hide();
            _context.GoTo(InspectorPhase.Verdict);
        }

        private async UniTask<bool> PlayStepAsync(
            CargoBox cargo, Verdict verdict, ExamineStep step, CancellationToken token)
        {
            bool suspicious = TryFindClue(verdict, step.Aspect, out Clue clue);

            _context.Focus.Show(cargo.transform.TransformPoint(step.FocusOffset), suspicious);
            _context.Voice.Say(_context.Lines.Probe(step.Aspect));

            if (await Pause.ForAsync(step.Seconds, token)) 
                return true;

            if (!suspicious) 
                return await Pause.ForAsync(_context.Definition.StepPauseSeconds, token);

            _context.Voice.Say(_context.Lines.Reason(clue.Type));

            return await Pause.ForAsync(_context.Definition.ClueReactionSeconds, token);
        }

        private static bool TryFindClue(in Verdict verdict, InspectionAspect aspect, out Clue found)
        {
            IReadOnlyList<Clue> clues = verdict.Clues;

            for (int i = 0; i < clues.Count; i++)
            {
                if (InspectionAspects.Of(clues[i].Type) != aspect) 
                    continue;

                found = clues[i];
                return true;
            }

            found = default;

            return false;
        }
    }
}

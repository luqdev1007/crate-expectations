using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Осмотр по шагам. Оценщик дёргается ровно один раз - на входе в состояние; шаги только
    /// отыгрывают уже готовый вердикт, поэтому увиденное на экране и ушедшее в шину событие
    /// не могут разойтись. Шаг, проверку которого профиль не выполняет, пропускается:
    /// ленивый инспектор и осматривает груз заметно короче дотошного.
    /// Выход: шаги кончились (в <see cref="InspectorPhase.Verdict"/>) либо груз забрали
    /// со стола (в <see cref="InspectorPhase.Idle"/>)
    /// </summary>
    public sealed class InspectorExamineState : IState
    {
        private readonly IInspectorContext _context;

        private CargoBox _subject;

        public InspectorExamineState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            _subject = _context.Zone.Occupant;
            if (_subject == null)
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            Verdict verdict = _context.OpenCase(_subject);
            PlayAsync(verdict, _context.StateToken).Forget();
        }

        /// <summary>
        /// Единственное дело тика - заметить, что груз со стола унесли. Сам осмотр идёт
        /// асинхронно и сворачивается по токену, который отменит смена состояния
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (ReferenceEquals(_context.Zone.Occupant, _subject)) return;

            _context.GoTo(InspectorPhase.Idle);
        }

        /// <inheritdoc />
        public void Exit() => _subject = null;

        /// <summary>
        /// Шаги осмотра. Метод переживает отмену молча: <see cref="UniTask"/> сворачивается
        /// через <c>SuppressCancellationThrow</c>, поэтому забранный со стола груз не рождает
        /// ни исключения, ни "догоняющих" реплик
        /// </summary>
        private async UniTaskVoid PlayAsync(Verdict verdict, CancellationToken token)
        {
            IReadOnlyList<ExamineStep> steps = _context.Definition.Steps;
            ClueChecks performed = _context.Profile.Checks;

            for (int i = 0; i < steps.Count; i++)
            {
                ExamineStep step = steps[i];
                if ((InspectionAspects.ChecksOf(step.Aspect) & performed) == 0) continue;

                if (await PlayStepAsync(verdict, step, token)) return;
            }

            _context.GoTo(InspectorPhase.Verdict);
        }

        /// <summary>Один шаг: посмотреть и прокомментировать</summary>
        /// <returns><c>true</c>, если осмотр отменили и продолжать нечего</returns>
        private async UniTask<bool> PlayStepAsync(
            Verdict verdict, ExamineStep step, CancellationToken token)
        {
            bool suspicious = TryFindClue(verdict, step.Aspect, out Clue clue);

            _context.Voice.Say(_context.Lines.Probe(step.Aspect));

            if (await Pause.ForAsync(step.Seconds, token)) return true;

            // Чисто - идём дальше; нашёл улику - задерживается и называет её вслух
            if (!suspicious) return await Pause.ForAsync(_context.Definition.StepPauseSeconds, token);

            _context.Voice.Say(_context.Lines.Reason(clue.Type));
            return await Pause.ForAsync(_context.Definition.ClueReactionSeconds, token);
        }

        /// <summary>Улика, которая всплывает именно на этом шаге. Перебор - улик единицы</summary>
        private static bool TryFindClue(in Verdict verdict, InspectionAspect aspect, out Clue found)
        {
            IReadOnlyList<Clue> clues = verdict.Clues;
            for (int i = 0; i < clues.Count; i++)
            {
                if (InspectionAspects.Of(clues[i].Type) != aspect) continue;

                found = clues[i];
                return true;
            }

            found = default;
            return false;
        }
    }
}

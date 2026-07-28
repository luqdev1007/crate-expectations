using System.Threading;
using CrateExpectations.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Оглашение исхода: инспектор выдерживает паузу, называет решение и причину, красит зону
    /// в цвет вердикта и закрывает случай - тем самым выпуская событие в шину.
    /// Забирать груз со стола на этом шаге уже поздно: решение принято в
    /// <see cref="InspectorExamineState"/>, здесь его только произносят.
    /// Выход: результат отвисел положенное время - обратно в <see cref="InspectorPhase.Idle"/>
    /// </summary>
    public sealed class InspectorVerdictState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorVerdictState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            if (!_context.Case.IsOpen)
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            AnnounceAsync(_context.StateToken).Forget();
        }

        /// <summary>Держит инспектора лицом к столу, даже если груз с него уже сняли</summary>
        public void Tick(float deltaTime) =>
            _context.Motor.FaceTowards(_context.Zone.transform.position, deltaTime);

        /// <inheritdoc />
        public void Exit()
        {
        }

        private async UniTaskVoid AnnounceAsync(CancellationToken token)
        {
            InspectionDefinition definition = _context.Definition;

            if (await Pause.ForAsync(definition.VerdictDelaySeconds, token)) return;

            VerdictReport report = _context.Lines.Narrate(_context.Case.Verdict);
            _context.Voice.ShowVerdict(report);
            _context.Zone.SetColorOverride(report.Accent);

            // Закрываем случай сразу после показа: экран и шина получают один и тот же
            // вердикт, а повторно этот ящик инспектор уже не тронет
            _context.CloseCase();

            if (await Pause.ForAsync(definition.VerdictHoldSeconds, token)) return;

            _context.GoTo(InspectorPhase.Idle);
        }
    }
}

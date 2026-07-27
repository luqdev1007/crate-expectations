using System.Threading;
using CrateExpectations.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorVerdictState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorVerdictState(IInspectorContext context) => _context = context;

        public void Enter()
        {
            if (!_context.Case.IsOpen)
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            AnnounceAsync(_context.StateToken).Forget();
        }

        public void Tick(float deltaTime) =>
            _context.Motor.FaceTowards(_context.Zone.transform.position, deltaTime);

        public void Exit()
        {
        }

        private async UniTaskVoid AnnounceAsync(CancellationToken token)
        {
            InspectionDefinition definition = _context.Definition;

            if (await Pause.ForAsync(definition.VerdictDelaySeconds, token)) 
                return;

            VerdictReport report = _context.Lines.Narrate(_context.Case.Verdict);
            _context.Voice.ShowVerdict(report);
            _context.Zone.SetColorOverride(report.Accent);

            _context.CloseCase();

            if (await Pause.ForAsync(definition.VerdictHoldSeconds, token)) 
                return;

            _context.GoTo(InspectorPhase.Idle);
        }
    }
}

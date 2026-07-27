using CrateExpectations.Cargo;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorApproachState : IState
    {
        private readonly IInspectorContext _context;

        private CargoBox _subject;

        public InspectorApproachState(IInspectorContext context) => _context = context;

        public void Enter()
        {
            _subject = _context.Zone.Occupant;

            if (_subject == null)
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            _context.Voice.Say(_context.Lines.Greeting);
        }

        public void Tick(float deltaTime)
        {
            if (!ReferenceEquals(_context.Zone.Occupant, _subject))
            {
                _context.GoTo(InspectorPhase.Idle);
                return;
            }

            if (!_context.Motor.MoveTo(_context.ExaminePoint.position, deltaTime)) 
                return;

            if (!_context.Motor.FaceTowards(_subject.transform.position, deltaTime))
                return;

            _context.GoTo(InspectorPhase.Examine);
        }

        public void Exit() => _subject = null;
    }
}

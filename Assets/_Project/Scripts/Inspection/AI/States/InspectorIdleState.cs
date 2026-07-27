using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorIdleState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorIdleState(IInspectorContext context) => _context = context;

        public void Enter()
        {
            _context.Voice.Clear();
            _context.Focus.Hide();
            _context.Zone.ClearColorOverride();
        }

        public void Tick(float deltaTime) => _context.GoTo(_context.AwaitsInspection
            ? InspectorPhase.Approach
            : InspectorPhase.Return);

        public void Exit()
        {
        }
    }
}

using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorReturnState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorReturnState(IInspectorContext context) => _context = context;

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

            if (!_context.Motor.MoveTo(_context.Post.position, deltaTime)) 
                return;

            if (!_context.Motor.FaceLike(_context.Post.rotation, deltaTime)) 
                return;

            _context.GoTo(InspectorPhase.Patrol);
        }

        public void Exit()
        {
        }
    }
}

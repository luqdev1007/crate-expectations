using CrateExpectations.Cargo;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorNoticeState : IState
    {
        private readonly IInspectorContext _context;

        private CargoBox _subject;
        private float _remaining;

        public InspectorNoticeState(IInspectorContext context) => _context = context;

        public void Enter()
        {
            _subject = _context.ApproachingCargo;
            _remaining = _context.Definition.NoticeHoldSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

            if (_subject != null)
            {
                _context.Motor.FaceTowards(_subject.transform.position, deltaTime);
            }

            _remaining -= deltaTime;

            if (_remaining > 0f) 
                return;

            _context.GoTo(InspectorPhase.Patrol);
        }

        public void Exit() => _subject = null;
    }
}

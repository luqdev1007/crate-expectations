using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Возвращение на пост. Отдельное состояние, а не хвост <see cref="InspectorIdleState"/>:
    /// "прибрать за досмотром" и "дойти до места" - разные дела, и разводить их стоило хотя бы
    /// потому, что прибирать надо мгновенно и один раз, а идти - сколько потребуется.
    /// Груз, поставленный на стол по дороге, разворачивает инспектора с полпути.
    /// Выход: пришёл и встал в позу поста - в <see cref="InspectorPhase.Patrol"/>
    /// </summary>
    public sealed class InspectorReturnState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorReturnState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

            if (!_context.Motor.MoveTo(_context.Post.position, deltaTime)) return;
            if (!_context.Motor.FaceLike(_context.Post.rotation, deltaTime)) return;

            _context.GoTo(InspectorPhase.Patrol);
        }

        /// <inheritdoc />
        public void Exit()
        {
        }
    }
}

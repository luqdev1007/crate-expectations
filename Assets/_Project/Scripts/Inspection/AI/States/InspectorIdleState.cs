using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Уборщик и стрелочник. Прибирает за предыдущим досмотром - экран, указку и подсветку
    /// зоны, - поэтому в Idle можно свалиться откуда угодно, в том числе из прерванного
    /// осмотра, и сцена всё равно останется чистой. Своего поведения у состояния нет:
    /// прибрав, оно тут же отправляет инспектора по делу или домой.
    /// Выход: за грузом - в <see cref="InspectorPhase.Approach"/>, иначе на пост -
    /// в <see cref="InspectorPhase.Return"/>
    /// </summary>
    public sealed class InspectorIdleState : IState
    {
        private readonly IInspectorContext _context;

        public InspectorIdleState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            _context.Voice.Clear();
            _context.Focus.Hide();
            _context.Zone.ClearColorOverride();
        }

        /// <inheritdoc />
        public void Tick(float deltaTime) => _context.GoTo(_context.AwaitsInspection
            ? InspectorPhase.Approach
            : InspectorPhase.Return);

        /// <inheritdoc />
        public void Exit()
        {
        }
    }
}

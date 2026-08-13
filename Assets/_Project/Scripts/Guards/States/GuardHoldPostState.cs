using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Пост: стражник стоит там, где его поставили, и никуда не идёт.
    /// <para>
    /// <see cref="Tick"/> пуст, и это не заглушка: картинку держит сам блендтри -
    /// <c>GuardAnimatorDriver</c> пишет в <c>Speed</c> нулевую скорость агента,
    /// а на нулевом пороге стоит idle-клип. Заводить здесь ещё и вызов аниматора
    /// значило бы описывать одно и то же в двух местах
    /// </para>
    /// </summary>
    public sealed class GuardHoldPostState : IState
    {
        private readonly IGuardStateContext _context;

        public GuardHoldPostState(IGuardStateContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            // isStopped у агента вне навмеша - ошибка в консоли, а не тихий отказ
            if (_context.Agent.isOnNavMesh)
                _context.Agent.isStopped = true;
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
        }

        /// <inheritdoc />
        public void Exit()
        {
        }
    }
}

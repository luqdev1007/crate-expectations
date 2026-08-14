using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Стражника ударили: он стоит, пока играет вздрагивание.
    /// <para>
    /// <see cref="Tick"/> пуст, как и у <see cref="GuardHoldPostState"/>, и по той же
    /// причине - картинку держит граф, а не состояние. Отличие одно: срок этой паузы
    /// считает не машина, а <see cref="GuardHitReaction"/>, который знает длину клипа.
    /// Состояние живёт ровно столько, сколько взведён его флаг, и считать в себе срок
    /// собственной жизни ему незачем.
    /// </para>
    /// <para>
    /// Отдельное состояние, а не «стоп агенту» из компонента реакции, - потому что
    /// <c>NavMeshAgent</c> в этом модуле принадлежит состояниям, и только им.
    /// Останови его снаружи посреди обхода - <see cref="GuardPatrolState"/> остался бы
    /// в фазе хода с остановленным агентом, никогда бы не «дошёл» до точки и завис бы
    /// молча. Через FSM выход из паузы возвращает обход в <c>Enter</c>, который
    /// сам заново отправляет стражника к цели
    /// </para>
    /// </summary>
    public sealed class GuardStaggerState : IState
    {
        private readonly IGuardStateContext _context;

        public GuardStaggerState(IGuardStateContext context) => _context = context;

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

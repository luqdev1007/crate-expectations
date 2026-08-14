using CrateExpectations.Core.Services;
using CrateExpectations.Core.StateMachine;
using UnityEngine.AI;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Погоня: стражник бежит к игроку, пока тот не окажется в пределах удара.
    /// <para>
    /// Сам по себе выхода не ищет - как и <see cref="GuardPatrolState"/>. Когда бежать
    /// хватит, решает <see cref="GuardBrain"/> по дистанции, а состояние занято одним:
    /// держать агента направленным на цель.
    /// </para>
    /// </summary>
    public sealed class GuardChaseState : IState
    {
        private readonly IGuardStateContext _context;

        private float _repathTimer;

        public GuardChaseState(IGuardStateContext context) => _context = context;

        /// <inheritdoc />
        public void Enter()
        {
            NavMeshAgent agent = _context.Agent;

            agent.speed = _context.Combat.ChaseSpeed;

            if (agent.isOnNavMesh)
                agent.isStopped = false;

            // Первый путь строим немедленно, не выждав интервал: иначе стражник
            // первые 0.2 с погони стоял бы на месте с уже включённой анимацией бега
            Repath();
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            _repathTimer -= deltaTime;

            if (_repathTimer > 0f)
                return;

            Repath();
        }

        /// <summary>
        /// Возвращает агенту патрульный темп. Это не уборка ради уборки: скорость -
        /// свойство АГЕНТА, а не состояния, и оставить на нём чужие 2.78 значит, что
        /// стражник, вернувшийся к обходу, побежит по маршруту бегом. Та же дисциплина,
        /// что у <see cref="GuardPatrolState.Exit"/>, который не бросает агента идущим.
        /// <para>
        /// Значение берётся из ассета, а не кэшируется на входе: кэш вернул бы то,
        /// что стояло на агенте В МОМЕНТ НАЧАЛА погони, а там могло оказаться что угодно -
        /// например, скорость, оставленная предыдущей погоней, прерванной на полпути
        /// </para>
        /// </summary>
        public void Exit()
        {
            NavMeshAgent agent = _context.Agent;

            agent.speed = _context.Movement.PatrolSpeed;

            // Оставляем агента стоящим, а не бегущим в последнюю известную точку:
            // следующее состояние само решит, куда его отправить
            if (agent.isOnNavMesh)
                agent.isStopped = true;
        }

        /// <summary>
        /// Отправить агента к игроку. Не каждый кадр: <c>SetDestination</c> строит путь
        /// по навмешу, и звать его 60 раз в секунду ради цели, сдвинувшейся
        /// на сантиметры, - работа впустую
        /// </summary>
        private void Repath()
        {
            NavMeshAgent agent = _context.Agent;
            IPlayerTarget target = _context.Target;

            // Таймер НЕ взводим: цели или навмеша может не быть секунду, а может -
            // один кадр, и во втором случае ждать ещё интервал незачем. Стоит это
            // двух сравнений в кадр
            if (target == null || !target.Exists || !agent.isOnNavMesh)
                return;

            agent.SetDestination(target.Position);

            _repathTimer = _context.Combat.RepathInterval;
        }
    }
}

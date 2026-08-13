using System.Collections.Generic;
using CrateExpectations.Core.StateMachine;
using UnityEngine;
using UnityEngine.AI;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Обход маршрута: стражник идёт от точки к точке и стоит на каждой несколько секунд,
    /// иногда оглядываясь.
    /// <para>
    /// Ход и ожидание - две ВНУТРЕННИЕ фазы, полем, а не двумя состояниями FSM. Снаружи
    /// это всё один обход: намерение стражника от того, шагает он сейчас или стоит на точке,
    /// не меняется. Разводить их по состояниям машины значило бы вынести подробности
    /// обхода наружу, туда, где они никому не нужны
    /// </para>
    /// </summary>
    public sealed class GuardPatrolState : IState
    {
        private static readonly int LookAroundTriggerId = Animator.StringToHash("LookAroundTrigger");

        private readonly IGuardStateContext _context;

        private int _targetIndex;
        private float _waitTimer;
        private Leg _leg;

        public GuardPatrolState(IGuardStateContext context) => _context = context;

        /// <summary>Чем стражник занят внутри обхода</summary>
        private enum Leg
        {
            /// <summary>Идёт к очередной точке</summary>
            Moving,

            /// <summary>Стоит на точке и пережидает паузу</summary>
            Waiting,
        }

        /// <inheritdoc />
        public void Enter()
        {
            IReadOnlyList<Transform> points = _context.Route != null ? _context.Route.Points : null;
            if (points == null || points.Count == 0)
                return;

            // Ближайшая точка, а не нулевая: стражник, которого поставили в середину
            // маршрута, должен продолжить обход отсюда, а не идти через весь док к началу.
            // Перебор разовый, на входе в состояние, - точек единицы, и в кадре это не живёт
            _targetIndex = NearestPointIndex(points);
            GoToTarget();
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            IReadOnlyList<Transform> points = _context.Route != null ? _context.Route.Points : null;
            if (points == null || points.Count == 0)
                return;

            if (_leg == Leg.Waiting)
            {
                TickWaiting(deltaTime, points);
                return;
            }

            if (HasArrived())
                BeginWaiting();
        }

        /// <inheritdoc />
        public void Exit()
        {
            // Интент может смениться прямо посреди шага (задел на фазу E). Состояние обязано
            // оставить агента в понятном виде, а не бросить его идущим в никуда
            if (_context.Agent.isOnNavMesh)
                _context.Agent.isStopped = true;
        }

        private void TickWaiting(float deltaTime, IReadOnlyList<Transform> points)
        {
            _waitTimer -= deltaTime;
            if (_waitTimer > 0f)
                return;

            // Петля - через ноль; у незамкнутого маршрута стражник встаёт на последней точке.
            // Loop сейчас всегда true, но правило записано здесь, а не подразумевается
            bool isLast = _targetIndex >= points.Count - 1;
            if (isLast && !_context.Route.Loop)
                return;

            _targetIndex = isLast ? 0 : _targetIndex + 1;
            GoToTarget();
        }

        /// <summary>Отправить агента к текущей точке маршрута</summary>
        private void GoToTarget()
        {
            IReadOnlyList<Transform> points = _context.Route.Points;
            Transform point = points[_targetIndex];

            // Точку могли удалить или не назначить - пропускаем и идём к следующей,
            // вместо того чтобы застрять на дыре в сцене
            if (point == null)
            {
                _targetIndex = (_targetIndex + 1) % points.Count;
                point = points[_targetIndex];

                if (point == null)
                    return;
            }

            NavMeshAgent agent = _context.Agent;
            if (!agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.SetDestination(point.position);
            _leg = Leg.Moving;
        }

        /// <summary>
        /// Дошли ли до точки. Одного <c>remainingDistance</c> мало: пока путь не построен,
        /// он равен нулю, и первый же кадр после <c>SetDestination</c> засчитался бы
        /// как приход
        /// </summary>
        private bool HasArrived()
        {
            NavMeshAgent agent = _context.Agent;

            if (!agent.isOnNavMesh || agent.pathPending)
                return false;

            if (agent.remainingDistance > agent.stoppingDistance)
                return false;

            return !agent.hasPath || agent.velocity.sqrMagnitude < ArrivalSpeedThreshold;
        }

        private void BeginWaiting()
        {
            NavMeshAgent agent = _context.Agent;
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            GuardMovementDefinition movement = _context.Movement;
            _waitTimer = Random.Range(movement.PauseDurationMin, movement.PauseDurationMax);

            if (Random.value < movement.LookAroundChance)
                _waitTimer = Mathf.Max(_waitTimer, StartLookAround());

            _leg = Leg.Waiting;
        }

        /// <summary>
        /// Запустить оглядывание и сказать, сколько оно займёт. Длина берётся у самого клипа,
        /// а не числом в коде: пауза короче него оборвала бы жест на середине - стражник
        /// зашагал бы, не докрутив голову
        /// </summary>
        private float StartLookAround()
        {
            AnimationClip clip = _context.Movement.LookAroundClip;
            if (clip == null)
                return 0f;

            _context.Animator.SetTrigger(LookAroundTriggerId);
            return clip.length;
        }

        private int NearestPointIndex(IReadOnlyList<Transform> points)
        {
            Vector3 position = _context.Agent.transform.position;

            int nearest = 0;
            float best = float.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null)
                    continue;

                // Квадрат расстояния: корень здесь ничего не решает, а стоит
                float distance = (points[i].position - position).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                nearest = i;
            }

            return nearest;
        }

        // Порог "стоим" в квадрате скорости: 1 см/с, ниже этого агент уже не едет
        private const float ArrivalSpeedThreshold = 0.0001f;
    }
}

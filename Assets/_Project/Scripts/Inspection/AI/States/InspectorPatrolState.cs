using System.Collections.Generic;
using CrateExpectations.Core.StateMachine;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Обход поста: инспектор прогулочным шагом ходит между своими точками и стоит на
    /// каждой пару секунд. Ничего, кроме вида, это не меняет - досмотр начинается по тому
    /// же условию, что и раньше.
    /// Ожидание здесь обычным таймером, а не <see cref="Pause"/>: состояние синхронное,
    /// отменять нечего, и токен ему не нужен - в отличие от осмотра и вердикта, которые
    /// ждут по-настоящему.
    /// Выход: в зоне появился груз, которого этот инспектор ещё не смотрел
    /// </summary>
    public sealed class InspectorPatrolState : IState
    {
        private readonly IInspectorContext _context;

        // Индекс переживает выход из состояния: вернувшись с досмотра, инспектор
        // продолжает обход, а не начинает круг заново
        private int _next;
        private float _dwell;

        // Кулдаун реакции тоже переживает выход - и именно поэтому его не сбрасывает Enter().
        // Иначе возврат из Notice сразу обнулял бы паузу, и инспектор кивал бы прохожему
        // без остановки, пока тот стоит у поста с ящиком
        private float _noticeCooldown;

        public InspectorPatrolState(IInspectorContext context) => _context = context;

        /// <inheritdoc />
        public void Enter() => _dwell = 0f;

        /// <inheritdoc />
        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

            // Груз в чужих руках - повод обернуться, но досмотр всегда важнее: проверяем
            // это вторым. Сенсор опрашивается только когда пауза вышла, так что сфера
            // физики не считается каждый кадр обхода
            if (_noticeCooldown > 0f)
            {
                _noticeCooldown -= deltaTime;
            }
            else if (_context.ApproachingCargo != null)
            {
                _noticeCooldown = _context.Definition.NoticeCooldownSeconds;
                _context.GoTo(InspectorPhase.Notice);
                return;
            }

            IReadOnlyList<Transform> points = _context.PatrolPoints;
            if (points == null || points.Count == 0) return;

            if (_dwell > 0f)
            {
                _dwell -= deltaTime;
                return;
            }

            _next %= points.Count;
            Transform point = points[_next];

            // Точку могли не назначить или выключить - пропускаем и идём к следующей,
            // вместо того чтобы застрять на дыре в сцене
            if (point == null)
            {
                _next++;
                return;
            }

            if (!_context.Motor.MoveTo(point.position, deltaTime, _context.Definition.PatrolSpeed)) return;

            _dwell = _context.Definition.PatrolDwellSeconds;
            _next++;
        }

        /// <inheritdoc />
        public void Exit()
        {
        }
    }
}

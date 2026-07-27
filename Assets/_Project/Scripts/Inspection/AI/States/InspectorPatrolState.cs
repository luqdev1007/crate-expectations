using System.Collections.Generic;
using CrateExpectations.Core.StateMachine;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorPatrolState : IState
    {
        private readonly IInspectorContext _context;

        private int _next;
        private float _dwell;

        private float _noticeCooldown;

        public InspectorPatrolState(IInspectorContext context) => _context = context;

        public void Enter() => _dwell = 0f;

        public void Tick(float deltaTime)
        {
            if (_context.AwaitsInspection)
            {
                _context.GoTo(InspectorPhase.Approach);
                return;
            }

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

            if (points == null || points.Count == 0) 
                return;

            if (_dwell > 0f)
            {
                _dwell -= deltaTime;
                return;
            }

            _next %= points.Count;
            Transform point = points[_next];

            if (point == null)
            {
                _next++;
                return;
            }

            if (!_context.Motor.MoveTo(point.position, deltaTime, _context.Definition.PatrolSpeed)) 
                return;

            _dwell = _context.Definition.PatrolDwellSeconds;
            _next++;
        }

        public void Exit()
        {
        }
    }
}

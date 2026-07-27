using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    public sealed class InspectorMotor
    {
        private const float FacingToleranceDegrees = 2f;

        private readonly Transform _body;
        private readonly InspectionDefinition _definition;

        public InspectorMotor(Transform body, InspectionDefinition definition)
        {
            _body = body;
            _definition = definition;
        }

        public bool MoveTo(Vector3 target, float deltaTime) =>
            MoveTo(target, deltaTime, _definition.WalkSpeed);

        public bool MoveTo(Vector3 target, float deltaTime, float speed)
        {
            Vector3 position = _body.position;

            target.y = position.y;

            Vector3 offset = target - position;
            float stop = _definition.StopDistance;

            if (offset.sqrMagnitude <= stop * stop) 
                return true;

            _body.position = Vector3.MoveTowards(position, target, speed * deltaTime);
            Face(offset, deltaTime);

            return false;
        }

        public bool FaceTowards(Vector3 point, float deltaTime) =>
            Face(point - _body.position, deltaTime);

        public bool FaceLike(Quaternion rotation, float deltaTime) =>
            Face(rotation * Vector3.forward, deltaTime);

        private bool Face(Vector3 direction, float deltaTime)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f) 
                return true;

            Quaternion target = Quaternion.LookRotation(direction);
            _body.rotation = Quaternion.RotateTowards(
                _body.rotation, target, _definition.TurnSpeed * deltaTime);

            return Quaternion.Angle(_body.rotation, target) <= FacingToleranceDegrees;
        }
    }
}

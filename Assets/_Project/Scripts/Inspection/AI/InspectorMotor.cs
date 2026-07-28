using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Перемещение инспектора: дойти до точки и развернуться. Намеренно примитивно -
    /// NavMesh на прямом пути от поста к столу ничего бы не добавил, а запекание навигации
    /// стоило бы времени, которое лучше потратить на саму машину состояний.
    /// Двигает <see cref="Transform"/>, а не физику: тело инспектора кинематическое,
    /// толкать его некому. Все скорости - из <see cref="InspectionDefinition"/>
    /// </summary>
    public sealed class InspectorMotor
    {
        /// <summary>Ниже этого расхождения считаем, что инспектор уже смотрит куда надо</summary>
        private const float FacingToleranceDegrees = 2f;

        private readonly Transform _body;
        private readonly InspectionDefinition _definition;

        public InspectorMotor(Transform body, InspectionDefinition definition)
        {
            _body = body;
            _definition = definition;
        }

        /// <summary>Шаг к точке маршевым шагом. <c>true</c> - уже пришёл</summary>
        public bool MoveTo(Vector3 target, float deltaTime) =>
            MoveTo(target, deltaTime, _definition.WalkSpeed);

        /// <summary>
        /// Шаг к точке с заданной скоростью; по дороге инспектор смотрит вперёд.
        /// Скорость параметром, а не полем: по делу инспектор идёт маршем, а пост обходит
        /// прогулочным шагом - и то, и другое числа из ассета, а не два разных мотора
        /// </summary>
        /// <returns><c>true</c> - уже пришёл</returns>
        public bool MoveTo(Vector3 target, float deltaTime, float speed)
        {
            Vector3 position = _body.position;

            // Высоту не трогаем: пола инспектор не покидает, а точки назначения
            // расставлены дизайнером на глаз
            target.y = position.y;

            Vector3 offset = target - position;
            float stop = _definition.StopDistance;
            if (offset.sqrMagnitude <= stop * stop) return true;

            _body.position = Vector3.MoveTowards(position, target, speed * deltaTime);
            Face(offset, deltaTime);
            return false;
        }

        /// <summary>Развернуться к точке. <c>true</c> - уже смотрит на неё</summary>
        public bool FaceTowards(Vector3 point, float deltaTime) =>
            Face(point - _body.position, deltaTime);

        /// <summary>Принять заданную ориентацию - поза на посту. <c>true</c> - уже принял</summary>
        public bool FaceLike(Quaternion rotation, float deltaTime) =>
            Face(rotation * Vector3.forward, deltaTime);

        private bool Face(Vector3 direction, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return true;

            Quaternion target = Quaternion.LookRotation(direction);
            _body.rotation = Quaternion.RotateTowards(
                _body.rotation, target, _definition.TurnSpeed * deltaTime);

            return Quaternion.Angle(_body.rotation, target) <= FacingToleranceDegrees;
        }
    }
}

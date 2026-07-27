using System;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    [Serializable]
    public struct ExamineStep
    {
        [Tooltip("Что инспектор проверяет. Шаг пропускается, если профиль такой проверки не делает")]
        public InspectionAspect Aspect;

        [Tooltip("Точка внимания: смещение от центра ящика в его локальных осях")]
        public Vector3 FocusOffset;

        [Tooltip("Длительность шага")]
        public float Seconds;
    }
}

using System;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Один шаг осмотра: что проверяем и сколько это длится.
    /// Порядок и состав шагов - дизайнерское решение, поэтому они живут массивом
    /// в <see cref="InspectionDefinition"/>, а не ветвлением в коде состояния
    /// </summary>
    [Serializable]
    public struct ExamineStep
    {
        [Tooltip("Что инспектор проверяет. Шаг пропускается, если профиль такой проверки не делает")]
        public InspectionAspect Aspect;

        [Tooltip("Длительность шага, с")]
        public float Seconds;
    }
}

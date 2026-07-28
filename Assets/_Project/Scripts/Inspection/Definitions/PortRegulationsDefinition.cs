using System;
using CrateExpectations.Cargo;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Правила порта: как должен выглядеть ящик с тем или иным заявленным содержимым.
    /// Именно отсюда берётся смысл улик "не та окраска" и "не та пломба" - без регламента
    /// сравнивать было бы не с чем. Регламент принадлежит порту, а не грузу,
    /// поэтому живёт в <c>Inspection</c> и не тянет за собой правок в <c>Cargo</c>
    /// </summary>
    [CreateAssetMenu(
        fileName = "PortRegulations",
        menuName = "CrateExpectations/Inspection/Port Regulations")]
    public sealed class PortRegulationsDefinition : ScriptableObject
    {
        /// <summary>Требования к одному типу груза</summary>
        [Serializable]
        public struct Requirement
        {
            [Tooltip("Заявленное содержимое")]
            public CargoTypeDefinition CargoType;

            [Tooltip("Положенная окраска. Пусто - окраска не регламентирована")]
            public PaintDefinition Paint;

            [Tooltip("Положенная пломба. Пусто - пломба не требуется")]
            public StampDefinition Stamp;
        }

        [Tooltip("Регламент по типам груза")]
        [SerializeField] private Requirement[] _requirements = Array.Empty<Requirement>();

        /// <summary>
        /// Требования к заявленному содержимому. Список короткий, поэтому обходится линейно -
        /// без словаря и без аллокаций
        /// </summary>
        public bool TryGetRequirement(CargoTypeDefinition cargoType, out Requirement requirement)
        {
            for (int i = 0; i < _requirements.Length; i++)
            {
                if (_requirements[i].CargoType != cargoType) continue;

                requirement = _requirements[i];
                return true;
            }

            requirement = default;
            return false;
        }

        /// <summary>
        /// Собрать то, что увидит инспектор: состояние груза плюс требования к заявленному
        /// содержимому. Груза, которого нет в регламенте, требования не касаются
        /// </summary>
        public InspectionSubject CreateSubject(in CargoState declared, in CargoIdentity truth)
        {
            return TryGetRequirement(declared.DeclaredType, out Requirement requirement)
                ? new InspectionSubject(declared, truth, requirement.Paint, requirement.Stamp)
                : new InspectionSubject(declared, truth);
        }
    }
}

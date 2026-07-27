using System;
using CrateExpectations.Cargo;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    [CreateAssetMenu(
        fileName = "PortRegulations",
        menuName = "CrateExpectations/Inspection/Port Regulations")]
    public sealed class PortRegulationsDefinition : ScriptableObject
    {
        [Serializable]
        public struct Requirement
        {
            public CargoTypeDefinition CargoType;
            public PaintDefinition Paint;
            public StampDefinition Stamp;
        }

        [SerializeField] private Requirement[] _requirements = Array.Empty<Requirement>();

        public bool TryGetRequirement(CargoTypeDefinition cargoType, out Requirement requirement)
        {
            for (int i = 0; i < _requirements.Length; i++)
            {
                if (_requirements[i].CargoType != cargoType) 
                    continue;

                requirement = _requirements[i];
                return true;
            }

            requirement = default;

            return false;
        }

        public InspectionSubject CreateSubject(in CargoState declared, in CargoIdentity truth)
        {
            return TryGetRequirement(declared.DeclaredType, out Requirement requirement)
                ? new InspectionSubject(declared, truth, requirement.Paint, requirement.Stamp)
                : new InspectionSubject(declared, truth);
        }
    }
}

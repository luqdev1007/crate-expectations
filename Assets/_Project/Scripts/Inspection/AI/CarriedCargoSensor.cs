using CrateExpectations.Cargo;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    internal sealed class CarriedCargoSensor
    {
        private const int Capacity = 16;

        private readonly Collider[] _hits = new Collider[Capacity];
        private readonly Transform _eyes;
        private readonly InspectionDefinition _definition;

        internal CarriedCargoSensor(Transform eyes, InspectionDefinition definition)
        {
            _eyes = eyes;
            _definition = definition;
        }

        internal CargoBox FindNearest()
        {
            float radius = _definition.NoticeRadius;

            if (radius <= 0f) 
                return null;

            Vector3 origin = _eyes.position;

            int count = Physics.OverlapSphereNonAlloc(
                origin, radius, _hits, _definition.CargoMask, QueryTriggerInteraction.Ignore);

            CargoBox nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                CargoBox box = Resolve(_hits[i]);

                if (box == null || !box.IsCarried) 
                    continue;

                float distance = (box.transform.position - origin).sqrMagnitude;

                if (distance >= nearestDistance)
                    continue;

                nearest = box;
                nearestDistance = distance;
            }

            return nearest;
        }

        private static CargoBox Resolve(Collider hit) =>
            hit.TryGetComponent(out CargoBox box) ? box : hit.GetComponentInParent<CargoBox>();
    }
}

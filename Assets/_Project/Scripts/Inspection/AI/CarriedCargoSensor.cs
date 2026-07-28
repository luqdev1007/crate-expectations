using CrateExpectations.Cargo;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Глаза инспектора, отвечающие на один вопрос: не несёт ли кто-то мимо груз в руках.
    /// Спрашивает физику, а не игрока - модуль досмотра про игрока не знает и знать не должен,
    /// а ящик в руках всё равно ходит вместе с ним, так что искать носильщика отдельно незачем.
    /// POCO, как и <see cref="InspectorMotor"/>: настраивать в сцене тут нечего, радиус и слои
    /// живут в <see cref="InspectionDefinition"/>
    /// </summary>
    internal sealed class CarriedCargoSensor
    {
        // Столько коллайдеров груза разом у поста не окажется. Буфер один на весь срок жизни:
        // сенсор опрашивают из Tick, и аллокации на кадр тут недопустимы
        private const int Capacity = 16;

        private readonly Collider[] _hits = new Collider[Capacity];
        private readonly Transform _eyes;
        private readonly InspectionDefinition _definition;

        internal CarriedCargoSensor(Transform eyes, InspectionDefinition definition)
        {
            _eyes = eyes;
            _definition = definition;
        }

        /// <summary>
        /// Ближайший ящик, который прямо сейчас держат в руках рядом с инспектором,
        /// или <c>null</c>. Поставленный груз не считается: им занимается зона досмотра,
        /// и путать эти два повода незачем - на один инспектор оборачивается, ко второму идёт
        /// </summary>
        internal CargoBox FindNearest()
        {
            float radius = _definition.NoticeRadius;
            if (radius <= 0f) return null;

            Vector3 origin = _eyes.position;

            // Триггеры пропускаем: зоны размещения и подсветки - не груз, а разметка стола
            int count = Physics.OverlapSphereNonAlloc(
                origin, radius, _hits, _definition.CargoMask, QueryTriggerInteraction.Ignore);

            CargoBox nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                CargoBox box = Resolve(_hits[i]);
                if (box == null || !box.IsCarried) continue;

                float distance = (box.transform.position - origin).sqrMagnitude;
                if (distance >= nearestDistance) continue;

                nearest = box;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>Коллайдер может висеть и на самом ящике, и на его меше-ребёнке</summary>
        private static CargoBox Resolve(Collider hit) =>
            hit.TryGetComponent(out CargoBox box) ? box : hit.GetComponentInParent<CargoBox>();
    }
}

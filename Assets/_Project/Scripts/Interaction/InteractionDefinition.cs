using UnityEngine;

namespace CrateExpectations.Interaction
{
    [CreateAssetMenu(
        fileName = "InteractionDefinition",
        menuName = "CrateExpectations/Interaction/Interaction Definition")]
    public sealed class InteractionDefinition : ScriptableObject
    {
        [Header("Взаимодействие")]
        [Tooltip("Дальность, с которой срабатывает IInteractable")]
        [field: SerializeField] public float MaxDistance { get; private set; } = 3f;

        [Tooltip("Слои, на которых лежат интерактивные объекты (обычно 'Interactable')")]
        [field: SerializeField] public LayerMask InteractableMask { get; private set; } = ~0;

        [Header("Взгляд")]
        [Tooltip("Дальность, с которой Interactor сообщает о предмете под прицелом")]
        [field: SerializeField] public float FocusDistance { get; private set; } = 4f;

        [Tooltip("Слои предметов, о которых Interactor сообщает как о цели под прицелом " +
                 "(обычно 'Carriable' и 'Carried'). Взаимодействие они не перехватывают")]
        [field: SerializeField] public LayerMask FocusMask { get; private set; }

        /// <summary>Дальность одного общего луча: покрывает и взаимодействие, и взгляд.</summary>
        public float ScanDistance => Mathf.Max(MaxDistance, FocusDistance);

        /// <summary>Маска одного общего луча: покрывает и взаимодействие, и взгляд.</summary>
        public int ScanMask => InteractableMask.value | FocusMask.value;
    }
}

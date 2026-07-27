using UnityEngine;

namespace CrateExpectations.Interaction
{
    [CreateAssetMenu(
        fileName = "InteractionDefinition",
        menuName = "CrateExpectations/Interaction/Interaction Definition")]
    public sealed class InteractionDefinition : ScriptableObject
    {
        [field: SerializeField] public float MaxDistance { get; private set; } = 3f;
        [field: SerializeField] public LayerMask InteractableMask { get; private set; } = ~0;
    }
}

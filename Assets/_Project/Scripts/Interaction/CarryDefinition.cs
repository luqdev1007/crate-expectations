using UnityEngine;

namespace CrateExpectations.Interaction
{
    public enum HoldMode
    {
        VelocityHold,
        ConfigurableJoint,
    }

    [CreateAssetMenu(
        fileName = "CarryDefinition",
        menuName = "CrateExpectations/Interaction/Carry Definition")]
    public sealed class CarryDefinition : ScriptableObject
    {
        [field: SerializeField] public float HoldDistance { get; private set; } = 2f;
        [field: SerializeField] public float MinHoldDistance { get; private set; } = 0.6f;
        [field: SerializeField] public LayerMask HoldBlockingMask { get; private set; } = ~0;
        [field: SerializeField] public float GrabDistance { get; private set; } = 3f;
        [field: SerializeField] public float BreakDistance { get; private set; } = 3f;
        [field: SerializeField] public float FollowSpeed { get; private set; } = 12f;
        [field: SerializeField] public float MaxVelocity { get; private set; } = 15f;
        [field: SerializeField] public float ThrowForce { get; private set; } = 8f;
        [field: SerializeField] public float CarriedAngularDamping { get; private set; } = 8f;
        [field: SerializeField] public HoldMode HoldMode { get; private set; } = HoldMode.VelocityHold;
        [field: SerializeField] public float JointSpring { get; private set; } = 1000f;
        [field: SerializeField] public float JointDamper { get; private set; } = 50f;
        [field: SerializeField] public LayerMask CarriableMask { get; private set; } = ~0;
        [field: SerializeField] public int CarriedLayer { get; private set; }
    }
}

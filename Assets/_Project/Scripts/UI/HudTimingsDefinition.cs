using UnityEngine;

namespace CrateExpectations.UI
{
    [CreateAssetMenu(
        fileName = "HudTimings",
        menuName = "CrateExpectations/UI/HUD Timings")]
    public sealed class HudTimingsDefinition : ScriptableObject
    {        
        [field: SerializeField][Min(0.5f)] public float ContractOutcomeSeconds { get; private set; } = 4f; 
        [field: SerializeField][Min(0.5f)] public float SaveStatusSeconds { get; private set; } = 3.5f;  
        [field: SerializeField][Min(0.1f)] public float BalanceFlashSeconds { get; private set; } = 2.5f;
    }
}

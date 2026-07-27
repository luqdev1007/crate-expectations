using UnityEngine;

namespace CrateExpectations.Persistence
{
    [CreateAssetMenu(
        fileName = "SaveSlot",
        menuName = "CrateExpectations/Persistence/Save Slot")]
    public sealed class SaveSlotDefinition : ScriptableObject
    {
        [field: SerializeField] public string Key { get; private set; } = "crate-expectations-save";
        [field: SerializeField] public string DisplayName { get; private set; } = "Быстрое сохранение";
    }
}

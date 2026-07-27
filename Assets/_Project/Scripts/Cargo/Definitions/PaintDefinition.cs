using UnityEngine;

namespace CrateExpectations.Cargo
{
    [CreateAssetMenu(
        fileName = "PaintDefinition",
        menuName = "CrateExpectations/Cargo/Paint")]
    public sealed class PaintDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя цвета: подставляется в подсказку станции")]
        [field: SerializeField] public string DisplayName { get; private set; } = "краска";

        [Tooltip("Цвет корпуса ящика после покраски")]
        [field: SerializeField] public Color Color { get; private set; } = Color.white;
    }
}

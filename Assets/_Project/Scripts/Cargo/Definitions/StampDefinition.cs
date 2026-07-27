using UnityEngine;

namespace CrateExpectations.Cargo
{
    [CreateAssetMenu(
        fileName = "StampDefinition",
        menuName = "CrateExpectations/Cargo/Stamp")]
    public sealed class StampDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя печати (подставляется в подсказку станци)")]
        [field: SerializeField] public string DisplayName { get; private set; } = "печать";

        [Tooltip("Цвет декали")]
        [field: SerializeField] public Color Color { get; private set; } = Color.white;

        [Tooltip("Текстура декали (если пусто декаль остаётся однотонной)")]
        [field: SerializeField] public Texture2D Texture { get; private set; }
    }
}

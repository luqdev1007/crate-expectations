using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>Вид печати-пломбы: как выглядит декаль, которую станция ставит на грань ящика</summary>
    [CreateAssetMenu(
        fileName = "StampDefinition",
        menuName = "CrateExpectations/Cargo/Stamp")]
    public sealed class StampDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя печати: подставляется в подсказку станции")]
        [field: SerializeField] public string DisplayName { get; private set; } = "печать";

        [field: SerializeField] public Color Color { get; private set; } = Color.white;

        [Tooltip("Текстура декали. Пусто - декаль остаётся однотонной (grey-box)")]
        [field: SerializeField] public Texture2D Texture { get; private set; }
    }
}

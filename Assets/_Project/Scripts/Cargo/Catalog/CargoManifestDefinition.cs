using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    /// <summary>
    /// Что за груз стоит на доке в начале смены. Ключи контента живут здесь, а не строками по коду
    /// </summary>
    [CreateAssetMenu(
        fileName = "CargoManifestDefinition",
        menuName = "CrateExpectations/Cargo/Cargo Manifest")]
    public sealed class CargoManifestDefinition : ScriptableObject
    {
        [Tooltip("Addressables-ключи типов груза. Раскладываются по точкам спавна по порядку")]
        [field: SerializeField] public string[] CargoKeys { get; private set; } = new string[0];
    }
}

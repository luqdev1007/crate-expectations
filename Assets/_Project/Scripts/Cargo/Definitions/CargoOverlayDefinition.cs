using UnityEngine;

namespace CrateExpectations.Cargo
{
    [CreateAssetMenu(
        fileName = "CargoOverlayDefinition",
        menuName = "CrateExpectations/Cargo/Cargo Overlay")]
    public sealed class CargoOverlayDefinition : ScriptableObject
    {
        [Tooltip("Показывать оверлей. Если выключено: текст скрыт, лучи не пускаются")]
        [field: SerializeField] public bool Enabled { get; private set; } = true;

        [Tooltip("Дальность луча, которым оверлей ищет ящик под прицелом")]
        [field: SerializeField] public float MaxDistance { get; private set; } = 4f;

        [Tooltip("Слои, на которых лежит груз (обычно 'Carriable')")]
        [field: SerializeField] public LayerMask CargoMask { get; private set; } = ~0;

        [Header("Тексты")]
        [Tooltip("Шаблон: {0} истина, {1} окраска, {2} печать, {3} заявлено, {4} расхождение")]
        [field: SerializeField, TextArea(3, 8)] public string Format { get; private set; } =
            "<b>Истина:</b> {0}\n<b>Окраска:</b> {1}\n<b>Печать:</b> {2}\n<b>Заявлено:</b> {3}\n<b>Расхождение:</b> {4}";

        [Tooltip("Значение пустого поля")]
        [field: SerializeField] public string NoneLabel { get; private set; } = "-";

        [Tooltip("Заявленное разошлось с истиной")]
        [field: SerializeField] public string DivergedLabel { get; private set; } = "да";

        [Tooltip("Заявленное совпадает с истиной")]
        [field: SerializeField] public string MatchesLabel { get; private set; } = "нет";
    }
}

using UnityEngine;

namespace CrateExpectations.Cargo
{
    [CreateAssetMenu(
        fileName = "CargoTypeDefinition",
        menuName = "CrateExpectations/Cargo/Cargo Type")]
    public sealed class CargoTypeDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя: попадает в подсказки и в реплики инспектора")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Груз";

        [Tooltip("Контрабанда: за такой груз в порту прилетит штраф, если инспектор его узнает")]
        [field: SerializeField] public bool IsContraband { get; private set; }

        [Tooltip("Цвет метки содержимого на ящике - визуальный отклик на 'перелив'")]
        [field: SerializeField] public Color LabelColor { get; private set; } = Color.white;

        [Tooltip("Базовая стоимость единицы груза")]
        [field: SerializeField] public int BaseValue { get; private set; } = 100;

        [Tooltip("Addressables-ключ префаб варианта ящика этого типа")]
        [field: SerializeField] public string PrefabKey { get; private set; } = string.Empty;

        [Tooltip("Заводская окраска ящика этого типа: как он выглядит до первой станции")]
        [field: SerializeField] public PaintDefinition FactoryPaint { get; private set; }
    }
}

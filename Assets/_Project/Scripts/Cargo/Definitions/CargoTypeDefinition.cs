using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Тип груза. Один и тот же ассет играет две роли: он может быть истиной
    /// (<see cref="CargoIdentity"/>) и может быть заявленным содержимым (<see cref="CargoState"/>) -
    /// именно поэтому "ром, притворяющийся специями" описывается двумя ссылками на такие ассеты
    /// </summary>
    [CreateAssetMenu(
        fileName = "CargoTypeDefinition",
        menuName = "CrateExpectations/Cargo/Cargo Type")]
    public sealed class CargoTypeDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя: попадает в подсказки и в реплики инспектора")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Груз";

        [Tooltip("Контрабанда: за такой груз в порту прилетит штраф, если инспектор его узнает")]
        [field: SerializeField] public bool IsContraband { get; private set; }

        [Tooltip("Иконка товара на грани ящика - та же картинка, что и на листке заказа: " +
                 "игрок узнаёт содержимое по одному значку и на доске, и на самом ящике. " +
                 "Меняется при \"переливе\" вместе с заявленным типом")]
        [field: SerializeField] public Texture2D Icon { get; private set; }

        [Tooltip("Базовая стоимость единицы груза")]
        [field: SerializeField] public int BaseValue { get; private set; } = 100;

        [Tooltip("Addressables-ключ префаба-варианта ящика этого типа. " +
                 "Ключи живут здесь, а не строками по коду.")]
        [field: SerializeField] public string PrefabKey { get; private set; } = string.Empty;

        [Tooltip("Заводская окраска ящика этого типа: как он выглядит до первой станции")]
        [field: SerializeField] public PaintDefinition FactoryPaint { get; private set; }
    }
}

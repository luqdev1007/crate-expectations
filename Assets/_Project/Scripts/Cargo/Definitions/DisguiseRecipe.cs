using UnityEngine;

namespace CrateExpectations.Cargo
{
    public enum DisguiseAction
    {
        Paint,
        Stamp,
        Pour,
    }

    [CreateAssetMenu(
        fileName = "DisguiseRecipe",
        menuName = "CrateExpectations/Cargo/Disguise Recipe")]
    public sealed class DisguiseRecipe : ScriptableObject
    {
        [Tooltip("Тип действия (решает, какое из полей ниже обязательно)")]
        [field: SerializeField] public DisguiseAction Action { get; private set; }

        [Tooltip("Краска для действия Paint")]
        [field: SerializeField] public PaintDefinition Paint { get; private set; }

        [Tooltip("Печать для действия Stamp")]
        [field: SerializeField] public StampDefinition Stamp { get; private set; }

        [Tooltip("Заявленное содержимое для действия Pour")]
        [field: SerializeField] public CargoTypeDefinition DeclaredType { get; private set; }

        [Tooltip("Условие применимости: ящик уже должен быть покрашен этой краской, если пусто,то условия нет")]
        [field: SerializeField] public PaintDefinition RequiredPaint { get; private set; }

        [Tooltip("Название действия для подсказки, например 'Покрасить в'")]
        [field: SerializeField] public string ActionLabel { get; private set; } = "Применить";

        private string _description;

        public DisguiseOperation Operation =>
            new(Action, Paint, Stamp, DeclaredType, RequiredPaint);

        public string Description => _description ??= $"{ActionLabel} {TargetName}";

        private string TargetName => Action switch
        {
            DisguiseAction.Paint => Paint != null ? Paint.DisplayName : string.Empty,
            DisguiseAction.Stamp => Stamp != null ? Stamp.DisplayName : string.Empty,
            DisguiseAction.Pour => DeclaredType != null ? DeclaredType.DisplayName : string.Empty,
            _ => string.Empty,
        };

        private void OnValidate() => _description = null;
    }
}

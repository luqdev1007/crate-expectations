using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>Что именно делает действие маскировки</summary>
    public enum DisguiseAction
    {
        /// <summary>Перекрасить корпус - меняет <see cref="CargoState.Paint"/></summary>
        Paint,

        /// <summary>Поставить пломбу - меняет <see cref="CargoState.Stamp"/></summary>
        Stamp,

        /// <summary>Перелить содержимое - меняет <see cref="CargoState.DeclaredType"/></summary>
        Pour,
    }

    /// <summary>
    /// Рецепт маскировки: авторская запись одного действия станции. Дизайнер собирает станции
    /// из этих ассетов, не трогая код. Для расчёта рецепт отдаёт неизменяемую
    /// <see cref="DisguiseOperation"/> - так ядро логики не зависит от ScriptableObject-обвязки.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DisguiseRecipe",
        menuName = "CrateExpectations/Cargo/Disguise Recipe")]
    public sealed class DisguiseRecipe : ScriptableObject
    {
        [Tooltip("Тип действия. Он же решает, какое из полей ниже обязательно")]
        [field: SerializeField] public DisguiseAction Action { get; private set; }

        [Tooltip("Краска для действия Paint")]
        [field: SerializeField] public PaintDefinition Paint { get; private set; }

        [Tooltip("Печать для действия Stamp")]
        [field: SerializeField] public StampDefinition Stamp { get; private set; }

        [Tooltip("Заявленное содержимое для действия Pour. Истину не меняет никогда")]
        [field: SerializeField] public CargoTypeDefinition DeclaredType { get; private set; }

        [Tooltip("Условие применимости: ящик уже должен быть покрашен этой краской. " +
                 "Пусто - условия нет.")]
        [field: SerializeField] public PaintDefinition RequiredPaint { get; private set; }

        [Tooltip("Глагол действия для подсказки, например \"Покрасить в\"")]
        [field: SerializeField] public string ActionLabel { get; private set; } = "Применить";

        // Описание строится один раз и кэшируется: подсказка станции опрашивается каждый кадр
        private string _description;

        /// <summary>Данные рецепта в виде значения - то, с чем работает <see cref="DisguiseProcessor"/></summary>
        public DisguiseOperation Operation =>
            new(Action, Paint, Stamp, DeclaredType, RequiredPaint);

        /// <summary>Человекочитаемое описание действия: "Покрасить в синий"</summary>
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

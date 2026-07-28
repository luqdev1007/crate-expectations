namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Рецепт маскировки в виде значения: ровно те данные, которые нужны расчёту.
    /// Отделён от <see cref="DisguiseRecipe"/>, чтобы ядро логики не зависело от
    /// ScriptableObject-обвязки, а тесты собирали операции напрямую, без ассетов
    /// </summary>
    public readonly struct DisguiseOperation
    {
        public DisguiseOperation(
            DisguiseAction action,
            PaintDefinition paint = null,
            StampDefinition stamp = null,
            CargoTypeDefinition declaredType = null,
            PaintDefinition requiredPaint = null)
        {
            Action = action;
            Paint = paint;
            Stamp = stamp;
            DeclaredType = declaredType;
            RequiredPaint = requiredPaint;
        }

        /// <summary>Что делаем</summary>
        public DisguiseAction Action { get; }

        /// <summary>Краска для <see cref="DisguiseAction.Paint"/></summary>
        public PaintDefinition Paint { get; }

        /// <summary>Печать для <see cref="DisguiseAction.Stamp"/></summary>
        public StampDefinition Stamp { get; }

        /// <summary>Новое заявленное содержимое для <see cref="DisguiseAction.Pour"/></summary>
        public CargoTypeDefinition DeclaredType { get; }

        /// <summary>Условие: ящик уже должен быть покрашен этой краской. <c>null</c> - условия нет</summary>
        public PaintDefinition RequiredPaint { get; }
    }
}

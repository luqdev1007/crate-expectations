namespace CrateExpectations.Cargo
{
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

        public DisguiseAction Action { get; }

        public PaintDefinition Paint { get; }

        public StampDefinition Stamp { get; }

        public CargoTypeDefinition DeclaredType { get; }

        public PaintDefinition RequiredPaint { get; }
    }
}

using System;

namespace CrateExpectations.Cargo
{
    public readonly struct CargoState : IEquatable<CargoState>
    {
        public CargoState(
            PaintDefinition paint,
            StampDefinition stamp,
            CargoTypeDefinition declaredType)
        {
            Paint = paint;
            Stamp = stamp;
            DeclaredType = declaredType;
        }

        public PaintDefinition Paint { get; }

        public StampDefinition Stamp { get; }

        public CargoTypeDefinition DeclaredType { get; }

        public static CargoState Undisguised(in CargoIdentity identity, PaintDefinition factoryPaint = null) =>
            new(factoryPaint, null, identity.TrueType);

        public CargoState WithPaint(PaintDefinition paint) => new(paint, Stamp, DeclaredType);

        public CargoState WithStamp(StampDefinition stamp) => new(Paint, stamp, DeclaredType);

        public CargoState WithDeclaredType(CargoTypeDefinition declaredType) =>
            new(Paint, Stamp, declaredType);

        public bool MatchesTruth(in CargoIdentity identity) => DeclaredType == identity.TrueType;

        public bool Equals(CargoState other) =>
            Paint == other.Paint && Stamp == other.Stamp && DeclaredType == other.DeclaredType;

        public override bool Equals(object obj) => obj is CargoState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Paint, Stamp, DeclaredType);

        public override string ToString() =>
            $"заявлено: {(DeclaredType != null ? DeclaredType.DisplayName : "-")}, " +
            $"окраска: {(Paint != null ? Paint.DisplayName : "-")}, " +
            $"печать: {(Stamp != null ? Stamp.DisplayName : "-")}";
    }
}

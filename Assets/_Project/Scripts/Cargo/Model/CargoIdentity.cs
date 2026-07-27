using System;

namespace CrateExpectations.Cargo
{
    public readonly struct CargoIdentity : IEquatable<CargoIdentity>
    {
        public CargoIdentity(CargoTypeDefinition trueType) => TrueType = trueType;

        public CargoTypeDefinition TrueType { get; }

        public bool IsContraband => TrueType != null && TrueType.IsContraband;

        public bool Equals(CargoIdentity other) => TrueType == other.TrueType;

        public override bool Equals(object obj) => obj is CargoIdentity other && Equals(other);

        public override int GetHashCode() => TrueType != null ? TrueType.GetHashCode() : 0;

        public override string ToString() =>
            TrueType != null ? TrueType.DisplayName : "<неизвестный груз>";
    }
}

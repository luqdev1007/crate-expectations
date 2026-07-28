using System;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Истинная личность груза: что лежит в ящике на самом деле. Задаётся при создании ящика
    /// и не меняется никогда - тип неизменяемый (readonly struct), поэтому ни станция,
    /// ни <see cref="DisguiseProcessor"/> физически не могут его переписать
    /// </summary>
    public readonly struct CargoIdentity : IEquatable<CargoIdentity>
    {
        public CargoIdentity(CargoTypeDefinition trueType) => TrueType = trueType;

        /// <summary>Что внутри на самом деле</summary>
        public CargoTypeDefinition TrueType { get; }

        /// <summary>Груз запрещён к перевозке - за него в порту прилетит штраф</summary>
        public bool IsContraband => TrueType != null && TrueType.IsContraband;

        public bool Equals(CargoIdentity other) => TrueType == other.TrueType;

        public override bool Equals(object obj) => obj is CargoIdentity other && Equals(other);

        public override int GetHashCode() => TrueType != null ? TrueType.GetHashCode() : 0;

        public override string ToString() =>
            TrueType != null ? TrueType.DisplayName : "<неизвестный груз>";
    }
}

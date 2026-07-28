using System;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Заявленное состояние груза - всё, что видно снаружи и что оценит инспектор.
    /// Значение неизменяемое: любое действие маскировки порождает новое состояние, а не правит
    /// старое. Благодаря этому <see cref="DisguiseProcessor"/> чист, а "примерить" рецепт
    /// можно без побочных эффектов
    /// </summary>
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

        /// <summary>Чем покрашен корпус. <c>null</c> - заводской вид</summary>
        public PaintDefinition Paint { get; }

        /// <summary>Какая пломба стоит. <c>null</c> - печати нет</summary>
        public StampDefinition Stamp { get; }

        /// <summary>Чем груз притворяется. Совпадает с истиной, пока его не переливали</summary>
        public CargoTypeDefinition DeclaredType { get; }

        /// <summary>Состояние только что созданного ящика: заявлено ровно то, что внутри</summary>
        public static CargoState Undisguised(in CargoIdentity identity, PaintDefinition factoryPaint = null) =>
            new(factoryPaint, null, identity.TrueType);

        public CargoState WithPaint(PaintDefinition paint) => new(paint, Stamp, DeclaredType);

        public CargoState WithStamp(StampDefinition stamp) => new(Paint, stamp, DeclaredType);

        public CargoState WithDeclaredType(CargoTypeDefinition declaredType) =>
            new(Paint, Stamp, declaredType);

        /// <summary>
        /// Заявленное содержимое совпадает с истинным. Расхождение - это будущая улика,
        /// которую в Фазе 3 будет искать инспектор
        /// </summary>
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

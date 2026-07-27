using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    public enum CargoStanding
    {
        OnDock,
        Delivered,
        Seized,
    }

    public readonly struct CargoRecord
    {
        public CargoRecord(
            int id,
            in CargoIdentity truth,
            in CargoState declared,
            CargoStanding standing = CargoStanding.OnDock)
        {
            Id = id;
            Truth = truth;
            Declared = declared;
            Standing = standing;
        }

        public int Id { get; }

        public CargoIdentity Truth { get; }

        public CargoState Declared { get; }

        public CargoStanding Standing { get; }

        public bool IsOnDock => Standing == CargoStanding.OnDock;

        public bool IsDisguised => !Declared.MatchesTruth(Truth);

        public CargoRecord Redeclared(in CargoState declared) =>
            new(Id, Truth, declared, Standing);

        public CargoRecord Settled(CargoStanding standing) =>
            new(Id, Truth, Declared, standing);

        public override string ToString() => $"#{Id} {Truth} ({Standing}): {Declared}";
    }
}

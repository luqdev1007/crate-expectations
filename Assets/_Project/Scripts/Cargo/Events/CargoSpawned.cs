namespace CrateExpectations.Cargo.Events
{
    public readonly struct CargoSpawned
    {
        public CargoSpawned(CargoBox box) => Box = box;
        public CargoBox Box { get; }
    }
}

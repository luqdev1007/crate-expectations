namespace CrateExpectations.Cargo.Events
{
    /// <summary>
    /// На доке появился ящик. Нужен тем, кто ведёт учёт груза: сканировать сцену в поисках
    /// новых ящиков - значит каждый кадр спрашивать физику о том, что и так известно
    /// в момент создания
    /// </summary>
    public readonly struct CargoSpawned
    {
        public CargoSpawned(CargoBox box) => Box = box;

        /// <summary>Появившийся ящик</summary>
        public CargoBox Box { get; }
    }
}

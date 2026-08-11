namespace CrateExpectations.Combat
{
    /// <summary>
    /// Тайминги оружия в виде значения: отдельно от ассета, чтобы
    /// <see cref="WeaponStateMachine"/> оставался обычным C#-классом и заводился в тесте
    /// одной строкой, без сцены и без ScriptableObject.
    /// <para>
    /// Длительности удара здесь нет намеренно: она перестала быть свойством оружия.
    /// У каждого приёма своя (<see cref="AttackDefinition.Duration"/>), и машина
    /// получает её аргументом на входе в удар, а не хранит одну на всех.
    /// </para>
    /// </summary>
    public readonly struct WeaponTimings
    {
        public WeaponTimings(
            float drawDuration, float sheatheDuration,
            float blockRaiseDuration = 0.2f, float blockLowerDuration = 0.2f,
            float drawRevealFraction = 0.5f, float sheatheHideFraction = 0.5f)
        {
            DrawDuration = drawDuration;
            SheatheDuration = sheatheDuration;
            BlockRaiseDuration = blockRaiseDuration;
            BlockLowerDuration = blockLowerDuration;
            DrawRevealFraction = drawRevealFraction;
            SheatheHideFraction = sheatheHideFraction;
        }

        /// <summary>Сколько длится доставание, с</summary>
        public float DrawDuration { get; }

        /// <summary>Сколько длится убирание, с</summary>
        public float SheatheDuration { get; }

        /// <summary>Сколько длится подъём клинка в блок, с</summary>
        public float BlockRaiseDuration { get; }

        /// <summary>Сколько длится опускание клинка из блока, с</summary>
        public float BlockLowerDuration { get; }

        /// <summary>
        /// На какой доле доставания клинок появляется в руке, 0..1. Доля, а не секунды:
        /// момент должен ехать вместе с длительностью фазы, а не отставать на пересчёт
        /// </summary>
        public float DrawRevealFraction { get; }

        /// <summary>На какой доле убирания клинок исчезает из руки, 0..1</summary>
        public float SheatheHideFraction { get; }
    }
}

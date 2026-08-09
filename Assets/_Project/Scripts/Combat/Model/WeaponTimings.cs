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
        public WeaponTimings(float drawDuration, float sheatheDuration)
        {
            DrawDuration = drawDuration;
            SheatheDuration = sheatheDuration;
        }

        /// <summary>Сколько длится доставание, с</summary>
        public float DrawDuration { get; }

        /// <summary>Сколько длится убирание, с</summary>
        public float SheatheDuration { get; }
    }
}

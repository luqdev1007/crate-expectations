namespace CrateExpectations.Combat
{
    /// <summary>
    /// Тайминги оружия в виде значения: отдельно от ассета, чтобы
    /// <see cref="WeaponStateMachine"/> оставался обычным C#-классом и заводился в тесте
    /// одной строкой, без сцены и без ScriptableObject
    /// </summary>
    public readonly struct WeaponTimings
    {
        public WeaponTimings(float drawDuration, float sheatheDuration, float attackDuration)
        {
            DrawDuration = drawDuration;
            SheatheDuration = sheatheDuration;
            AttackDuration = attackDuration;
        }

        /// <summary>Сколько длится доставание, с</summary>
        public float DrawDuration { get; }

        /// <summary>Сколько длится убирание, с</summary>
        public float SheatheDuration { get; }

        /// <summary>Сколько длится взмах, с. Анимация подгоняется под это число, а не наоборот</summary>
        public float AttackDuration { get; }
    }
}

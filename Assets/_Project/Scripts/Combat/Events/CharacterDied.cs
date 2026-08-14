namespace CrateExpectations.Combat.Events
{
    /// <summary>
    /// Кто-то умер. Публикуется РОВНО ОДИН раз за жизнь - за это отвечает
    /// <see cref="HealthState"/>, который по мёртвому не бьёт и второй раз о смерти
    /// не объявляет. Подписчику поэтому не нужно защищаться от повтора самому.
    /// <para>
    /// Отдельным событием, а не флагом в <see cref="DamageTaken"/>, намеренно: смерть
    /// интересна другому кругу систем (счёт трупов, реакция порта, провал контракта),
    /// и заставлять их фильтровать поток каждого попадания ради одного кадра из сотни -
    /// значит будить их сотню раз впустую.
    /// </para>
    /// <para>
    /// Смертельный удар придёт двумя событиями: сначала <see cref="DamageTaken"/>
    /// с <c>Result.Died = true</c>, следом это. Порядок гарантирован
    /// </para>
    /// </summary>
    public readonly struct CharacterDied
    {
        public CharacterDied(HealthComponent victim, in HitInfo lastHit)
        {
            Victim = victim;
            LastHit = lastHit;
        }

        /// <summary>Кто умер. Объект ещё жив в сцене: труп разбирают подписчики</summary>
        public HealthComponent Victim { get; }

        /// <summary>
        /// Удар, который добил. Нужен тем, кто отыгрывает падение: труп валится
        /// по <see cref="HitInfo.Direction"/> и <see cref="HitInfo.Impulse"/>,
        /// а не просто складывается на месте
        /// </summary>
        public HitInfo LastHit { get; }
    }
}

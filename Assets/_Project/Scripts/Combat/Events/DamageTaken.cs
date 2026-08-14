namespace CrateExpectations.Combat.Events
{
    /// <summary>
    /// По кому-то живому попали. Событие для тех, у кого НЕТ ссылки на пострадавшего:
    /// будущие свидетели драки, счётчик шума, статистика смены. Соседям по объекту -
    /// хитреакту и падению - шина не нужна, у них есть прямая ссылка и локальное
    /// событие <see cref="HealthComponent.Damaged"/>; дублировать их подписку сюда
    /// значило бы платить рассылкой по всей сцене за то, что и так рядом.
    /// <para>
    /// Ни слова про то, кто ударил: <c>Combat</c> знает только, что пришло попадание
    /// с такими-то числами. Кому его приписать - игроку, стражнику или падению
    /// с высоты - решает тот, кто заводил удар, а не тот, кто его принял
    /// </para>
    /// </summary>
    public readonly struct DamageTaken
    {
        public DamageTaken(HealthComponent victim, in DamageResult result, in HitInfo hit)
        {
            Victim = victim;
            Result = result;
            Hit = hit;
        }

        /// <summary>
        /// Кому досталось. Компонентом, а не <c>GameObject</c>: подписчику почти всегда
        /// нужно тут же спросить, сколько здоровья осталось, а от компонента до объекта
        /// один шаг - от объекта до компонента поиск
        /// </summary>
        public HealthComponent Victim { get; }

        /// <summary>Сколько осталось и был ли удар смертельным</summary>
        public DamageResult Result { get; }

        /// <summary>Сам удар: точка, направление, урон и тир</summary>
        public HitInfo Hit { get; }
    }
}

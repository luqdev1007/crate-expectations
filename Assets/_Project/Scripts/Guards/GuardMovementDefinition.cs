using UnityEngine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Повадки стражника в обходе: сколько он стоит на точке и как часто оглядывается.
    /// <para>
    /// Один ассет на всех стражников, а не поля на каждом префабе, - тот же приём,
    /// что у <c>AttackDefinition</c>: правка темпа обхода должна быть одной правкой
    /// в одном месте, а не обходом всех стражников сцены. Расстановка точек при этом
    /// остаётся за <see cref="PatrolRoute"/>: маршрут - свойство места, темп - свойство
    /// стражников вообще
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "GuardMovementDefinition",
        menuName = "CrateExpectations/Guards/Guard Movement Definition")]
    public sealed class GuardMovementDefinition : ScriptableObject
    {
        [Header("Пауза на точке маршрута")]
        [Tooltip("Нижняя граница паузы, с")]
        [field: SerializeField][Min(0f)] public float PauseDurationMin { get; private set; } = 2f;

        [Tooltip("Верхняя граница паузы, с. Пауза сэмплируется равномерно между границами, " +
                 "чтобы обход не читался метрономом")]
        [field: SerializeField][Min(0f)] public float PauseDurationMax { get; private set; } = 5f;

        [Header("Оглядывание")]
        [Tooltip("Вероятность оглядеться на точке, 0..1. Если клип оглядывания длиннее " +
                 "выпавшей паузы, пауза растягивается до длины клипа - оглядывание " +
                 "не обрывается на середине")]
        [field: SerializeField][Range(0f, 1f)] public float LookAroundChance { get; private set; } = 0.4f;

        // Ссылка на клип, а не его длина числом: длину нельзя было бы держать в синхроне
        // с ассетом руками - подменили клип, а число осталось прежним, и оглядывание
        // начало обрываться. Здесь длина всегда берётся у того клипа, который реально
        // играет. Тот же клип стоит в состоянии Looking графа Guard.controller
        [Tooltip("Клип оглядывания. Нужен не как анимация - её проигрывает контроллер, - " +
                 "а как источник длины: пауза на точке не может быть короче него. " +
                 "Пусто - оглядывание не запускается вовсе")]
        [field: SerializeField] public AnimationClip LookAroundClip { get; private set; }
    }
}

using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Настройки карточки груза над ящиком. Инструмент разработчика: показывает истину,
    /// которую игрок «на глаз» знать не должен, поэтому целиком выключается флагом.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CargoOverlayDefinition",
        menuName = "CrateExpectations/Cargo/Cargo Overlay")]
    public sealed class CargoOverlayDefinition : ScriptableObject
    {
        [Tooltip("Показывать карточку. Если выключено: карточка не появляется вовсе")]
        [field: SerializeField] public bool Enabled { get; private set; } = true;

        [Header("Размещение")]
        [Tooltip("Запас над верхней гранью ящика, метры: чтобы карточка не легла на крышку")]
        [field: SerializeField][Min(0f)] public float HeightPadding { get; private set; } = 0.45f;

        [Tooltip("Ближе этого к камере карточку не подпускаем, метры. Вплотную к глазу плашка " +
                 "уходит за ближнюю плоскость отсечения и не рисуется вовсе")]
        [field: SerializeField][Min(0.2f)] public float MinDistance { get; private set; } = 0.7f;

        [Tooltip("Высота карточки в долях высоты кадра. Размер держится постоянным: в мире ящик " +
                 "бывает и в двух шагах, и под носом, а карточка должна читаться одинаково")]
        [field: SerializeField][Range(0.05f, 0.6f)] public float ScreenHeight { get; private set; } = 0.22f;

        [Tooltip("Отступ от верхнего края кадра в долях экрана: ниже этой границы карточку " +
                 "прижимает, когда честный подъём над ящиком уводит её из кадра")]
        [field: SerializeField][Range(0f, 0.5f)] public float ScreenMargin { get; private set; } = 0.02f;

        [Header("Появление")]
        [Tooltip("Сколько секунд карточка проявляется, когда ящик попал под прицел")]
        [field: SerializeField][Min(0f)] public float FadeInSeconds { get; private set; } = 0.12f;

        [Tooltip("Сколько секунд карточка гаснет, когда игрок отвёл взгляд")]
        [field: SerializeField][Min(0f)] public float FadeOutSeconds { get; private set; } = 0.2f;

        [Header("Тексты")]
        [Tooltip("Заголовок, когда истинный тип груза не задан")]
        [field: SerializeField] public string UnknownLabel { get; private set; } = "неизвестный груз";

        [Tooltip("Строки характеристик: {0} окраска, {1} печать, {2} заявлено. " +
                 "Тег <pos> держит значения в общей колонке")]
        [field: SerializeField, TextArea(2, 6)] public string StatsFormat { get; private set; } =
            "Окраска<pos=48%><b>{0}</b>\nПечать<pos=48%><b>{1}</b>\nЗаявлено<pos=48%><b>{2}</b>";

        [Tooltip("Значение пустого поля")]
        [field: SerializeField] public string NoneLabel { get; private set; } = "—";

        [Tooltip("Заявленное разошлось с истиной")]
        [field: SerializeField] public string DivergedLabel { get; private set; } = "РАСХОЖДЕНИЕ";

        [Tooltip("Заявленное совпадает с истиной")]
        [field: SerializeField] public string MatchesLabel { get; private set; } = "чисто";

        [Tooltip("Цвет строки расхождения: должен считываться мгновенно")]
        [field: SerializeField] public Color DivergedColor { get; private set; } = new(0.73f, 0.16f, 0.11f);

        [Tooltip("Цвет строки, когда груз соответствует заявленному")]
        [field: SerializeField] public Color MatchesColor { get; private set; } = new(0.29f, 0.12f, 0.03f);
    }
}

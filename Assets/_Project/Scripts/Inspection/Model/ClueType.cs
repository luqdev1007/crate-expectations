using System;

namespace CrateExpectations.Inspection
{
    /// <summary>Что именно не сошлось при досмотре</summary>
    public enum ClueType
    {
        /// <summary>
        /// Заявлено то, что везти нельзя. Смотреть на ящик для этого не нужно -
        /// достаточно сверить декларацию с реестром запрещённых грузов
        /// </summary>
        DeclaredContraband,

        /// <summary>
        /// Главная улика подмены: внутри не то, что заявлено. Инспектор не рентген -
        /// находит её, только когда внешность дала повод вскрыть ящик
        /// (см. <see cref="ClueEvaluator"/>)
        /// </summary>
        ContentMismatch,

        /// <summary>Ящик покрашен не в тот цвет, какой положен заявленному содержимому</summary>
        PaintMismatch,

        /// <summary>Обязательной пломбы нет вовсе</summary>
        MissingStamp,

        /// <summary>Пломба есть, но не та, какая положена заявленному содержимому</summary>
        WrongStamp,

        /// <summary>
        /// Маскировка не доведена до конца: обязательная окраска не нанесена вообще
        /// (шаг рецепта пропущен, а не выполнен неверно)
        /// </summary>
        IncompleteDisguise,
    }

    /// <summary>
    /// Какие проверки инспектор вообще выполняет. Ленивый может не открывать декларацию
    /// и тем более не вскрывать ящик - это задаётся профилем, а не кодом
    /// </summary>
    [Flags]
    public enum ClueChecks
    {
        None = 0,

        /// <summary>Сверить декларацию с реестром запрещённых грузов</summary>
        Manifest = 1 << 0,

        /// <summary>Вскрыть ящик и сверить содержимое с заявленным</summary>
        Contents = 1 << 1,

        /// <summary>Проверить окраску</summary>
        Paint = 1 << 2,

        /// <summary>Проверить пломбу</summary>
        Stamp = 1 << 3,

        /// <summary>Заметить, что обязательная окраска не нанесена вовсе</summary>
        Completeness = 1 << 4,

        All = Manifest | Contents | Paint | Stamp | Completeness,
    }
}

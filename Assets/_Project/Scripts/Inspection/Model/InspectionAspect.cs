namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Что инспектор проверяет на очередном шаге осмотра. Это единица <i>подачи</i>:
    /// одному аспекту соответствует один заметный игроку шаг - поворот к нужной грани,
    /// подсветка и реплика
    /// </summary>
    public enum InspectionAspect
    {
        /// <summary>Бумаги: сверка декларации с реестром запрещённых грузов</summary>
        Manifest,

        /// <summary>Окраска корпуса</summary>
        Paint,

        /// <summary>Пломба</summary>
        Stamp,

        /// <summary>Содержимое: вскрыть ящик и заглянуть внутрь</summary>
        Contents,
    }

    /// <summary>
    /// Мост между шагами осмотра и правилами ядра. Чистое отображение без состояния:
    /// шаги - это только подача, а что считается уликой и делает ли инспектор такую
    /// проверку вообще, решают <see cref="ClueType"/> и <see cref="ClueChecks"/>.
    /// Благодаря этому отыгрыш не может разойтись с вердиктом: обе стороны смотрят
    /// на одну и ту же таблицу
    /// </summary>
    public static class InspectionAspects
    {
        /// <summary>На каком шаге осмотра всплывает улика такого типа</summary>
        public static InspectionAspect Of(ClueType clue) => clue switch
        {
            ClueType.DeclaredContraband => InspectionAspect.Manifest,
            ClueType.ContentMismatch => InspectionAspect.Contents,
            ClueType.PaintMismatch => InspectionAspect.Paint,
            ClueType.IncompleteDisguise => InspectionAspect.Paint,
            ClueType.MissingStamp => InspectionAspect.Stamp,
            ClueType.WrongStamp => InspectionAspect.Stamp,
            _ => InspectionAspect.Manifest,
        };

        /// <summary>
        /// Какие проверки профиля стоят за этим шагом. Если инспектор не выполняет ни одной
        /// из них, шаг пропускается - ленивый и осматривает груз заметно короче
        /// </summary>
        public static ClueChecks ChecksOf(InspectionAspect aspect) => aspect switch
        {
            InspectionAspect.Manifest => ClueChecks.Manifest,
            InspectionAspect.Paint => ClueChecks.Paint | ClueChecks.Completeness,
            InspectionAspect.Stamp => ClueChecks.Stamp,
            InspectionAspect.Contents => ClueChecks.Contents,
            _ => ClueChecks.None,
        };
    }
}

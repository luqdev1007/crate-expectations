using UnityEngine;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>Насколько всё плохо</summary>
    public enum IssueLevel
    {
        /// <summary>Играть можно, но дизайнер, скорее всего, имел в виду другое</summary>
        Warning,

        /// <summary>Контент сломан: в игре это не заработает</summary>
        Error,
    }

    /// <summary>
    /// Одна находка валидатора: что не так и в каком ассете. Обычное значение без единой
    /// ссылки на UI - окно только рисует список, а собрать его может кто угодно,
    /// в том числе edit-mode тест
    /// </summary>
    public readonly struct ContentIssue
    {
        public ContentIssue(IssueLevel level, string source, string message, Object asset = null)
        {
            Level = level;
            Source = source;
            Message = message;
            Asset = asset;
        }

        /// <summary>Ошибка или предупреждение</summary>
        public IssueLevel Level { get; }

        /// <summary>Какая проверка это нашла - чтобы в списке было видно, о чём речь</summary>
        public string Source { get; }

        /// <summary>Человеческое объяснение проблемы</summary>
        public string Message { get; }

        /// <summary>Проблемный ассет. По клику на запись он подсветится в Project</summary>
        public Object Asset { get; }

        /// <summary>Ошибка</summary>
        public bool IsError => Level == IssueLevel.Error;

        public static ContentIssue Error(string source, string message, Object asset = null) =>
            new(IssueLevel.Error, source, message, asset);

        public static ContentIssue Warning(string source, string message, Object asset = null) =>
            new(IssueLevel.Warning, source, message, asset);

        public override string ToString() =>
            $"[{Level}] {Source}: {Message}" + (Asset != null ? $" ({Asset.name})" : string.Empty);
    }
}

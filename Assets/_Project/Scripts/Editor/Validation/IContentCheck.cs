using System.Collections.Generic;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Одна проверка контента. Читает снимок проекта и дописывает найденное в общий список -
    /// сама ничего не рисует и ни на что не ссылается, поэтому проверяется edit-mode тестом
    /// на собранном руками каталоге
    /// </summary>
    public interface IContentCheck
    {
        /// <summary>Как проверка называется в отчёте</summary>
        string Title { get; }

        /// <summary>Прогнать проверку</summary>
        void Run(ContentCatalog catalog, List<ContentIssue> issues);
    }
}

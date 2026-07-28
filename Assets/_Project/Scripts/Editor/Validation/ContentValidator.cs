using System;
using System.Collections.Generic;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Прогоняет все проверки по снимку контента и собирает находки в один список.
    /// Ни окна, ни <c>AssetDatabase</c>: валидатору всё равно, откуда взялся каталог, -
    /// поэтому тот же валидатор, что живёт в окне, гоняется в edit-mode тесте
    /// </summary>
    public sealed class ContentValidator
    {
        private readonly IContentCheck[] _checks;
        private readonly List<ContentIssue> _issues = new(32);

        /// <summary>Полный набор проверок - тот, что запускает кнопка "Проверить всё"</summary>
        public ContentValidator() : this(
            new BrokenReferencesCheck(),
            new ContractReachableCheck(),
            new RegulationsCoverageCheck(),
            new DockSupplyCheck(),
            new AddressableKeysCheck(),
            new InspectorBalanceCheck())
        {
        }

        /// <summary>Свой набор проверок - для тестов, где нужна ровно одна</summary>
        public ContentValidator(params IContentCheck[] checks) =>
            _checks = checks ?? Array.Empty<IContentCheck>();

        /// <summary>Проверки, входящие в прогон</summary>
        public IReadOnlyList<IContentCheck> Checks => _checks;

        /// <summary>
        /// Проверить контент. Возвращается список находок: сначала ошибки, потом
        /// предупреждения - читать отчёт сверху вниз и есть правильный порядок работы
        /// </summary>
        public IReadOnlyList<ContentIssue> Validate(ContentCatalog catalog)
        {
            _issues.Clear();

            if (catalog == null) 
                return _issues;

            for (int i = 0; i < _checks.Length; i++)
                if (_checks[i] != null) 
                    _checks[i].Run(catalog, _issues);

            _issues.Sort(CompareByLevel);

            return _issues;
        }

        private static int CompareByLevel(ContentIssue a, ContentIssue b)
        {
            // Ошибки выше предупреждений; внутри уровня - по названию проверки,
            // чтобы находки одной природы стояли рядом
            int byLevel = b.Level.CompareTo(a.Level);

            return byLevel != 0 ? byLevel : string.CompareOrdinal(a.Source, b.Source);
        }
    }
}

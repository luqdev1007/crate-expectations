using System;
using System.Collections.Generic;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class ContentValidator
    {
        private readonly IContentCheck[] _checks;
        private readonly List<ContentIssue> _issues = new(32);

        public ContentValidator() : this(
            new BrokenReferencesCheck(),
            new ContractReachableCheck(),
            new RegulationsCoverageCheck(),
            new DockSupplyCheck(),
            new AddressableKeysCheck(),
            new InspectorBalanceCheck())
        {
        }

        public ContentValidator(params IContentCheck[] checks) =>
            _checks = checks ?? Array.Empty<IContentCheck>();

        public IReadOnlyList<IContentCheck> Checks => _checks;

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
            int byLevel = b.Level.CompareTo(a.Level);

            return byLevel != 0 ? byLevel : string.CompareOrdinal(a.Source, b.Source);
        }
    }
}

using System.Collections.Generic;

namespace CrateExpectations.EditorTools.Validation
{
    public interface IContentCheck
    {
        string Title { get; }

        void Run(ContentCatalog catalog, List<ContentIssue> issues);
    }
}

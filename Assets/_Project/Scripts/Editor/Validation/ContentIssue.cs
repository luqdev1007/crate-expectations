using UnityEngine;

namespace CrateExpectations.EditorTools.Validation
{
    public enum IssueLevel
    {
        Warning,
        Error,
    }

    public readonly struct ContentIssue
    {
        public ContentIssue(IssueLevel level, string source, string message, Object asset = null)
        {
            Level = level;
            Source = source;
            Message = message;
            Asset = asset;
        }

        public IssueLevel Level { get; }

        public string Source { get; }

        public string Message { get; }

        public Object Asset { get; }

        public bool IsError => Level == IssueLevel.Error;

        public static ContentIssue Error(string source, string message, Object asset = null) =>
            new(IssueLevel.Error, source, message, asset);

        public static ContentIssue Warning(string source, string message, Object asset = null) =>
            new(IssueLevel.Warning, source, message, asset);

        public override string ToString() =>
            $"[{Level}] {Source}: {Message}" + (Asset != null ? $" ({Asset.name})" : string.Empty);
    }
}

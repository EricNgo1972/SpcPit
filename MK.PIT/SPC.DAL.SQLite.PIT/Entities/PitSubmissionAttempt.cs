using SPC.DAL.SQLite;

namespace SPC.DAL.SQLite.PIT;

/// <summary>SQLite persistence mapping for <c>PitSubmissionAttempts</c>.</summary>
[Serializable]
public class PitSubmissionAttempt : SqliteDataEntity
{
    protected override string GetTableName() => "PitSubmissionAttempts";

    protected override string? OrderByColumns => "AttemptedAt=DESC";

    protected override HashSet<string> GetPrimaryKeyColumns() =>
        new(StringComparer.OrdinalIgnoreCase) { "PitSubmissionAttemptId" };
}

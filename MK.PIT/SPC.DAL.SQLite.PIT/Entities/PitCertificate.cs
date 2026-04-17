using System.Collections;
using SPC.DAL.SQLite;
using SqlKata;

namespace SPC.DAL.SQLite.PIT;

/// <summary>SQLite persistence mapping for <c>PitCertificates</c>.</summary>
[Serializable]
public class PitCertificate : SqliteDataEntity
{
    protected override string GetTableName() => "PitCertificates";

    protected override string? OrderByColumns => "IncomePaymentYear=DESC&UpdatedAt=DESC";

    protected override HashSet<string> GetPrimaryKeyColumns() =>
        new(StringComparer.OrdinalIgnoreCase) { "PitCertificateId" };

    /// <summary>Apply a soft-delete filter to every read so deleted rows never surface.</summary>
    protected override Query AddComputedColumns(Query query)
    {
        query.Where("IsDeleted", 0);
        return query;
    }
}

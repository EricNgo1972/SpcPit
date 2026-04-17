using Microsoft.Data.Sqlite;
using SPC.DAL.SQLite;

namespace SPC.DAL.SQLite.PIT;

/// <summary>
/// Creates the PIT module's SQLite tables and indexes on app startup.
/// Raw CREATE TABLE IF NOT EXISTS — no dependency on the MK.Core SchemaManager,
/// because that one only processes schemas embedded in the SPC.DAL assembly.
/// </summary>
public sealed class DatabaseBootstrapper
{
    private readonly SqlKataDb _database;

    public DatabaseBootstrapper(SqlKataDb database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        await using var session = await _database.OpenAsync();
        await CreateTablesAsync(session.Connection);
        await CreateIndexesAsync(session.Connection);
    }

    private static async Task CreateTablesAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS KeyValueStore (
                Category  TEXT NOT NULL,
                Key       TEXT NOT NULL,
                Value     TEXT,
                UpdatedAt TEXT,
                PRIMARY KEY (Category, Key)
            );

            CREATE TABLE IF NOT EXISTS PitCertificates (
                PitCertificateId          TEXT PRIMARY KEY,
                TaxPayerCode              TEXT,
                ProformaNo                TEXT NOT NULL,
                TaxPayerTaxCode           TEXT NOT NULL,
                TaxPayerName              TEXT NOT NULL,
                Nationality               TEXT,
                ResidentType              TEXT,
                IdentificationNo          TEXT,
                IssueDate                 TEXT,
                IssuePlace                TEXT,
                Phone                     TEXT,
                Email                     TEXT,
                Address                   TEXT,
                InsurancePremiums         REAL,
                CharityDonations          REAL,
                IncomePaymentMonthFrom    INTEGER,
                IncomePaymentMonthTo      INTEGER,
                IncomePaymentYear         INTEGER NOT NULL,
                TotalTaxableIncome        REAL NOT NULL DEFAULT 0,
                AmountPersonalIncomeTax   REAL NOT NULL DEFAULT 0,
                IncomeStillReceivable     REAL,
                IncomeType                TEXT,
                Note                      TEXT,
                RelatedProformaNo         TEXT,
                RelatedFormNo             TEXT,
                Status                    TEXT NOT NULL DEFAULT 'Draft',
                MessageId                 TEXT,
                UnsignedXml               BLOB,
                SignedXml                 BLOB,
                CqtCode                   TEXT,
                RejectReason              TEXT,
                ImportRowNumber           INTEGER NOT NULL DEFAULT 0,
                IsDeleted                 INTEGER NOT NULL DEFAULT 0,
                CreatedAt                 TEXT NOT NULL,
                UpdatedAt                 TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PitSubmissionAttempts (
                PitSubmissionAttemptId TEXT PRIMARY KEY,
                PitCertificateId       TEXT NOT NULL,
                AttemptedAt            TEXT NOT NULL,
                Action                 TEXT NOT NULL,
                Success                INTEGER NOT NULL DEFAULT 0,
                ResponseCode           TEXT,
                ResponseBody           TEXT
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateIndexesAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE UNIQUE INDEX IF NOT EXISTS UX_PitCertificates_ProformaNo
                ON PitCertificates(ProformaNo) WHERE IsDeleted = 0;
            CREATE INDEX IF NOT EXISTS IX_PitCertificates_Status
                ON PitCertificates(Status);
            CREATE INDEX IF NOT EXISTS IX_PitCertificates_Year
                ON PitCertificates(IncomePaymentYear);
            CREATE INDEX IF NOT EXISTS IX_PitCertificates_TaxPayerTaxCode
                ON PitCertificates(TaxPayerTaxCode);
            CREATE INDEX IF NOT EXISTS IX_PitSubmissionAttempts_CertId
                ON PitSubmissionAttempts(PitCertificateId, AttemptedAt DESC);
            """;
        await cmd.ExecuteNonQueryAsync();
    }
}

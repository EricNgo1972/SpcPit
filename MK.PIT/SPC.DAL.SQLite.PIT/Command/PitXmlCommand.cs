using Csla;
using SPC.DAL;
using SPC.DAL.SQLite;
using SqlKata.Execution;
using BoPitXmlCommand = SPC.BO.PIT.Commands.PitXmlCommand;
using BoPitXmlOperation = SPC.BO.PIT.Commands.PitXmlOperation;

namespace SPC.DAL.SQLite.PIT;

/// <summary>
/// Persists the result of a <see cref="BoPitXmlCommand"/>: writes UnsignedXml / SignedXml /
/// Status / CqtCode / MessageId / UpdatedAt columns on the target row. No business logic.
/// </summary>
[Serializable]
public class PitXmlCommand : ICommandDataAccess<BoPitXmlCommand>, Csla.Core.IUseApplicationContext
{
    public ApplicationContext ApplicationContext { get; set; } = null!;
    private SqlKataDb Database => ApplicationContext.GetRequiredService<SqlKataDb>();

    public async Task ExecuteAsync(BoPitXmlCommand command)
    {
        await using var session = await Database.OpenAsync();
        var update = new Dictionary<string, object?> { ["UpdatedAt"] = command.UpdatedAt };

        if (!string.IsNullOrEmpty(command.NewStatus))
            update["Status"] = command.NewStatus;

        switch (command.Operation)
        {
            case BoPitXmlOperation.GenerateUnsigned:
                update["UnsignedXml"] = command.UnsignedXml;
                update["MessageId"] = command.MessageId;
                break;

            case BoPitXmlOperation.MarkSigned:
                update["SignedXml"] = command.SignedXml;
                break;

            case BoPitXmlOperation.MarkAccepted:
                update["CqtCode"] = command.CqtCode;
                update["RejectReason"] = null;
                break;

            case BoPitXmlOperation.MarkRejected:
                update["RejectReason"] = command.RejectReason;
                break;
        }

        await session.Db.Query("PitCertificates")
            .Where("PitCertificateId", command.PitCertificateId)
            .UpdateAsync(update);
    }
}

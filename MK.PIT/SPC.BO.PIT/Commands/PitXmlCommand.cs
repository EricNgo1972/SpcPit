using Csla;
using Csla.Core;
using SPC.BO;
using SPC.BO.PIT.Xml;
using SPC.DAL;

namespace SPC.BO.PIT.Commands;

public enum PitXmlOperation
{
    GenerateUnsigned,
    MarkSigned,
    MarkSubmitted,
    MarkAccepted,
    MarkRejected
}

/// <summary>
/// Lifecycle operations on a PIT certificate's XML: generate the unsigned envelope,
/// attach signed bytes, record submission response. Business logic (XML build, status
/// transitions) lives here; the DAL only persists the resulting column values.
/// </summary>
[Serializable]
public class PitXmlCommand : CommandBase<PitXmlCommand>
{
    // --- Input ---

    public static readonly PropertyInfo<PitXmlOperation> OperationProperty = RegisterProperty<PitXmlOperation>(nameof(Operation));
    public PitXmlOperation Operation
    {
        get => ReadProperty(OperationProperty);
        set => LoadProperty(OperationProperty, value);
    }

    public static readonly PropertyInfo<string> PitCertificateIdProperty = RegisterProperty<string>(nameof(PitCertificateId));
    public string PitCertificateId
    {
        get => ReadProperty(PitCertificateIdProperty);
        set => LoadProperty(PitCertificateIdProperty, value);
    }

    public static readonly PropertyInfo<byte[]?> SignedXmlProperty = RegisterProperty<byte[]?>(nameof(SignedXml));
    public byte[]? SignedXml
    {
        get => ReadProperty(SignedXmlProperty);
        set => LoadProperty(SignedXmlProperty, value);
    }

    public static readonly PropertyInfo<string?> CqtCodeProperty = RegisterProperty<string?>(nameof(CqtCode));
    public string? CqtCode
    {
        get => ReadProperty(CqtCodeProperty);
        set => LoadProperty(CqtCodeProperty, value);
    }

    public static readonly PropertyInfo<string?> RejectReasonProperty = RegisterProperty<string?>(nameof(RejectReason));
    public string? RejectReason
    {
        get => ReadProperty(RejectReasonProperty);
        set => LoadProperty(RejectReasonProperty, value);
    }

    // --- Output (persisted back by DAL) ---

    public static readonly PropertyInfo<byte[]?> UnsignedXmlProperty = RegisterProperty<byte[]?>(nameof(UnsignedXml));
    public byte[]? UnsignedXml
    {
        get => ReadProperty(UnsignedXmlProperty);
        set => LoadProperty(UnsignedXmlProperty, value);
    }

    public static readonly PropertyInfo<string?> MessageIdProperty = RegisterProperty<string?>(nameof(MessageId));
    public string? MessageId
    {
        get => ReadProperty(MessageIdProperty);
        set => LoadProperty(MessageIdProperty, value);
    }

    public static readonly PropertyInfo<string> NewStatusProperty = RegisterProperty<string>(nameof(NewStatus));
    public string NewStatus
    {
        get => ReadProperty(NewStatusProperty);
        set => LoadProperty(NewStatusProperty, value);
    }

    public static readonly PropertyInfo<DateTime> UpdatedAtProperty = RegisterProperty<DateTime>(nameof(UpdatedAt));
    public DateTime UpdatedAt
    {
        get => ReadProperty(UpdatedAtProperty);
        set => LoadProperty(UpdatedAtProperty, value);
    }

    // --- Execute (business logic) ---

    [Execute]
    private async Task Execute()
    {
        UpdatedAt = DateTime.UtcNow;

        switch (Operation)
        {
            case PitXmlOperation.GenerateUnsigned:
                await GenerateUnsignedAsync();
                break;

            case PitXmlOperation.MarkSigned:
                if (SignedXml is null || SignedXml.Length == 0)
                    throw new InvalidOperationException("MarkSigned requires non-empty SignedXml.");
                NewStatus = CertificateStatus.Signed;
                break;

            case PitXmlOperation.MarkSubmitted:
                NewStatus = CertificateStatus.Submitted;
                break;

            case PitXmlOperation.MarkAccepted:
                NewStatus = CertificateStatus.Accepted;
                break;

            case PitXmlOperation.MarkRejected:
                NewStatus = CertificateStatus.Rejected;
                break;
        }

        var dal = ApplicationContext.GetRequiredService<DataAccessResolver>().ResolveCommand<PitXmlCommand>();
        await dal.ExecuteAsync(this);
    }

    private async Task GenerateUnsignedAsync()
    {
        var cert = await ApplicationContext.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitCertificate>().FetchAsync(PitCertificateId);
        var settings = await PitSettings.GetPitSettingsAsync(ApplicationContext);
        var idFactory = ApplicationContext.GetRequiredService<MessageIdFactory>();
        var messageId = idFactory.New(settings.SenderCode);

        var builder = new PitXmlBuilder();
        var result = builder.Build(new PitXmlBuildContext(
            PitCertificateXmlInput.From(cert),
            PitSettingsXmlInput.From(settings),
            messageId,
            DateTime.UtcNow));

        UnsignedXml = result.Xml;
        MessageId = messageId;
        NewStatus = CertificateStatus.XmlGenerated;
    }

    // --- Static convenience helpers ---

    public static async Task<PitXmlCommand> GenerateUnsignedAsync(ApplicationContext ctx, string pitCertificateId)
    {
        var cmd = ctx.CreateInstanceDI<PitXmlCommand>();
        cmd.Operation = PitXmlOperation.GenerateUnsigned;
        cmd.PitCertificateId = pitCertificateId;
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitXmlCommand>().ExecuteAsync(cmd);
    }

    public static async Task<PitXmlCommand> MarkSignedAsync(ApplicationContext ctx, string pitCertificateId, byte[] signedXml)
    {
        var cmd = ctx.CreateInstanceDI<PitXmlCommand>();
        cmd.Operation = PitXmlOperation.MarkSigned;
        cmd.PitCertificateId = pitCertificateId;
        cmd.SignedXml = signedXml;
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitXmlCommand>().ExecuteAsync(cmd);
    }

    public static async Task<PitXmlCommand> MarkSubmittedAsync(ApplicationContext ctx, string pitCertificateId)
    {
        var cmd = ctx.CreateInstanceDI<PitXmlCommand>();
        cmd.Operation = PitXmlOperation.MarkSubmitted;
        cmd.PitCertificateId = pitCertificateId;
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitXmlCommand>().ExecuteAsync(cmd);
    }

    public static async Task<PitXmlCommand> MarkAcceptedAsync(ApplicationContext ctx, string pitCertificateId, string cqtCode)
    {
        var cmd = ctx.CreateInstanceDI<PitXmlCommand>();
        cmd.Operation = PitXmlOperation.MarkAccepted;
        cmd.PitCertificateId = pitCertificateId;
        cmd.CqtCode = cqtCode;
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitXmlCommand>().ExecuteAsync(cmd);
    }

    public static async Task<PitXmlCommand> MarkRejectedAsync(ApplicationContext ctx, string pitCertificateId, string rejectReason)
    {
        var cmd = ctx.CreateInstanceDI<PitXmlCommand>();
        cmd.Operation = PitXmlOperation.MarkRejected;
        cmd.PitCertificateId = pitCertificateId;
        cmd.RejectReason = rejectReason;
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitXmlCommand>().ExecuteAsync(cmd);
    }
}

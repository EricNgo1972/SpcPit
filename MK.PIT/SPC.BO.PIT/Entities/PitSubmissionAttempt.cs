using System.ComponentModel;
using Csla;
using SPC.BO;

namespace SPC.BO.PIT;

/// <summary>Audit record for a Sign or Submit call to an external service.</summary>
[Serializable]
public class PitSubmissionAttempt : EBO<PitSubmissionAttempt>
{
    public static readonly PropertyInfo<string> PitSubmissionAttemptIdProperty = RegisterProperty<string>(nameof(PitSubmissionAttemptId));
    [DataObjectField(true, true)]
    public string PitSubmissionAttemptId
    {
        get => GetProperty(PitSubmissionAttemptIdProperty);
        set => SetProperty(PitSubmissionAttemptIdProperty, value);
    }

    public static readonly PropertyInfo<string> PitCertificateIdProperty = RegisterProperty<string>(nameof(PitCertificateId));
    public string PitCertificateId
    {
        get => GetProperty(PitCertificateIdProperty);
        set => SetProperty(PitCertificateIdProperty, value);
    }

    public static readonly PropertyInfo<DateTime> AttemptedAtProperty = RegisterProperty<DateTime>(nameof(AttemptedAt));
    public DateTime AttemptedAt
    {
        get => GetProperty(AttemptedAtProperty);
        set => SetProperty(AttemptedAtProperty, value);
    }

    public static readonly PropertyInfo<string> ActionProperty = RegisterProperty<string>(nameof(Action));
    public string Action
    {
        get => GetProperty(ActionProperty);
        set => SetProperty(ActionProperty, value);
    }

    public static readonly PropertyInfo<bool> SuccessProperty = RegisterProperty<bool>(nameof(Success));
    public bool Success
    {
        get => GetProperty(SuccessProperty);
        set => SetProperty(SuccessProperty, value);
    }

    public static readonly PropertyInfo<string?> ResponseCodeProperty = RegisterProperty<string?>(nameof(ResponseCode));
    public string? ResponseCode
    {
        get => GetProperty(ResponseCodeProperty);
        set => SetProperty(ResponseCodeProperty, value);
    }

    public static readonly PropertyInfo<string?> ResponseBodyProperty = RegisterProperty<string?>(nameof(ResponseBody));
    public string? ResponseBody
    {
        get => GetProperty(ResponseBodyProperty);
        set => SetProperty(ResponseBodyProperty, value);
    }

    protected override void SetDefaultValues()
    {
        base.SetDefaultValues();
        var ids = ApplicationContext.GetRequiredService<CompactIdGenerator>();
        PitSubmissionAttemptId = ids.NewId("PSA-");
        AttemptedAt = DateTime.UtcNow;
    }
}

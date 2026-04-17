using System.Text.Json;
using Csla;
using SPC.BO;
using SPC.DAL;

namespace SPC.BO.PIT;

/// <summary>
/// Per-deployment settings for the PIT module: the employer's organization details,
/// QĐ 1306 envelope parameters, and external-service endpoints.
/// Stored as key-value rows in KeyValueStore (Category = "PitSettings").
/// </summary>
[Serializable]
public class PitSettings : KeyValueSettingsBase<PitSettings>
{
    public static Task<PitSettings> GetPitSettingsAsync(ApplicationContext ctx) => GetAsync(ctx);

    protected override void SetDefaultValues()
    {
        base.SetDefaultValues();
        LocalAgentSignUrl = "http://localhost:9999/sign";
        XmlSchemaVersion = "2.0.0";
        XmlMessageTypeCode = "201";
        TvanProviderName = "None";
    }

    // --- Employer (Organization) info — written into <MST>, <DLCT>/<TTCTKhauTru> elements ---

    public static readonly PropertyInfo<string> OrganizationTaxCodeProperty = RegisterProperty<string>(nameof(OrganizationTaxCode));
    public string OrganizationTaxCode
    {
        get => GetProperty(OrganizationTaxCodeProperty);
        set => SetProperty(OrganizationTaxCodeProperty, value);
    }

    public static readonly PropertyInfo<string> OrganizationNameProperty = RegisterProperty<string>(nameof(OrganizationName));
    public string OrganizationName
    {
        get => GetProperty(OrganizationNameProperty);
        set => SetProperty(OrganizationNameProperty, value);
    }

    public static readonly PropertyInfo<string> OrganizationAddressProperty = RegisterProperty<string>(nameof(OrganizationAddress));
    public string OrganizationAddress
    {
        get => GetProperty(OrganizationAddressProperty);
        set => SetProperty(OrganizationAddressProperty, value);
    }

    public static readonly PropertyInfo<string> OrganizationPhoneProperty = RegisterProperty<string>(nameof(OrganizationPhone));
    public string OrganizationPhone
    {
        get => GetProperty(OrganizationPhoneProperty);
        set => SetProperty(OrganizationPhoneProperty, value);
    }

    public static readonly PropertyInfo<string> OrganizationEmailProperty = RegisterProperty<string>(nameof(OrganizationEmail));
    public string OrganizationEmail
    {
        get => GetProperty(OrganizationEmailProperty);
        set => SetProperty(OrganizationEmailProperty, value);
    }

    public static readonly PropertyInfo<string> RepresentativeNameProperty = RegisterProperty<string>(nameof(RepresentativeName));
    public string RepresentativeName
    {
        get => GetProperty(RepresentativeNameProperty);
        set => SetProperty(RepresentativeNameProperty, value);
    }

    public static readonly PropertyInfo<string> RepresentativeTitleProperty = RegisterProperty<string>(nameof(RepresentativeTitle));
    public string RepresentativeTitle
    {
        get => GetProperty(RepresentativeTitleProperty);
        set => SetProperty(RepresentativeTitleProperty, value);
    }

    // --- XML envelope parameters (QĐ 1306 §II) ---

    /// <summary>Prefix used in MTDiep (message ID); becomes the MNGui sender code in TTChung.</summary>
    public static readonly PropertyInfo<string> SenderCodeProperty = RegisterProperty<string>(nameof(SenderCode));
    public string SenderCode
    {
        get => GetProperty(SenderCodeProperty);
        set => SetProperty(SenderCodeProperty, value);
    }

    /// <summary>Schema version written to TTChung/PBan. Default "2.0.0".</summary>
    public static readonly PropertyInfo<string> XmlSchemaVersionProperty = RegisterProperty<string>(nameof(XmlSchemaVersion));
    public string XmlSchemaVersion
    {
        get => GetProperty(XmlSchemaVersionProperty);
        set => SetProperty(XmlSchemaVersionProperty, value);
    }

    /// <summary>MLTDiep code (message type). TBD from QĐ 1306 Phụ lục — configurable without recompile.</summary>
    public static readonly PropertyInfo<string> XmlMessageTypeCodeProperty = RegisterProperty<string>(nameof(XmlMessageTypeCode));
    public string XmlMessageTypeCode
    {
        get => GetProperty(XmlMessageTypeCodeProperty);
        set => SetProperty(XmlMessageTypeCodeProperty, value);
    }

    // --- External service endpoints ---

    public static readonly PropertyInfo<string> LocalAgentSignUrlProperty = RegisterProperty<string>(nameof(LocalAgentSignUrl));
    public string LocalAgentSignUrl
    {
        get => GetProperty(LocalAgentSignUrlProperty);
        set => SetProperty(LocalAgentSignUrlProperty, value);
    }

    public static readonly PropertyInfo<string> TvanProviderNameProperty = RegisterProperty<string>(nameof(TvanProviderName));
    public string TvanProviderName
    {
        get => GetProperty(TvanProviderNameProperty);
        set => SetProperty(TvanProviderNameProperty, value);
    }

    public static readonly PropertyInfo<string> TvanSubmitUrlProperty = RegisterProperty<string>(nameof(TvanSubmitUrl));
    public string TvanSubmitUrl
    {
        get => GetProperty(TvanSubmitUrlProperty);
        set => SetProperty(TvanSubmitUrlProperty, value);
    }

    // --- Override FromDto to handle string settings that look like JSON numbers -----------
    // Several PIT settings are strings whose values happen to be all-digit (e.g.
    // XmlMessageTypeCode = "201"). The base KeyValueSettingsBase<T>.FromDto runs every
    // persisted value through JsonDocument.Parse — a bare "201" parses as a JSON number,
    // and deserializing a JSON number into a string property throws
    // "Cannot get the value of a token type 'Number' as a string." This override detects
    // that case and uses the element's raw text instead.

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    protected override void FromDto(DTO<PitSettings> dto)
    {
        var values = dto.ToDictionary();
        if (values is null || values.Count == 0)
        {
            MarkNew();
            return;
        }

        foreach (var prop in FieldManager.GetRegisteredProperties())
        {
            if (!values.TryGetValue(prop.Name, out var raw) || raw is null)
                continue;

            if (raw is string s && string.IsNullOrEmpty(s))
            {
                LoadProperty(prop, null);
                continue;
            }

            var targetType = prop.Type;

            if (raw is JsonElement element)
            {
                object? converted = targetType == typeof(string)
                    ? (element.ValueKind == JsonValueKind.String
                        ? element.GetString() ?? string.Empty
                        : element.ToString())
                    : JsonSerializer.Deserialize(element.GetRawText(), targetType, s_jsonOptions);
                LoadProperty(prop, converted);
            }
            else if (raw.GetType() == targetType)
            {
                LoadProperty(prop, raw);
            }
            else
            {
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                LoadProperty(prop, Convert.ChangeType(raw, underlying));
            }
        }
    }
}

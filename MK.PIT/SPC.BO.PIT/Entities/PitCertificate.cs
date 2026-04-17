using System.ComponentModel;
using Csla;
using Csla.Core;
using Csla.Rules;
using Csla.Rules.CommonRules;
using SPC.BO;

namespace SPC.BO.PIT;

/// <summary>
/// A single Vietnamese Personal Income Tax (TNCN) withholding certificate for one staff member
/// for a given payment period. Maps 1:1 to a QĐ 1306-compliant XML document.
/// Property names match the Excel Import form row-5 system names verbatim.
/// </summary>
[Serializable]
public class PitCertificate : EBO<PitCertificate>
{
    // --- Primary key ---

    public static readonly PropertyInfo<string> PitCertificateIdProperty = RegisterProperty<string>(nameof(PitCertificateId));
    [DataObjectField(true, true)]
    public string PitCertificateId
    {
        get => GetProperty(PitCertificateIdProperty);
        set => SetProperty(PitCertificateIdProperty, value);
    }

    // --- Tax Payer Information (Excel cols C–L) ---

    public static readonly PropertyInfo<string> TaxPayerCodeProperty = RegisterProperty<string>(nameof(TaxPayerCode));
    public string TaxPayerCode
    {
        get => GetProperty(TaxPayerCodeProperty);
        set => SetProperty(TaxPayerCodeProperty, value);
    }

    public static readonly PropertyInfo<string> ProformaNoProperty = RegisterProperty<string>(nameof(ProformaNo));
    public string ProformaNo
    {
        get => GetProperty(ProformaNoProperty);
        set => SetProperty(ProformaNoProperty, value);
    }

    public static readonly PropertyInfo<string> TaxPayerTaxCodeProperty = RegisterProperty<string>(nameof(TaxPayerTaxCode));
    public string TaxPayerTaxCode
    {
        get => GetProperty(TaxPayerTaxCodeProperty);
        set => SetProperty(TaxPayerTaxCodeProperty, value);
    }

    public static readonly PropertyInfo<string> TaxPayerNameProperty = RegisterProperty<string>(nameof(TaxPayerName));
    public string TaxPayerName
    {
        get => GetProperty(TaxPayerNameProperty);
        set => SetProperty(TaxPayerNameProperty, value);
    }

    public static readonly PropertyInfo<string?> NationalityProperty = RegisterProperty<string?>(nameof(Nationality));
    public string? Nationality
    {
        get => GetProperty(NationalityProperty);
        set => SetProperty(NationalityProperty, value);
    }

    public static readonly PropertyInfo<string?> ResidentTypeProperty = RegisterProperty<string?>(nameof(ResidentType));
    public string? ResidentType
    {
        get => GetProperty(ResidentTypeProperty);
        set => SetProperty(ResidentTypeProperty, value);
    }

    public static readonly PropertyInfo<string?> IdentificationNoProperty = RegisterProperty<string?>(nameof(IdentificationNo));
    public string? IdentificationNo
    {
        get => GetProperty(IdentificationNoProperty);
        set => SetProperty(IdentificationNoProperty, value);
    }

    public static readonly PropertyInfo<DateTime?> IssueDateProperty = RegisterProperty<DateTime?>(nameof(IssueDate));
    public DateTime? IssueDate
    {
        get => GetProperty(IssueDateProperty);
        set => SetProperty(IssueDateProperty, value);
    }

    public static readonly PropertyInfo<string?> IssuePlaceProperty = RegisterProperty<string?>(nameof(IssuePlace));
    public string? IssuePlace
    {
        get => GetProperty(IssuePlaceProperty);
        set => SetProperty(IssuePlaceProperty, value);
    }

    public static readonly PropertyInfo<string?> PhoneProperty = RegisterProperty<string?>(nameof(Phone));
    public string? Phone
    {
        get => GetProperty(PhoneProperty);
        set => SetProperty(PhoneProperty, value);
    }

    public static readonly PropertyInfo<string?> EmailProperty = RegisterProperty<string?>(nameof(Email));
    public string? Email
    {
        get => GetProperty(EmailProperty);
        set => SetProperty(EmailProperty, value);
    }

    public static readonly PropertyInfo<string?> AddressProperty = RegisterProperty<string?>(nameof(Address));
    public string? Address
    {
        get => GetProperty(AddressProperty);
        set => SetProperty(AddressProperty, value);
    }

    // --- PIT Withholding (Excel cols M–Y) ---

    public static readonly PropertyInfo<decimal?> InsurancePremiumsProperty = RegisterProperty<decimal?>(nameof(InsurancePremiums));
    public decimal? InsurancePremiums
    {
        get => GetProperty(InsurancePremiumsProperty);
        set => SetProperty(InsurancePremiumsProperty, value);
    }

    public static readonly PropertyInfo<decimal?> CharityDonationsProperty = RegisterProperty<decimal?>(nameof(CharityDonations));
    public decimal? CharityDonations
    {
        get => GetProperty(CharityDonationsProperty);
        set => SetProperty(CharityDonationsProperty, value);
    }

    public static readonly PropertyInfo<int?> IncomePaymentMonthFromProperty = RegisterProperty<int?>(nameof(IncomePaymentMonthFrom));
    public int? IncomePaymentMonthFrom
    {
        get => GetProperty(IncomePaymentMonthFromProperty);
        set => SetProperty(IncomePaymentMonthFromProperty, value);
    }

    public static readonly PropertyInfo<int?> IncomePaymentMonthToProperty = RegisterProperty<int?>(nameof(IncomePaymentMonthTo));
    public int? IncomePaymentMonthTo
    {
        get => GetProperty(IncomePaymentMonthToProperty);
        set => SetProperty(IncomePaymentMonthToProperty, value);
    }

    public static readonly PropertyInfo<int> IncomePaymentYearProperty = RegisterProperty<int>(nameof(IncomePaymentYear));
    public int IncomePaymentYear
    {
        get => GetProperty(IncomePaymentYearProperty);
        set => SetProperty(IncomePaymentYearProperty, value);
    }

    public static readonly PropertyInfo<decimal> TotalTaxableIncomeProperty = RegisterProperty<decimal>(nameof(TotalTaxableIncome));
    public decimal TotalTaxableIncome
    {
        get => GetProperty(TotalTaxableIncomeProperty);
        set => SetProperty(TotalTaxableIncomeProperty, value);
    }

    public static readonly PropertyInfo<decimal> AmountPersonalIncomeTaxProperty = RegisterProperty<decimal>(nameof(AmountPersonalIncomeTax));
    public decimal AmountPersonalIncomeTax
    {
        get => GetProperty(AmountPersonalIncomeTaxProperty);
        set => SetProperty(AmountPersonalIncomeTaxProperty, value);
    }

    public static readonly PropertyInfo<decimal?> IncomeStillReceivableProperty = RegisterProperty<decimal?>(nameof(IncomeStillReceivable));
    public decimal? IncomeStillReceivable
    {
        get => GetProperty(IncomeStillReceivableProperty);
        set => SetProperty(IncomeStillReceivableProperty, value);
    }

    public static readonly PropertyInfo<string?> IncomeTypeProperty = RegisterProperty<string?>(nameof(IncomeType));
    public string? IncomeType
    {
        get => GetProperty(IncomeTypeProperty);
        set => SetProperty(IncomeTypeProperty, value);
    }

    public static readonly PropertyInfo<string?> NoteProperty = RegisterProperty<string?>(nameof(Note));
    public string? Note
    {
        get => GetProperty(NoteProperty);
        set => SetProperty(NoteProperty, value);
    }

    // --- Replacement (only populated when this cert replaces another) ---

    public static readonly PropertyInfo<string?> RelatedProformaNoProperty = RegisterProperty<string?>(nameof(RelatedProformaNo));
    public string? RelatedProformaNo
    {
        get => GetProperty(RelatedProformaNoProperty);
        set => SetProperty(RelatedProformaNoProperty, value);
    }

    public static readonly PropertyInfo<string?> RelatedFormNoProperty = RegisterProperty<string?>(nameof(RelatedFormNo));
    public string? RelatedFormNo
    {
        get => GetProperty(RelatedFormNoProperty);
        set => SetProperty(RelatedFormNoProperty, value);
    }

    public bool IsReplacement => !string.IsNullOrEmpty(RelatedProformaNo);

    // --- Lifecycle ---

    public static readonly PropertyInfo<string> StatusProperty = RegisterProperty<string>(nameof(Status));
    public string Status
    {
        get => GetProperty(StatusProperty);
        set => SetProperty(StatusProperty, value);
    }

    public static readonly PropertyInfo<string?> MessageIdProperty = RegisterProperty<string?>(nameof(MessageId));
    public string? MessageId
    {
        get => GetProperty(MessageIdProperty);
        set => SetProperty(MessageIdProperty, value);
    }

    public static readonly PropertyInfo<byte[]?> UnsignedXmlProperty = RegisterProperty<byte[]?>(nameof(UnsignedXml));
    public byte[]? UnsignedXml
    {
        get => GetProperty(UnsignedXmlProperty);
        set => SetProperty(UnsignedXmlProperty, value);
    }

    public static readonly PropertyInfo<byte[]?> SignedXmlProperty = RegisterProperty<byte[]?>(nameof(SignedXml));
    public byte[]? SignedXml
    {
        get => GetProperty(SignedXmlProperty);
        set => SetProperty(SignedXmlProperty, value);
    }

    public static readonly PropertyInfo<string?> CqtCodeProperty = RegisterProperty<string?>(nameof(CqtCode));
    public string? CqtCode
    {
        get => GetProperty(CqtCodeProperty);
        set => SetProperty(CqtCodeProperty, value);
    }

    public static readonly PropertyInfo<string?> RejectReasonProperty = RegisterProperty<string?>(nameof(RejectReason));
    public string? RejectReason
    {
        get => GetProperty(RejectReasonProperty);
        set => SetProperty(RejectReasonProperty, value);
    }

    public static readonly PropertyInfo<int> ImportRowNumberProperty = RegisterProperty<int>(nameof(ImportRowNumber));
    public int ImportRowNumber
    {
        get => GetProperty(ImportRowNumberProperty);
        set => SetProperty(ImportRowNumberProperty, value);
    }

    public static readonly PropertyInfo<bool> IsDeletedProperty = RegisterProperty<bool>(nameof(IsDeleted));
    public new bool IsDeleted
    {
        get => GetProperty(IsDeletedProperty);
        set => SetProperty(IsDeletedProperty, value);
    }

    public static readonly PropertyInfo<DateTime> CreatedAtProperty = RegisterProperty<DateTime>(nameof(CreatedAt));
    public DateTime CreatedAt
    {
        get => GetProperty(CreatedAtProperty);
        set => SetProperty(CreatedAtProperty, value);
    }

    public static readonly PropertyInfo<DateTime> UpdatedAtProperty = RegisterProperty<DateTime>(nameof(UpdatedAt));
    public DateTime UpdatedAt
    {
        get => GetProperty(UpdatedAtProperty);
        set => SetProperty(UpdatedAtProperty, value);
    }

    // --- Defaults & validation ---

    protected override void SetDefaultValues()
    {
        base.SetDefaultValues();
        var ids = ApplicationContext.GetRequiredService<CompactIdGenerator>();
        PitCertificateId = ids.NewId("PIT-");
        Status = CertificateStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected override void AddBusinessRules()
    {
        base.AddBusinessRules();
        BusinessRules.AddRule(new Required(ProformaNoProperty));
        BusinessRules.AddRule(new Required(TaxPayerTaxCodeProperty));
        BusinessRules.AddRule(new Required(TaxPayerNameProperty));
        BusinessRules.AddRule(new MinValue<int>(IncomePaymentYearProperty, 2000));
        BusinessRules.AddRule(new MinValue<decimal>(TotalTaxableIncomeProperty, 0m));
        BusinessRules.AddRule(new MinValue<decimal>(AmountPersonalIncomeTaxProperty, 0m));
        BusinessRules.AddRule(new MonthRangeRule(IncomePaymentMonthFromProperty, IncomePaymentMonthToProperty));
    }

    /// <summary>Cross-field rule: MonthFrom must be ≤ MonthTo when both are set.</summary>
    private sealed class MonthRangeRule : BusinessRule
    {
        private readonly IPropertyInfo _fromProperty;
        private readonly IPropertyInfo _toProperty;

        public MonthRangeRule(IPropertyInfo fromProperty, IPropertyInfo toProperty)
            : base(fromProperty)
        {
            _fromProperty = fromProperty;
            _toProperty = toProperty;
            InputProperties = new List<IPropertyInfo> { fromProperty, toProperty };
        }

        protected override void Execute(IRuleContext context)
        {
            var from = (int?)context.InputPropertyValues[_fromProperty];
            var to = (int?)context.InputPropertyValues[_toProperty];
            if (from.HasValue && to.HasValue && from.Value > to.Value)
                context.AddErrorResult($"{_fromProperty.FriendlyName} must be less than or equal to {_toProperty.FriendlyName}.");
        }
    }
}

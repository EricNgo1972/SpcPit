using System.ComponentModel;
using Csla;
using SPC.BO;

namespace SPC.BO.PIT;

/// <summary>Read-only projection of a <see cref="PitCertificate"/> for list/detail display.</summary>
[Serializable]
public class PitCertificateInfo : RBOInfo<PitCertificateInfo>
{
    public static readonly PropertyInfo<string> PitCertificateIdProperty = RegisterProperty<string>(nameof(PitCertificateId));
    [DataObjectField(true, true)]
    public string PitCertificateId => GetProperty(PitCertificateIdProperty);

    public static readonly PropertyInfo<string> TaxPayerCodeProperty = RegisterProperty<string>(nameof(TaxPayerCode));
    public string TaxPayerCode => GetProperty(TaxPayerCodeProperty);

    public static readonly PropertyInfo<string> ProformaNoProperty = RegisterProperty<string>(nameof(ProformaNo));
    public string ProformaNo => GetProperty(ProformaNoProperty);

    public static readonly PropertyInfo<string> TaxPayerTaxCodeProperty = RegisterProperty<string>(nameof(TaxPayerTaxCode));
    public string TaxPayerTaxCode => GetProperty(TaxPayerTaxCodeProperty);

    public static readonly PropertyInfo<string> TaxPayerNameProperty = RegisterProperty<string>(nameof(TaxPayerName));
    public string TaxPayerName => GetProperty(TaxPayerNameProperty);

    public static readonly PropertyInfo<int> IncomePaymentYearProperty = RegisterProperty<int>(nameof(IncomePaymentYear));
    public int IncomePaymentYear => GetProperty(IncomePaymentYearProperty);

    public static readonly PropertyInfo<decimal> TotalTaxableIncomeProperty = RegisterProperty<decimal>(nameof(TotalTaxableIncome));
    public decimal TotalTaxableIncome => GetProperty(TotalTaxableIncomeProperty);

    public static readonly PropertyInfo<decimal> AmountPersonalIncomeTaxProperty = RegisterProperty<decimal>(nameof(AmountPersonalIncomeTax));
    public decimal AmountPersonalIncomeTax => GetProperty(AmountPersonalIncomeTaxProperty);

    public static readonly PropertyInfo<string> StatusProperty = RegisterProperty<string>(nameof(Status));
    public string Status => GetProperty(StatusProperty);

    public static readonly PropertyInfo<string?> CqtCodeProperty = RegisterProperty<string?>(nameof(CqtCode));
    public string? CqtCode => GetProperty(CqtCodeProperty);

    public static readonly PropertyInfo<string?> RelatedProformaNoProperty = RegisterProperty<string?>(nameof(RelatedProformaNo));
    public string? RelatedProformaNo => GetProperty(RelatedProformaNoProperty);

    public bool IsReplacement => !string.IsNullOrEmpty(RelatedProformaNo);

    public static readonly PropertyInfo<DateTime> UpdatedAtProperty = RegisterProperty<DateTime>(nameof(UpdatedAt));
    public DateTime UpdatedAt => GetProperty(UpdatedAtProperty);
}

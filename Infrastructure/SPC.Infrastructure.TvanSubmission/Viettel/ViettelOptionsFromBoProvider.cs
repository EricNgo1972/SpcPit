using Csla;
using SPC.BO.PIT;

namespace SPC.Infrastructure.TvanSubmission.Viettel;

/// <summary>
/// Default provider: loads Viettel settings from the DB-backed <see cref="ViettelSettings"/>
/// BO (KeyValueStore). Every submission reads fresh values — the app does not restart to
/// pick up a credential or endpoint change made through the settings UI.
/// </summary>
public sealed class ViettelOptionsFromBoProvider : IViettelOptionsProvider
{
    private readonly ApplicationContext _applicationContext;

    public ViettelOptionsFromBoProvider(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<ViettelOptions> GetAsync(CancellationToken cancellationToken = default)
    {
        var s = await ViettelSettings.GetViettelSettingsAsync(_applicationContext);
        return new ViettelOptions
        {
            BaseUrl                 = s.BaseUrl,
            LoginPath               = s.LoginPath,
            SubmitPath              = s.SubmitPath,
            Username                = s.Username,
            Password                = s.Password,
            SupplierTaxCode         = s.SupplierTaxCode,
            InvoiceSeries           = s.InvoiceSeries,
            InvoiceType             = s.InvoiceType,
            TemplateCode            = s.TemplateCode,
            CurrencyCode            = s.CurrencyCode,
            TokenRefreshSkewSeconds = s.TokenRefreshSkewSeconds > 0 ? s.TokenRefreshSkewSeconds : 60,
            TimeoutSeconds          = s.TimeoutSeconds          > 0 ? s.TimeoutSeconds          : 30
        };
    }
}

using Csla;
using Microsoft.Extensions.DependencyInjection;
using SPC.BO.PIT;
using SPC.Infrastructure.TvanSubmission.Viettel;

namespace SPC.Infrastructure.TvanSubmission;

/// <summary>
/// Runtime switch over <see cref="ITvanSubmissionService"/>. Reads the active provider from
/// <see cref="PitSettings.TvanProviderName"/> at every submission and delegates to the
/// matching concrete adapter, so users can change providers in the app without a restart.
/// </summary>
public sealed class TvanSubmissionDispatcher : ITvanSubmissionService
{
    private readonly IServiceProvider _services;
    private readonly ApplicationContext _applicationContext;

    public TvanSubmissionDispatcher(IServiceProvider services, ApplicationContext applicationContext)
    {
        _services = services;
        _applicationContext = applicationContext;
    }

    public async Task<TvanSubmissionResponse> SubmitAsync(TvanSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = await PitSettings.GetPitSettingsAsync(_applicationContext);
        var provider = TvanProviderCatalog.Get(settings.TvanProviderName);

        if (!provider.IsAvailable)
            throw new InvalidOperationException(
                $"T-VAN provider '{provider.DisplayName}' is not implemented yet. Pick a different provider in PIT Settings.");

        ITvanSubmissionService adapter = provider.Code switch
        {
            TvanProviderCatalog.ViettelCode => _services.GetRequiredService<ViettelTvanSubmissionService>(),
            TvanProviderCatalog.NoneCode    => throw new InvalidOperationException(
                "No T-VAN provider selected. Pick one in PIT Settings before submitting."),
            _ => _services.GetRequiredService<StubTvanSubmissionService>(),
        };

        if (provider.Code == TvanProviderCatalog.ViettelCode)
        {
            var viettel = await ViettelSettings.GetViettelSettingsAsync(_applicationContext);
            if (!viettel.IsReadyForSubmission)
                throw new InvalidOperationException(
                    "Viettel is not ready. Open Viettel Service Settings, run Test Connection successfully, then submit again.");
        }

        return await adapter.SubmitAsync(request, cancellationToken);
    }
}

using System.Collections;
using System.Collections.Specialized;
using Csla;
using SPC.BO;

namespace SPC.BO.PIT;

/// <summary>Read-only list of PIT certificates for list views and dashboards.</summary>
[Serializable]
public class PitCertificateInfoList : RBOInfoList<PitCertificateInfoList, PitCertificateInfo>
{
    public static async Task<PitCertificateInfoList> GetAllAsync(ApplicationContext ctx) =>
        await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitCertificateInfoList>().FetchAsync();

    public static async Task<PitCertificateInfoList> GetByStatusAsync(ApplicationContext ctx, string status)
    {
        IDictionary criteria = new OrderedDictionary { ["Status"] = status };
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitCertificateInfoList>().FetchAsync(criteria);
    }

    public static async Task<PitCertificateInfoList> GetByYearAsync(ApplicationContext ctx, int year)
    {
        IDictionary criteria = new OrderedDictionary { ["IncomePaymentYear"] = year };
        return await ctx.GetRequiredService<IDataPortalFactory>()
            .GetPortal<PitCertificateInfoList>().FetchAsync(criteria);
    }
}

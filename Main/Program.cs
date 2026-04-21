using Csla.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting.WindowsServices;
using MudBlazor.Services;
using SPC.BO;
using SPC.BO.PIT;
using SPC.Blazor;
using SPC.DAL.SQLite.PIT;
using SPC.Infrastructure.TvanSubmission;
using SPC.Infrastructure.XmlSigning;
using Main.Components;
using Main.Services;

var builder = WebApplication.CreateBuilder(args);

if (IsServiceMode())
{
    // Service managers often launch from a different working directory. Pin the
    // content root to the deployed app folder so the DB, keys, config, and static
    // assets resolve consistently on both Windows and Linux.
    builder.Environment.ContentRootPath = AppContext.BaseDirectory;
}

builder.Host.UseWindowsService();
builder.Host.UseSystemd();

// When launching Main.exe directly from bin/Debug, explicitly load the static-web-assets
// manifest so package/RCL assets (e.g. MudBlazor) still resolve.
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Data Protection — encrypts sensitive BO values (e.g. Viettel password) before they
// reach the DAL. Keys persist to {ContentRoot}/keys so encrypted rows survive restarts.
// Back up the keys folder together with spc-pit.db.
builder.Services.AddDataProtection()
    .SetApplicationName("SpcPit")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));
builder.Services.AddSingleton<ISensitiveDataProtector, DataProtectionSensitiveDataProtector>();

// CSLA + MK.Core + PIT module
builder.Services.AddCsla();
builder.Services.AddSPCBO();
builder.Services.AddSPCBlazor();
builder.Services.AddSPCBOPIT(builder.Configuration);
builder.Services.AddSPCDALPIT(builder.Configuration);

// External gateway adapters (stubs by default; Mode switch in appsettings)
builder.Services.AddXmlSigning(builder.Configuration);
builder.Services.AddTvanSubmission(builder.Configuration);

var app = builder.Build();

// Create SQLite tables + indexes on startup.
app.UseSPCDALPIT();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(SPC.Blazor.PIT.Components.Pages.PitDashboard).Assembly);

app.Run();

static bool IsServiceMode()
{
    return WindowsServiceHelpers.IsWindowsService()
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVOCATION_ID"));
}

// Expose the implicit top-level Program class so Razor (<Router AppAssembly="@typeof(Program).Assembly">)
// can resolve it unambiguously.
public partial class Program { }

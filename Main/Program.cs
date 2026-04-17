using Csla.Configuration;
using MudBlazor.Services;
using SPC.BO;
using SPC.BO.PIT;
using SPC.Blazor;
using SPC.DAL.SQLite.PIT;
using SPC.Infrastructure.TvanSubmission;
using SPC.Infrastructure.XmlSigning;
using Main.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

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

// Expose the implicit top-level Program class so Razor (<Router AppAssembly="@typeof(Program).Assembly">)
// can resolve it unambiguously.
public partial class Program { }

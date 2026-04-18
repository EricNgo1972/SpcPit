# SPC PIT

PIT withholding certificate app built on .NET 8.

It covers:
- PIT certificate entry and listing
- XML generation/signing for QĐ 1306 flows
- PDF rendering
- T-VAN submission integration
- Viettel service configuration and readiness testing

## Solution Layout

- `Main/`: ASP.NET Core host
- `MK.PIT/SPC.Blazor.PIT/`: Blazor UI pages/components
- `MK.PIT/SPC.BO.PIT/`: PIT business objects and settings
- `MK.PIT/SPC.DAL.SQLite.PIT/`: SQLite-backed data access
- `Infrastructure/SPC.Infrastructure.TvanSubmission/`: T-VAN adapters and dispatcher
- `Infrastructure/SPC.Infrastructure.XmlSigning/`: XML signing integration
- `Tests/SPC.Tests.PIT/`: automated tests
- `docs/`: supporting documentation

## Key Pages

- `/`: dashboard
- `/pit/certificates`: certificate list, bulk actions, provider-aware workflow
- `/pit/settings`: PIT settings and active T-VAN selection
- `/pit/settings/viettel`: Viettel service settings and connection test

## Viettel Readiness Flow

Viettel submission is gated by a successful connection test.

1. Open `Viettel Service Settings`
2. Save endpoint, credentials, MST, and series values
3. Click `Test Connection`
4. If login succeeds, Viettel is marked `Ready`
5. Certificate submission is enabled only while that tested config remains unchanged

If Viettel settings change after a successful test, readiness is invalidated automatically and the connection must be tested again.

## Build

From the repo root:

```bash
dotnet build SpcPit.sln
```

If using the Windows SDK from WSL:

```bash
WIN_SOLUTION="$(wslpath -w 'SpcPit.sln')"
'/mnt/c/Program Files/dotnet/dotnet.exe' build "$WIN_SOLUTION" -v minimal
```

## Test

```bash
dotnet test Tests/SPC.Tests.PIT/SPC.Tests.PIT.csproj
```

## Notes

- Target framework: `.NET 8`
- Some providers are listed in the UI but are still planned/not implemented
- Viettel uses structured JSON submission and signs internally
- XML-related dashboard/certificate actions are shown only for workflows that require them

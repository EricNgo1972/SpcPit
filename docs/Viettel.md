# Viettel T-VAN integration

This module submits Vietnamese Personal Income Tax withholding certificates (**Chứng từ khấu trừ thuế TNCN**) to Viettel's T-VAN gateway. Viettel signs internally and forwards to the General Department of Taxation; we receive back an invoice number, a reservation code, and (asynchronously) the official tax-authority code (mã CQT).

**Reference** — Viettel integration guide v1.0 (`Tai_lieu_tich_hop_chung_tu_khau_tru_thue_v1.0.doc`). The adapter targets the **CTS Server** flow (no USB token); CTS Token and Cloud CA flows are not wired yet.

---

## 1. What Viettel expects

- **Structured JSON** — three sections: `generalInvoiceInfo`, `taxPayerInfo`, `incomeInfo`. Viettel handles signing and forwarding to CQT internally for CTS Server customers. We do **not** send our own signed XML to Viettel (it's kept for local audit and alternative providers).
- **Cookie-based auth** — `Cookie: access_token=<token>` on every request. Not `Authorization: Bearer`.
- **Product / series pre-registration** — the `invoiceSeries` you send must match a series you've registered on Viettel's portal (e.g. `CT/25E`). Viettel rejects unknown series.

---

## 2. Endpoints we call

| Step | Method | Path | Notes |
|---|---|---|---|
| Login | `POST` | `/auth/login` | JSON body `{"username","password"}` → `{"access_token","expires_in","token_type"}` |
| Submit | `POST` | `/api/InvoiceAPI/InvoiceWS/createTaxDeductionCertificate/{supplierTaxCode}` | `Content-Type: application/json`, `Cookie: access_token=…` header, JSON body as §4 |

The `SubmitPath` contains a `{mst}` placeholder (case-insensitive) that the adapter replaces with the configured `SupplierTaxCode` at call time.

Base URL defaults to `https://api-sinvoice.viettel.vn` (production). Use Viettel's dedicated test environment during integration — they grant a separate sandbox URL + credentials on request.

---

## 3. Configuration

Settings are split across two places:

### 3a. Provider switch (appsettings.json — startup only)

```jsonc
{
  "Tvan": {
    "Mode": "Viettel"     // "Stub" | "Viettel"
  }
}
```

Changing `Mode` requires an app restart (it rewires DI).

### 3b. Credentials & endpoints (in-app settings page — hot-reloaded)

Go to **PIT Settings → Viettel Service Settings** (`/pit/settings/viettel`). The page opens from the Settings page and is intentionally **not in the main navigation menu**.

| Field | Purpose |
|---|---|
| Base URL | Viettel gateway, e.g. `https://api-sinvoice.viettel.vn` |
| Login path | Default `/auth/login` |
| Submit path | Default `/api/InvoiceAPI/InvoiceWS/createTaxDeductionCertificate/{mst}` (the `{mst}` placeholder is substituted with SupplierTaxCode) |
| Username | Viettel-issued account (often `MST-suffix`, e.g. `0100109106-712`) |
| Password | Encrypted at the BO layer via ASP.NET Core Data Protection before reaching the DAL (see §12) |
| Supplier tax code | Employer MST; replaces `{mst}` in the Submit path |
| Invoice series | Must match a series registered with Viettel (e.g. `CT/25E`) |
| Invoice type / Template code | `03/TNCN` for PIT withholding certificates |
| Currency code | Default `VND` |
| Token refresh skew (s) | Refresh cached access_token this many seconds before expiry |
| Request timeout (s) | HTTP request timeout |

Persisted in `KeyValueStore` under `Category = "ViettelSettings"` via `SPC.BO.PIT.ViettelSettings : KeyValueSettingsBase<ViettelSettings>`. Saves take effect on the next submission — no restart needed.

**Required to go live** — Username, Password, Supplier tax code, Invoice series. The adapter throws `InvalidOperationException` on first call if any are blank, with a message pointing to the Viettel Service Settings page.

**Secrets note** — the password is encrypted at rest. See §12 below for the encryption model, key storage, backup guidance, and recovery behavior.

---

## 4. Request payload

Built by `Infrastructure/SPC.Infrastructure.TvanSubmission/Viettel/ViettelPayloadMapper.cs` from the `PitCertificate` + `PitSettings` in the BO. One cert per submit.

```json
{
  "generalInvoiceInfo": {
    "transactionUuid":    "<GUID>",
    "invoiceType":        "03/TNCN",
    "templateCode":       "03/TNCN",
    "invoiceSeries":      "CT/25E",
    "invoiceIssuedDate":  1747880851000,
    "currencyCode":       "VND",
    "exchangeRate":       1,
    "adjustmentType":     "1",           // "1" = original, "2" = replacement
    "paymentStatus":      true,
    "cusGetInvoiceRight": true,
    "originalInvoiceId":        null,    // only for replacements
    "originalInvoiceIssueDate": null,
    "adjustmentInvoiceType":    null,    // "2" for replacements
    "additionalReferenceDesc":  null,
    "additionalReferenceDate":  null
  },
  "taxPayerInfo": {
    "taxpayerName":        "Nguyễn Văn A",
    "taxpayerTaxCode":     "9876543210",
    "taxpayerAddress":     "123 Lê Lợi, Q.1, TP.HCM",
    "taxpayerNationality": "Việt Nam",
    "taxpayerResidence":   1,            // 1 = cư trú, 0 = non-resident
    "taxpayerIdNumber":    "012345678901",
    "taxpayerPhoneNumber": "0901234567",
    "taxpayerMailAddress": "a@example.com",
    "taxpayerNote":        null
  },
  "incomeInfo": {
    "incomeType":                       "Tiền lương",
    "paymentStartMonth":                1,
    "paymentEndMonth":                  12,
    "paymentYear":                      2025,
    "charityAmount":                    0,
    "insuranceAmount":                  1500000,
    "totalTaxableIncome":               180000000,
    "totalTaxCalculationIncome":        178500000,   // = totalTaxable - insurance - charity (clamped ≥0)
    "amountOfPersonalIncomeTaxWithheld": 18000000
  }
}
```

### Field derivations worth knowing

| Viettel field | Source |
|---|---|
| `taxpayerResidence` | `1` if `PitCertificate.ResidentType == "00081"`, else `0` |
| `totalTaxCalculationIncome` | `TotalTaxableIncome - InsurancePremiums - CharityDonations`, clamped to ≥ 0 |
| `adjustmentType` | `"2"` when `RelatedProformaNo` is set, else `"1"` |
| `originalInvoiceId` | `PitCertificate.RelatedProformaNo` (only emitted for replacements) |
| `invoiceIssuedDate` | `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` |
| `transactionUuid` | Fresh `Guid.NewGuid()` per request |

---

## 5. Response shape

```json
{
  "errorCode":   null,
  "description": null,
  "result": {
    "supplierTaxCode": "0100109106-998",
    "invoiceNo":       "CT/25E113",
    "transactionID":   "174952110424264570",
    "reservationCode": "DN9EEB8GDTCZKEX",
    "codeOfTax":       null
  }
}
```

Mapped to `TvanSubmissionResponse`:

| `TvanSubmissionResponse` field | Source |
|---|---|
| `Accepted` | `errorCode == null && invoiceNo != null` |
| `InvoiceNo` | `result.invoiceNo` |
| `TransactionId` | `result.transactionID` |
| `ReservationCode` | `result.reservationCode` |
| `CqtCode` | `result.codeOfTax` ?? `result.reservationCode` (see §6) |
| `RejectReason` | `description` ?? `errorCode` (when not accepted) |
| `Raw` | full JSON body |

### Mã CQT is often async

Viettel's initial response commonly has `"codeOfTax": null`. That doesn't mean rejected — it means **Viettel accepted the cert into their system** and will forward to CQT. The real tax-authority code arrives minutes to hours later via a status-query endpoint (not yet wired).

Our adapter therefore:
- Treats `errorCode == null && invoiceNo` as success
- Stores `reservationCode` as `CqtCode` when `codeOfTax` is absent so the cert has a non-empty identifier right away
- The cert lifecycle transitions to `Accepted` immediately; a later job can upgrade `CqtCode` once the real code is available

---

## 6. Error & rejection handling

| Situation | Adapter behavior |
|---|---|
| HTTP 2xx + `errorCode == null` + `invoiceNo` present | `Accepted = true` |
| HTTP 2xx + `errorCode != null` | `Accepted = false`, `RejectReason` = `description` (or `errorCode`) |
| HTTP 2xx with unparsable JSON | `Accepted = false`, `RejectReason = "Viettel returned non-JSON payload."` |
| HTTP 401 Unauthorized | Token invalidated → one retry with fresh token. If still 401 → `Accepted = false`, reason includes the HTTP code |
| Non-2xx HTTP | `Accepted = false`, `RejectReason = "HTTP {code}: {truncated body}"` |
| Missing credentials / series / MST | Throws `InvalidOperationException` synchronously on first call |
| Request timeout (`TimeoutSeconds`) | `TaskCanceledException` bubbles up — caller's try/catch surfaces it |

Viettel error codes (from integration guide §8) are provider-specific strings like `"INVALID_MST"`, `"DUPLICATE_CERTIFICATE"`, `"SERIES_NOT_REGISTERED"`. We surface them verbatim in `RejectReason` without mapping.

---

## 7. Authentication

### Token caching

`ViettelTokenStore` holds one access token per process. On every submit:

1. If the cached token exists and `now + TokenRefreshSkewSeconds < expiresAt` → reuse it.
2. Otherwise call `/auth/login` (under a single-flight semaphore) and cache the result.
3. If the submit returns 401, invalidate the cache and retry once with a fresh login.

Login response's `expires_in` (seconds) drives the cached expiry. Missing / zero → 30-minute fallback.

### Auth header format

```
Cookie: access_token=<token>
```

This is **not** standard `Authorization: Bearer` — it's Viettel's convention per integration guide §5.5. The adapter sets it with `Headers.TryAddWithoutValidation` to avoid .NET's Cookie validation pipeline.

---

## 8. Wiring in DI

`Infrastructure/SPC.Infrastructure.TvanSubmission/DependencyInjection.cs`:

```csharp
services.Configure<TvanSubmissionOptions>(configuration.GetSection("Tvan"));
var mode = configuration["Tvan:Mode"] ?? "Stub";
if (string.Equals(mode, "Viettel", StringComparison.OrdinalIgnoreCase))
{
    services.AddScoped<IViettelOptionsProvider, ViettelOptionsFromBoProvider>();
    services.AddHttpClient<ITvanSubmissionService, ViettelTvanSubmissionService>();
}
else
{
    services.AddScoped<ITvanSubmissionService, StubTvanSubmissionService>();
}
```

Switching providers: one `appsettings.Mode` change + an app restart.

Credential/endpoint changes: no restart — `ViettelOptionsFromBoProvider` reads `ViettelSettings` from the DB on every submit.

---

## 9. End-to-end flow

```
  PitCertificates.razor (Submit button)
       │
       ▼   for each selected cert that is in Signed state
  PitCertificate BO  ──── fetched ────▶  TvanSubmissionRequest { Certificate, Settings, SignedXml }
       │                                          │
       │                                          ▼
       │                            ViettelTvanSubmissionService.SubmitAsync
       │                                          │
       │                              ┌───────────┴───────────┐
       │                              ▼                       ▼
       │                     ViettelTokenStore        ViettelPayloadMapper
       │                     (cached access_token)    (build JSON payload)
       │                              │                       │
       │                              └──────────┬────────────┘
       │                                         ▼
       │                     POST /api/InvoiceAPI/InvoiceWS/createTaxDeductionCertificate/{mst}
       │                     Cookie: access_token=…
       │                     Body: { generalInvoiceInfo, taxPayerInfo, incomeInfo }
       │                                         │
       │                                         ▼
       │                     { errorCode, description, result: { invoiceNo, reservationCode, codeOfTax, … } }
       │                                         │
       ▼                                         ▼
  PitXmlCommand.MarkAccepted(id, cqtCode)   or   MarkRejected(id, reason)
       │
       ▼
  cert.Status = Accepted  (locked — downloads only)
```

---

## 10. Testing

Unit tests: `Tests/SPC.Tests.PIT/Tvan/ViettelTvanSubmissionServiceTests.cs` (10 cases, all with a mocked `HttpMessageHandler`):

- Login → submit happy path returns `invoiceNo`, `transactionId`, `reservationCode`
- `codeOfTax` (when present) wins over `reservationCode` for `CqtCode`
- Token is cached across submissions (one login, two submits)
- 401 on submit → token refresh → retry once → success
- Business rejection (`errorCode != null`) → `Accepted = false` with `description` as reason
- Non-2xx HTTP → `Accepted = false`, reject reason includes status code + truncated body
- Missing credentials throws on first call
- Request assertions: `Cookie` header, all three JSON sections, `invoiceSeries`, `invoiceType`, `taxpayerResidence`, `paymentYear`, correct `totalTaxCalculationIncome` math
- Replacement cert → `adjustmentType = "2"` + `originalInvoiceId`
- Non-resident (`ResidentType != "00081"`) → `taxpayerResidence = 0`

Run them: `dotnet test Tests/SPC.Tests.PIT/ --filter 'FullyQualifiedName~Viettel'`.

### Manual smoke against Viettel sandbox

1. Set `Tvan:Mode = "Viettel"` in `appsettings.json` and restart.
2. Open `/pit/settings/viettel` (or `/pit/settings` → click **Viettel Service Settings**) and fill in sandbox Base URL + credentials + SupplierTaxCode + InvoiceSeries. Save.
3. Open `/pit/settings` and confirm the employer info is correct (it populates `taxPayerInfo`-parent MST).
4. Import one staff row → Generate XML → Sign (stub is fine; Viettel doesn't use the XML) → Submit.
5. Check the snackbar for the accepted count; inspect the cert on `/pit/certificates` — status `Accepted`, `CqtCode` populated (initially the `reservationCode`).
6. In Viettel's web console, verify the same `invoiceNo` appears under the registered series.

---

## 11. What's not implemented yet (open items)

- **CTS Token / Cloud CA flow** — `createTaxDeductionCertificateUsbTokenGetHash` + sign externally + final submit (guide §5.7 / §7.31–7.33). Requires Local​Agent to return the signed-hash; the two-step API is straightforward to add on top of the current adapter.
- **Batch submission** — `createBatchTaxDeductionCertificate` sends up to N certs in one call. Useful for end-of-year bulk issuance. Currently we submit one at a time with the existing bulk UI.
- **Mã CQT poll** — query Viettel for `codeOfTax` after acceptance. Needs a status-query endpoint from the guide plus a scheduled task that walks `Accepted` rows whose `CqtCode` still equals `reservationCode`.
- **Email / reissue endpoints** — `sendMail`, reissue / cancel flows exist in the guide (§7.12+); not needed for MVP.
- **Error-code catalogue** — integration guide §8 lists common error codes. Today we surface them verbatim; a lookup table (`ErrorCodeCatalog.cs`) would give HR friendlier messages.
- **Real-provider contract test** — ping the Viettel sandbox in CI (gated by secrets) to catch upstream shape changes early.

---

## 12. Secret encryption at rest

The Viettel **password** is encrypted before it leaves the BO and reaches the DAL. The app uses **ASP.NET Core Data Protection**; a thin interface (`SPC.BO.PIT.ISensitiveDataProtector`) keeps the BO free of ASP.NET dependencies, and the concrete implementation (`Main/Services/DataProtectionSensitiveDataProtector`) plugs in at the composition root.

### Where the code lives

- **Interface** — `MK.PIT/SPC.BO.PIT/Services/ISensitiveDataProtector.cs` (two methods: `Protect`, `Unprotect`).
- **Implementation** — `Main/Services/DataProtectionSensitiveDataProtector.cs` wraps `IDataProtectionProvider.CreateProtector("SPC.BO.PIT.SensitiveData.v1")`.
- **BO hook** — `ViettelSettings` overrides `ToDto` (encrypt before persist) and `FromDto` (decrypt after load). Only the `Password` property is treated as a secret today; extending the list is a one-line change in `IsSecret(string)`.
- **DI wiring** — `Main/Program.cs` calls `AddDataProtection().SetApplicationName("SpcPit").PersistKeysToFileSystem("{ContentRoot}/keys")` and registers the protector as a singleton.

### How a saved password looks

1. UI writes `Password = "s3cr3t"` (plaintext in-memory).
2. `SaveAsync()` → CSLA calls `ToDto()` → we encrypt → DTO carries `"enc:v1:<ciphertext>"`.
3. SettingsDataAccessBase writes that string into `KeyValueStore.Value`.
4. Next load: the stored value is read, `FromDto` sees the `enc:v1:` prefix, calls `Unprotect`, and `Password` is plaintext again in the BO.

The prefix is a format version. Legacy rows without a prefix (pre-encryption) are loaded verbatim as plaintext and automatically re-saved encrypted the next time the user presses **Save** — no migration step required.

### Key material

Keys live in **`{ContentRoot}/keys/*.xml`** (for a dev run that's `Main/keys/`). Data Protection handles key creation and rotation automatically.

**Back up the `keys` folder together with `spc-pit.db`.** Losing either half renders encrypted passwords unreadable. A missing/rotated key does not corrupt the row — the BO catches the decryption error, logs it, and loads an empty password; the user must re-enter it in the Viettel Service Settings page.

### Operational notes

- Key material is machine-bound by default (DPAPI on Windows). For multi-host or container deployments, point Data Protection at a shared store (Azure Blob, Redis, or a distributed file share) via `PersistKeysToAzureBlobStorage` / similar — change only `Program.cs`.
- Rotating `keys/` (e.g. key-compromise response) invalidates all encrypted rows; expect users to re-enter the password after rotation.
- Nothing prevents an admin with DB read access AND keys-folder read access from recovering the plaintext. Treat the keys folder as sensitive.

---

## 13. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `InvalidOperationException: Viettel username and password are required.` | Settings blank | Fill them in on `/pit/settings/viettel` |
| `HTTP 401` on every submit after the first | Token rejected; `Cookie` header stripped by a proxy | Check outbound proxy config; ensure `Cookie` header isn't being dropped |
| `errorCode: "SERIES_NOT_REGISTERED"` | `InvoiceSeries` doesn't match what's registered with Viettel | Update Invoice series on the Viettel Settings page; or register the series on Viettel's portal |
| `errorCode: "DUPLICATE_CERTIFICATE"` | Same `transactionUuid` reused (shouldn't happen — we Guid.NewGuid() every call) or same `invoiceNo` re-submitted | Check whether the cert was already submitted successfully (look at `reservationCode` in `Raw` field of the first response) |
| `HTTP 502/504` intermittently | Viettel upstream flake | Retry later; the adapter returns the body verbatim so support can grep |
| Status stays `Accepted` but `CqtCode` still looks like a reservation code (no `M2-…` format) | Viettel hasn't issued the real mã CQT yet | Wait — it arrives async. Poll endpoint will be implemented in a later milestone (see §11) |
| Password field appears empty after a restart | The `keys/` folder was moved / deleted, or the app is running as a different user who can't read it | Re-enter the password in the Viettel Service Settings page; ensure the runtime user owns `{ContentRoot}/keys/*.xml` |
| `CryptographicException: The key {id} was not found` in logs | Data Protection key rotated or unavailable | Back up keys before rotation. If rotation is intentional, re-enter affected passwords |

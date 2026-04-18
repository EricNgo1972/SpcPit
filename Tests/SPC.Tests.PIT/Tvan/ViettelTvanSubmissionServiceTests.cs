using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SPC.BO.PIT.Xml;
using SPC.Infrastructure.TvanSubmission;
using SPC.Infrastructure.TvanSubmission.Viettel;
using Xunit;

namespace SPC.Tests.PIT.Tvan;

public class ViettelTvanSubmissionServiceTests
{
    private static TvanSubmissionRequest Request => new(
        Certificate: new PitCertificateXmlInput(
            TaxPayerCode: "EMP-001",
            ProformaNo: "000001",
            TaxPayerTaxCode: "9876543210",
            TaxPayerName: "Nguyễn Văn A",
            Nationality: "Việt Nam",
            ResidentType: "00081",
            IdentificationNo: "012345678901",
            IssueDate: new DateTime(2020, 5, 15),
            IssuePlace: "CA TP.HCM",
            Phone: "0901234567",
            Email: "a@example.com",
            Address: "123 Lê Lợi",
            InsurancePremiums: 1_500_000m,
            CharityDonations: 0m,
            IncomePaymentMonthFrom: 1,
            IncomePaymentMonthTo: 12,
            IncomePaymentYear: 2025,
            TotalTaxableIncome: 180_000_000m,
            AmountPersonalIncomeTax: 18_000_000m,
            IncomeStillReceivable: null,
            IncomeType: "Tiền lương",
            Note: null,
            RelatedProformaNo: null,
            RelatedFormNo: null),
        Settings: new PitSettingsXmlInput(
            OrganizationTaxCode: "0100109106-998",
            OrganizationName: "Công ty Test",
            OrganizationAddress: null, OrganizationPhone: null, OrganizationEmail: null,
            SenderCode: "VCTY0001",
            XmlSchemaVersion: "2.0.0",
            XmlMessageTypeCode: "203"),
        SignedXml: null);

    [Fact]
    public async Task Logs_in_once_then_returns_invoice_metadata_on_success()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600,"token_type":"Bearer"}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"description":null,"result":{"supplierTaxCode":"0100109106-998","invoiceNo":"CT/25E113","transactionID":"174952110424264570","reservationCode":"DN9EEB8GDTCZKEX","codeOfTax":null}}"""));

        var svc = BuildService(handler);
        var result = await svc.SubmitAsync(Request);

        result.Accepted.Should().BeTrue();
        result.InvoiceNo.Should().Be("CT/25E113");
        result.TransactionId.Should().Be("174952110424264570");
        result.ReservationCode.Should().Be("DN9EEB8GDTCZKEX");
        // codeOfTax is null → fallback to reservationCode
        result.CqtCode.Should().Be("DN9EEB8GDTCZKEX");
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Prefers_codeOfTax_over_reservationCode_when_present()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/25E1","codeOfTax":"M2-25-CQT-42","reservationCode":"RES-1"}}"""));

        var svc = BuildService(handler);
        var result = await svc.SubmitAsync(Request);

        result.CqtCode.Should().Be("M2-25-CQT-42");
    }

    [Fact]
    public async Task Reuses_cached_token_on_second_submission()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/25E1"}}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/25E2"}}"""));

        var svc = BuildService(handler);
        (await svc.SubmitAsync(Request)).InvoiceNo.Should().Be("CT/25E1");
        (await svc.SubmitAsync(Request)).InvoiceNo.Should().Be("CT/25E2");
        handler.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task Retries_submit_after_401_with_fresh_token()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"old","expires_in":3600}"""),
            (HttpStatusCode.Unauthorized, "stale token"),
            (HttpStatusCode.OK, """{"access_token":"new","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/NEW"}}"""));

        var svc = BuildService(handler);
        var result = await svc.SubmitAsync(Request);

        result.Accepted.Should().BeTrue();
        result.InvoiceNo.Should().Be("CT/NEW");
        handler.CallCount.Should().Be(4);
    }

    [Fact]
    public async Task Maps_business_rejection_to_not_accepted()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":"INVALID_MST","description":"MST không hợp lệ"}"""));

        var svc = BuildService(handler);
        var result = await svc.SubmitAsync(Request);

        result.Accepted.Should().BeFalse();
        result.RejectReason.Should().Be("MST không hợp lệ");
    }

    [Fact]
    public async Task Maps_non_success_http_to_reject_with_body()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.BadGateway, "upstream boom"));

        var svc = BuildService(handler);
        var result = await svc.SubmitAsync(Request);

        result.Accepted.Should().BeFalse();
        result.RejectReason.Should().Contain("502");
        result.Raw.Should().Contain("upstream boom");
    }

    [Fact]
    public async Task Throws_when_credentials_missing()
    {
        var handler = new RecordingHandler();
        var svc = BuildService(handler, cfg => { cfg.Username = ""; cfg.Password = ""; });

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SubmitAsync(Request));
    }

    [Fact]
    public async Task Test_connection_returns_success_when_login_succeeds()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""));

        var svc = BuildService(handler);
        var result = await svc.TestConnectionAsync(new ViettelOptions
        {
            BaseUrl = "https://viettel.test",
            LoginPath = "/auth/login",
            SubmitPath = "/submit/{mst}",
            Username = "acme",
            Password = "s3cret",
            SupplierTaxCode = "0100109106-998",
            InvoiceSeries = "CT/25E"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Login succeeded");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Test_connection_returns_failure_when_login_fails()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.Unauthorized, "bad credentials"));

        var svc = BuildService(handler);
        var result = await svc.TestConnectionAsync(new ViettelOptions
        {
            BaseUrl = "https://viettel.test",
            LoginPath = "/auth/login",
            SubmitPath = "/submit/{mst}",
            Username = "acme",
            Password = "wrong",
            SupplierTaxCode = "0100109106-998",
            InvoiceSeries = "CT/25E"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Posts_cookie_header_and_expected_json_body()
    {
        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"cookie-val","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/1"}}"""));

        var svc = BuildService(handler);
        await svc.SubmitAsync(Request);

        var submit = handler.Requests[1];
        submit.Headers.TryGetValues("Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().ContainSingle(v => v == "access_token=cookie-val");

        var body = handler.Bodies[1];
        body.Should().Contain("\"generalInvoiceInfo\"");
        body.Should().Contain("\"taxPayerInfo\"");
        body.Should().Contain("\"incomeInfo\"");
        body.Should().Contain("\"invoiceSeries\":\"CT/25E\"");
        body.Should().Contain("\"invoiceType\":\"03/TNCN\"");
        body.Should().Contain("\"taxpayerTaxCode\":\"9876543210\"");
        body.Should().Contain("\"taxpayerResidence\":1");
        body.Should().Contain("\"paymentYear\":2025");
        body.Should().Contain("\"adjustmentType\":\"1\"");
        // totalTaxCalculationIncome = 180M - 1.5M - 0 = 178,500,000
        body.Should().Contain("\"totalTaxCalculationIncome\":178500000");
    }

    [Fact]
    public async Task Replacement_cert_sets_adjustmentType_2_and_originalInvoiceId()
    {
        var replacementRequest = Request with
        {
            Certificate = Request.Certificate with
            {
                RelatedProformaNo = "CT/25E100",
                RelatedFormNo = "03/TNCN"
            }
        };

        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/25E101"}}"""));

        var svc = BuildService(handler);
        await svc.SubmitAsync(replacementRequest);

        var body = handler.Bodies[1];
        body.Should().Contain("\"adjustmentType\":\"2\"");
        body.Should().Contain("\"originalInvoiceId\":\"CT/25E100\"");
    }

    [Fact]
    public async Task Non_resident_cert_sets_taxpayerResidence_zero()
    {
        var nonResident = Request with
        {
            Certificate = Request.Certificate with { ResidentType = "00082" }
        };

        var handler = new RecordingHandler(
            (HttpStatusCode.OK, """{"access_token":"abc","expires_in":3600}"""),
            (HttpStatusCode.OK, """{"errorCode":null,"result":{"invoiceNo":"CT/1"}}"""));

        var svc = BuildService(handler);
        await svc.SubmitAsync(nonResident);

        handler.Bodies[1].Should().Contain("\"taxpayerResidence\":0");
    }

    // --- Helpers ---

    private static ViettelTvanSubmissionService BuildService(HttpMessageHandler handler, Action<ViettelOptions>? configure = null)
    {
        var options = new ViettelOptions
        {
            BaseUrl = "https://viettel.test",
            Username = "acme",
            Password = "s3cret",
            SupplierTaxCode = "0100109106-998",
            InvoiceSeries = "CT/25E"
        };
        configure?.Invoke(options);

        var http = new HttpClient(handler);
        var provider = new StaticViettelOptionsProvider(options);
        return new ViettelTvanSubmissionService(http, provider, NullLogger<ViettelTvanSubmissionService>.Instance);
    }

    private sealed class StaticViettelOptionsProvider(ViettelOptions options) : IViettelOptionsProvider
    {
        public Task<ViettelOptions> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(options);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode code, string body)> _responses;
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> Bodies { get; } = new();
        public int CallCount { get; private set; }

        public RecordingHandler(params (HttpStatusCode, string)[] responses)
        {
            _responses = new Queue<(HttpStatusCode, string)>(responses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

            if (_responses.Count == 0)
                throw new InvalidOperationException("Unexpected request — no queued response.");
            var (code, body) = _responses.Dequeue();
            return new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

}

using ClosedXML.Excel;

// Generates two sample xlsx files for the PIT importer:
//   samples/epit-sample-valid.xlsx        — 5 all-valid rows
//   samples/epit-sample-with-errors.xlsx  — 5 rows exercising various diagnostic paths
//
// Schema mirrors the importer:
//   Sheet "Import form"
//     Row 2: section group headers (human aid)
//     Row 3: English labels (human aid)
//     Row 4: Vietnamese labels (human aid)
//     Row 5: **system field names** — what the importer reads
//     Row 6+: data

var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples");
outputDir = Path.GetFullPath(outputDir);
Directory.CreateDirectory(outputDir);

WriteWorkbook(Path.Combine(outputDir, "epit-sample-valid.xlsx"), ValidRows());
WriteWorkbook(Path.Combine(outputDir, "epit-sample-with-errors.xlsx"), ErrorRows());

Console.WriteLine($"Wrote samples to {outputDir}");

static (string system, string en, string vi, string group)[] Columns() =>
[
    //                 system field             English label                        Vietnamese label                             Group
    ("TaxPayerCode",            "Employee code",                      "Mã nhân viên",                               "TAX PAYER INFORMATION"),
    ("ProformaNo",              "Certificate no.",                    "Số chứng từ",                                 "TAX PAYER INFORMATION"),
    ("TaxPayerTaxCode",         "Personal tax code (MST)",            "MST cá nhân",                                 "TAX PAYER INFORMATION"),
    ("TaxPayerName",            "Full name",                          "Họ và tên",                                   "TAX PAYER INFORMATION"),
    ("Nationality",             "Nationality",                        "Quốc tịch",                                   "TAX PAYER INFORMATION"),
    ("ResidentType",            "Resident type code",                 "Loại đối tượng",                              "TAX PAYER INFORMATION"),
    ("IdentificationNo",        "CCCD / Passport no.",                "Số CCCD / Hộ chiếu",                          "TAX PAYER INFORMATION"),
    ("IssueDate",               "Issue date",                         "Ngày cấp",                                    "TAX PAYER INFORMATION"),
    ("IssuePlace",              "Issue place",                        "Nơi cấp",                                     "TAX PAYER INFORMATION"),
    ("Phone",                   "Phone",                              "Điện thoại",                                  "TAX PAYER INFORMATION"),
    ("Email",                   "Email",                              "Email",                                       "TAX PAYER INFORMATION"),
    ("Address",                 "Address",                            "Địa chỉ",                                     "TAX PAYER INFORMATION"),
    ("InsurancePremiums",       "Compulsory insurance premiums",      "Tổng BH bắt buộc",                            "PIT WITHHOLDING"),
    ("CharityDonations",        "Charity / humanitarian donations",   "Từ thiện, khuyến học",                        "PIT WITHHOLDING"),
    ("IncomePaymentMonthFrom",  "Month from",                         "Từ tháng",                                    "PIT WITHHOLDING"),
    ("IncomePaymentMonthTo",    "Month to",                           "Đến tháng",                                   "PIT WITHHOLDING"),
    ("IncomePaymentYear",       "Tax year",                           "Năm",                                         "PIT WITHHOLDING"),
    ("TotalTaxableIncome",      "Total taxable income (VND)",         "Tổng TN chịu thuế (VND)",                     "PIT WITHHOLDING"),
    ("AmountPersonalIncomeTax", "PIT withheld (VND)",                 "Tổng TNCN đã khấu trừ (VND)",                 "PIT WITHHOLDING"),
    ("IncomeStillReceivable",   "Remaining income (VND)",             "Tiền còn nhận (VND)",                         "PIT WITHHOLDING"),
    ("IncomeType",              "Income type",                        "Loại thu nhập (TL/TC…)",                      "PIT WITHHOLDING"),
    ("Note",                    "Note",                               "Ghi chú",                                     "PIT WITHHOLDING"),
    ("RelatedProformaNo",       "Replaces certificate no.",           "Thay thế chứng từ số",                        "REPLACE CERTIFICATE"),
    ("RelatedFormNo",           "Replaces form no.",                  "Thay thế mẫu số",                             "REPLACE CERTIFICATE"),
];

static void WriteWorkbook(string path, IReadOnlyList<Dictionary<string, object?>> rows)
{
    using var wb = new XLWorkbook();
    var sheet = wb.Worksheets.Add("Import form");

    var cols = Columns();

    // Row 2: section groups (merged across consecutive same-group columns)
    var groupStart = 1;
    for (var i = 0; i < cols.Length; i++)
    {
        var isLast = i == cols.Length - 1;
        if (isLast || cols[i + 1].group != cols[i].group)
        {
            var range = sheet.Range(2, groupStart, 2, i + 1);
            range.Merge();
            range.Value = cols[i].group;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            range.Style.Font.Bold = true;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            groupStart = i + 2;
        }
    }

    // Row 3: English
    // Row 4: Vietnamese
    // Row 5: system field (used by the importer)
    for (var i = 0; i < cols.Length; i++)
    {
        sheet.Cell(3, i + 1).Value = cols[i].en;
        sheet.Cell(4, i + 1).Value = cols[i].vi;
        sheet.Cell(5, i + 1).Value = cols[i].system;
    }
    var headerBand = sheet.Range(3, 1, 5, cols.Length);
    headerBand.Style.Font.Bold = true;
    sheet.Row(5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");

    // Data rows
    for (var r = 0; r < rows.Count; r++)
    {
        var row = rows[r];
        for (var c = 0; c < cols.Length; c++)
        {
            var key = cols[c].system;
            if (!row.TryGetValue(key, out var value) || value is null) continue;
            var cell = sheet.Cell(6 + r, c + 1);
            switch (value)
            {
                case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "yyyy-MM-dd"; break;
                case decimal m:   cell.Value = m; cell.Style.NumberFormat.Format = "#,##0";     break;
                case int n:       cell.Value = n;                                               break;
                default:          cell.Value = value.ToString();                                break;
            }
        }
    }

    sheet.Columns().AdjustToContents();
    sheet.SheetView.FreezeRows(5);
    wb.SaveAs(path);
    Console.WriteLine($"  {Path.GetFileName(path)} — {rows.Count} rows");
}

static IReadOnlyList<Dictionary<string, object?>> ValidRows()
{
    var issue = new DateTime(2020, 5, 15);
    return [
        Row("EMP-001", "0000001", "8701234567",    "Nguyễn Văn An",       "Việt Nam", "00081", "079201012345", issue,
            "CA TP.HCM",    "0901234567", "an.nguyen@example.com", "12 Lê Lợi, Q.1, TP.HCM",
            insurance: 10_500_000m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 240_000_000m, tax: 24_000_000m, still: 216_000_000m, incomeType: "TL",
            note: "Nhân viên toàn thời gian"),

        Row("EMP-002", "0000002", "8702345678901", "Trần Thị Bình",        "Việt Nam", "00081", "079201023456", issue,
            "CA TP.HCM",    "0912345678", "binh.tran@example.com", "45 Nguyễn Huệ, Q.1, TP.HCM",
            insurance: 13_200_000m, charity: 2_000_000m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 360_000_000m, tax: 45_000_000m, still: 315_000_000m, incomeType: "TL",
            note: null),

        Row("EMP-003", "0000003", "8703456789",    "Lê Hoàng Cường",      "Việt Nam", "00081", "001201034567", issue,
            "CA Hà Nội",    "0987654321", "cuong.le@example.com",  "78 Bà Triệu, Q. Hai Bà Trưng, Hà Nội",
            insurance: 8_800_000m, charity: 0m, monthFrom: 1, monthTo: 6, year: 2025,
            taxable: 110_000_000m, tax: 8_250_000m, still: 101_750_000m, incomeType: "TL",
            note: "Nghỉ việc tháng 6"),

        Row("EMP-004", "0000004", "8704567890",    "Phạm Minh Dung",      "Việt Nam", "00081", "079201045678", issue,
            "CA TP.HCM",    "0905555555", "dung.pham@example.com", "234 Cách Mạng Tháng 8, Q.3, TP.HCM",
            insurance: 12_000_000m, charity: 1_000_000m, monthFrom: 7, monthTo: 12, year: 2025,
            taxable: 180_000_000m, tax: 18_000_000m, still: 162_000_000m, incomeType: "TL",
            note: "Tuyển dụng từ tháng 7"),

        Row("EMP-005", "0000005", "8705678901",    "Hoàng Thu Em",         "Hoa Kỳ",    "00082", "A12345678",    issue,
            "Hộ chiếu",     "0931111111", "em.hoang@example.com",  "56 Hai Bà Trưng, Q.1, TP.HCM",
            insurance: 0m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 600_000_000m, tax: 120_000_000m, still: 480_000_000m, incomeType: "TL",
            note: "Cá nhân không cư trú (20% flat)"),
    ];
}

static IReadOnlyList<Dictionary<string, object?>> ErrorRows()
{
    var issue = new DateTime(2020, 5, 15);
    return [
        // OK — baseline
        Row("EMP-101", "0000101", "8701111111",    "Nguyễn Hợp Lệ",        "Việt Nam", "00081", "079201011111", issue,
            "CA TP.HCM",    "0901111111", "hople@example.com", "1 Sample St",
            insurance: 5_000_000m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 120_000_000m, tax: 6_000_000m, still: 114_000_000m, incomeType: "TL", note: null),

        // ERROR — missing TaxPayerTaxCode
        Row("EMP-102", "0000102", "",              "Lê Vô Mã",              "Việt Nam", "00081", "079201022222", issue,
            "CA TP.HCM",    "0901222222", "vomast@example.com", "2 Sample St",
            insurance: 3_000_000m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 60_000_000m, tax: 3_000_000m, still: 57_000_000m, incomeType: "TL", note: null),

        // ERROR — TaxPayerTaxCode wrong length (8 digits)
        Row("EMP-103", "0000103", "87033333",      "Trần Sai Mã",           "Việt Nam", "00081", "079201033333", issue,
            "CA TP.HCM",    "0901333333", "saima@example.com", "3 Sample St",
            insurance: 4_000_000m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 90_000_000m, tax: 4_500_000m, still: 85_500_000m, incomeType: "TL", note: null),

        // ERROR — MonthFrom > MonthTo
        Row("EMP-104", "0000104", "8704444444",    "Phạm Tháng Ngược",     "Việt Nam", "00081", "079201044444", issue,
            "CA TP.HCM",    "0901444444", "thangnguoc@example.com", "4 Sample St",
            insurance: 5_000_000m, charity: 0m, monthFrom: 10, monthTo: 3, year: 2025,
            taxable: 50_000_000m, tax: 2_500_000m, still: 47_500_000m, incomeType: "TL", note: null),

        // ERROR — missing ProformaNo and TaxPayerName
        Row("EMP-105", "", "8705555555",            "",                      "Việt Nam", "00081", "079201055555", issue,
            "CA TP.HCM",    "0901555555", "thieu@example.com", "5 Sample St",
            insurance: 5_000_000m, charity: 0m, monthFrom: 1, monthTo: 12, year: 2025,
            taxable: 70_000_000m, tax: 3_500_000m, still: 66_500_000m, incomeType: "TL", note: null),
    ];
}

static Dictionary<string, object?> Row(
    string code, string proforma, string taxCode, string name,
    string nat, string resident, string idNo, DateTime issue,
    string? issuePlace, string? phone, string? email, string? address,
    decimal? insurance, decimal? charity,
    int? monthFrom, int? monthTo, int year,
    decimal taxable, decimal tax,
    decimal? still, string? incomeType, string? note) =>
    new()
    {
        ["TaxPayerCode"] = code,
        ["ProformaNo"] = proforma,
        ["TaxPayerTaxCode"] = taxCode,
        ["TaxPayerName"] = name,
        ["Nationality"] = nat,
        ["ResidentType"] = resident,
        ["IdentificationNo"] = idNo,
        ["IssueDate"] = issue,
        ["IssuePlace"] = issuePlace,
        ["Phone"] = phone,
        ["Email"] = email,
        ["Address"] = address,
        ["InsurancePremiums"] = insurance,
        ["CharityDonations"] = charity,
        ["IncomePaymentMonthFrom"] = monthFrom,
        ["IncomePaymentMonthTo"] = monthTo,
        ["IncomePaymentYear"] = year,
        ["TotalTaxableIncome"] = taxable,
        ["AmountPersonalIncomeTax"] = tax,
        ["IncomeStillReceivable"] = still,
        ["IncomeType"] = incomeType,
        ["Note"] = note,
        ["RelatedProformaNo"] = null,
        ["RelatedFormNo"] = null,
    };

namespace SPC.BO.PIT.Xml;

/// <summary>
/// All QĐ 1306 element names used by the PIT withholding-certificate XML, gathered in one place
/// so corrections from the official Phụ lục only need to change this file.
/// </summary>
public static class PitXmlSchema
{
    // Envelope (TTChung — common header)
    public const string TDiep = "TDiep";
    public const string TTChung = "TTChung";
    public const string PBan = "PBan";
    public const string MNGui = "MNGui";
    public const string MNNhan = "MNNhan";
    public const string MLTDiep = "MLTDiep";
    public const string MTDiep = "MTDiep";
    public const string MTDTChieu = "MTDTChieu";
    public const string MST = "MST";
    public const string SLuong = "SLuong";

    // Data payload
    public const string DLieu = "DLieu";

    // PIT withholding-certificate document (TBD — confirm from Phụ lục)
    public const string CTDTKhauTru = "CTDTKhauTru";
    public const string DLCT = "DLCT";
    public const string TTChungCTu = "TTChungCTu";
    public const string TTNNT = "TTNNT";      // Taxpayer info (người nộp thuế)
    public const string TTTo = "TTTo";        // Organization info
    public const string TTKhauTru = "TTKhauTru"; // Withholding info

    // Tax authority receiver code for this sender
    public const string TaxAuthorityReceiver = "TCT";

    // Taxpayer fields
    public const string TName = "TNNT";              // Tên NNT
    public const string TMST = "MST";                 // MST
    public const string TNat = "QTich";               // Quốc tịch
    public const string TResType = "LoaiNNT";         // Loại (resident/non-resident)
    public const string TIdNo = "SoCMT";              // Số CMT/CCCD/Hộ chiếu
    public const string TIssueDate = "NgayCap";
    public const string TIssuePlace = "NoiCap";
    public const string TPhone = "SDT";
    public const string TEmail = "Email";
    public const string TAddress = "DChi";

    // Withholding fields
    public const string TTaxableIncome = "TongTNCTinhThue";
    public const string TTaxWithheld = "TongTNTTNCN";
    public const string TInsurance = "TongBHBB";
    public const string TCharity = "KhoanDongGop";
    public const string TPeriodFrom = "ThoiGianTuThang";
    public const string TPeriodTo = "ThoiGianDenThang";
    public const string TPeriodYear = "KyTinhThueNam";
    public const string TIncomeType = "LoaiTN";
    public const string TNote = "GhiChu";
    public const string TStillReceivable = "TienConNhan";

    // Organization (issuer) fields
    public const string OName = "Ten";
    public const string OAddress = "DChi";
    public const string OPhone = "SDT";
    public const string OEmail = "Email";

    // Certificate numbering
    public const string CertFormNo = "KHMau";
    public const string CertSeries = "KyHieu";
    public const string CertNumber = "So";

    // Related (for replacement certificates)
    public const string RelatedCert = "CTuLQuan";
    public const string RelatedFormNo = "KHMau";
    public const string RelatedNumber = "So";

    // W3C XML Digital Signature placeholder
    public const string Signature = "Signature";
    public const string SignatureNs = "http://www.w3.org/2000/09/xmldsig#";
}

using System.Globalization;

namespace SPC.BO.PIT.Xml;

/// <summary>
/// Vietnam-timezone date/datetime formatting per QĐ 1306 §I: dates as <c>yyyy-MM-dd</c>,
/// datetimes as <c>yyyy-MM-ddTHH:mm:ss</c> in GMT+7 (Asia/Ho_Chi_Minh).
/// </summary>
public static class VnDateTime
{
    private static readonly TimeZoneInfo VnTz = ResolveVietnamTimeZone();

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        // "Asia/Ho_Chi_Minh" on Linux/macOS, "SE Asia Standard Time" on Windows.
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Vietnam+7", TimeSpan.FromHours(7), "Vietnam+7", "Vietnam+7");
    }

    public static string FormatDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(value, VnTz)
            : value;
        return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string FormatDateTime(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(value, VnTz)
            : value;
        return local.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }
}

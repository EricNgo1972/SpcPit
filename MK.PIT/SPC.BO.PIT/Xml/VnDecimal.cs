using System.Globalization;

namespace SPC.BO.PIT.Xml;

/// <summary>
/// Decimal formatting per QĐ 1306 §I: up to 21 total digits and 6 decimal places,
/// invariant culture, trailing zeros stripped, never in scientific notation.
/// </summary>
public static class VnDecimal
{
    private const int MaxTotalDigits = 21;

    public static string Format(decimal value)
    {
        var text = value.ToString("0.######", CultureInfo.InvariantCulture);
        var digitCount = text.Count(char.IsDigit);
        if (digitCount > MaxTotalDigits)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Decimal exceeds QĐ 1306 limit of {MaxTotalDigits} digits: {text}");
        return text;
    }

    public static string Format(decimal? value) => value.HasValue ? Format(value.Value) : string.Empty;
}

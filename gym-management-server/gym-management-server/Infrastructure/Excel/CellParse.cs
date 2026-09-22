using System.Globalization;
using gym_management_server.Infrastructure.Enums;

namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// Cell-to-value conversions shared by every import sheet. Each throws a
    /// FormatException whose message is shown to the user verbatim, so the messages
    /// are Vietnamese and say what was expected.
    ///
    /// <see cref="ExcelReader"/> no longer hands parsers display text: it switches on
    /// <c>IXLCell.DataType</c> and emits culture-invariant strings (see its CellText).
    /// A numeric cell arrives as <c>double.ToString(InvariantCulture)</c> — "." is
    /// always the decimal point there, never a thousands separator, and it can be
    /// scientific notation (e.g. "1E+16") outside roughly [1e-5, 1e15). Only a genuine
    /// text cell (a user typing "1.500.000" by hand) carries Vietnamese/US thousands
    /// separators. So every numeric parser here tries an invariant parse FIRST — which
    /// handles every real numeric cell exactly, exponent included — and only falls back
    /// to stripping separators when that fails, which is the signal that the cell was
    /// text after all.
    /// </summary>
    public static class CellParse
    {
        // yyyy-MM-ddTHH:mm:ss is what ExcelReader emits for a date cell whose time-of-day
        // is non-zero; the rest are what a user might type in a text cell.
        private static readonly string[] DateFormats =
        [
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"
        ];

        private const NumberStyles NumericStyle = NumberStyles.Number | NumberStyles.AllowExponent;

        public static DateTime Date(string raw)
        {
            if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var value))
                return value;
            throw new FormatException($"\"{raw}\" không phải ngày hợp lệ. Định dạng mong đợi: dd/MM/yyyy.");
        }

        public static decimal Money(string raw)
        {
            // A numeric Excel cell arrives already culture-invariant (e.g. "1234.5" or
            // "1E+16"); parse it directly first so "." is read as the decimal point.
            if (decimal.TryParse(raw, NumericStyle, CultureInfo.InvariantCulture, out var invariant))
                return invariant;

            // Only reachable for a genuine text cell: a real spreadsheet may contain
            // "1.500.000" or "1,500,000" typed by hand, both meaning the same thing.
            var digits = raw.Replace(".", "").Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;

            throw new FormatException($"\"{raw}\" không phải số tiền hợp lệ.");
        }

        public static int Integer(string raw)
        {
            if (int.TryParse(raw, NumericStyle, CultureInfo.InvariantCulture, out var invariant))
                return invariant;

            var digits = raw.Replace(".", "").Replace(",", "").Replace(" ", "");
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;

            throw new FormatException($"\"{raw}\" phải là số nguyên.");
        }

        public static bool Boolean(string raw) => raw.Trim().ToLowerInvariant() switch
        {
            "có" or "co" or "true" or "1" or "x" => true,
            "không" or "khong" or "false" or "0" or "" => false,
            _ => throw new FormatException($"\"{raw}\" phải là Có hoặc Không."),
        };

        public static TEnum Enum<TEnum>(string raw) where TEnum : struct, Enum
        {
            if (EnumLabel.TryParseLabel<TEnum>(raw, out var value)) return value;

            var allowed = string.Join(", ", EnumLabel.Describe<TEnum>().Select(v => v.Label));
            throw new FormatException($"\"{raw}\" không hợp lệ. Giá trị cho phép: {allowed}.");
        }

        public static string Phone(string raw)
        {
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length is < 9 or > 11)
                throw new FormatException($"\"{raw}\" không phải số điện thoại hợp lệ.");
            return digits;
        }

        public static string Email(string raw)
        {
            if (!raw.Contains('@') || raw.StartsWith('@') || raw.EndsWith('@'))
                throw new FormatException($"\"{raw}\" không phải email hợp lệ.");
            return raw;
        }
    }
}

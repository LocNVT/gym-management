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
    /// scientific notation (e.g. "1E+16") outside roughly [1e-5, 1e15). A genuine text
    /// cell (a user typing "1.500.000", or "1,5" meaning 1.5, by hand) may carry either
    /// a Vietnamese/US thousands separator or a Vietnamese decimal comma — the same
    /// punctuation mark, two different meanings. <see cref="NormalizeNumber"/> decides
    /// which one a given separator means from its digit grouping, rather than letting
    /// <c>NumberStyles.AllowThousands</c> guess (it does not enforce 3-digit grouping,
    /// so it silently reads "1,5" as 15).
    /// </summary>
    public static class CellParse
    {
        // yyyy-MM-ddTHH:mm:ss is what ExcelReader emits for a date cell whose time-of-day
        // is non-zero; the rest are what a user might type in a text cell.
        private static readonly string[] DateFormats =
        [
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"
        ];

        // No AllowThousands: by the time a string reaches TryParse it has already been
        // disambiguated by NormalizeNumber, so a leftover "," or "." group separator
        // here is a bug, not something to guess about.
        private const NumberStyles NumericStyle = NumberStyles.Float;

        public static DateTime Date(string raw)
        {
            if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var value))
                return value;
            throw new FormatException($"\"{raw}\" không phải ngày hợp lệ. Định dạng mong đợi: dd/MM/yyyy.");
        }

        public static decimal Money(string raw)
        {
            var normalized = NormalizeNumber(raw);
            if (normalized is not null &&
                decimal.TryParse(normalized, NumericStyle, CultureInfo.InvariantCulture, out var value))
                return value;

            throw new FormatException($"\"{raw}\" không phải số tiền hợp lệ.");
        }

        public static int Integer(string raw)
        {
            var normalized = NormalizeNumber(raw);
            if (normalized is not null &&
                int.TryParse(normalized, NumericStyle, CultureInfo.InvariantCulture, out var value))
                return value;

            throw new FormatException($"\"{raw}\" phải là số nguyên.");
        }

        /// <summary>
        /// Turns a cell's text into an unambiguous invariant numeric literal ("." as the
        /// only possible decimal point, no group separators), or returns null when the
        /// text's punctuation cannot be read one way without guessing. Rules, applied to
        /// the string with any leading sign set aside:
        /// <list type="bullet">
        /// <item>no "." or "," at all → already unambiguous (includes scientific
        /// notation like "1E+16", which ExcelReader emits for a large numeric cell and
        /// never combines with a thousands grouping) — passed through as-is;</item>
        /// <item>one separator character used, and EVERY group after the first is
        /// exactly 3 digits ("1.500.000", "1,500,000", "1.500") → a thousands
        /// separator; strip it;</item>
        /// <item>exactly one separator, with exactly one digit after it ("1234.5",
        /// "1,5") → a decimal mark; only "." vs "," differs, so normalize to ".";</item>
        /// <item>anything else — both "." and "," present, or a trailing group that is
        /// neither a clean 3-digit thousands group nor a single decimal digit (e.g.
        /// "12,34", "1,2345") → genuinely ambiguous; the caller must reject rather than
        /// guess, exactly the failure mode this method exists to close off.</item>
        /// </list>
        /// </summary>
        private static string? NormalizeNumber(string raw)
        {
            var s = raw.Trim();
            if (s.Length == 0) return null;

            // Scientific notation never carries a thousands grouping (ExcelReader is the
            // only source of it, and it only ever emits a single "." decimal point before
            // the exponent), so hand it to TryParse unchanged rather than splitting it.
            if (s.IndexOf('E') >= 0 || s.IndexOf('e') >= 0) return s;

            var sign = "";
            if (s[0] is '-' or '+')
            {
                sign = s[0] == '-' ? "-" : "";
                s = s[1..];
            }

            var hasDot = s.Contains('.');
            var hasComma = s.Contains(',');

            if (!hasDot && !hasComma)
                return s.Length > 0 && s.All(char.IsDigit) ? sign + s : null;
            if (hasDot && hasComma)
                return null; // both marks present in one cell — no way to read this without guessing

            var parts = s.Split(hasDot ? '.' : ',');
            if (parts.Length < 2 || parts.Any(p => p.Length == 0 || !p.All(char.IsDigit)))
                return null;

            if (parts.Skip(1).All(p => p.Length == 3))
                return sign + string.Concat(parts); // thousands grouping, e.g. 1.500.000 / 1.500

            if (parts.Length == 2 && parts[1].Length == 1)
                return sign + parts[0] + "." + parts[1]; // a single decimal digit, e.g. 1234.5 / 1,5

            return null; // e.g. 12,34 or 1,2345 — not a clean thousands group, not a single decimal digit
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

            // The same subscriber can show up as "+84901234567", "84901234567" (the "+"
            // is already gone, stripped as non-digit) or the domestic "0901234567".
            // Collapse the international prefix down to the domestic leading 0 so all
            // three become the SAME string: Task 3's duplicate detection compares phone
            // strings exactly, so leaving two shapes of one number both reachable would
            // let the same person import twice with neither copy flagged as a duplicate.
            if (digits.Length is 11 or 12 && digits.StartsWith("84"))
                digits = "0" + digits[2..];

            // Vietnam's unified numbering plan: a mobile number is 10 digits with a
            // leading 0 (0 + a 9-digit subscriber number). A landline number is 11
            // digits with a leading 0 (0 + a 2-3 digit area code + a 7-8 digit local
            // number, e.g. Hanoi "024" + 8 digits). Both are real, current formats;
            // nothing else is — in particular the old 9-digit floor accepted a number
            // one digit short of a real mobile number.
            if (digits.Length is not (10 or 11) || digits[0] != '0')
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

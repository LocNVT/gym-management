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

        /// <summary>
        /// Enforces an nvarchar(N) column's length before the value ever reaches SaveChanges.
        /// A dry run that used no length check at all was "clean" for a string SQL Server would
        /// go on to reject at commit time with a truncation error that names no row and no
        /// column — this is what stands between the user and that experience: the same
        /// Vietnamese, row/column-addressed error every other CellParse failure produces.
        /// </summary>
        public static string Text(string raw, int maxLength, string columnName)
        {
            if (raw.Length > maxLength)
                throw new FormatException(
                    $"\"{columnName}\" dài {raw.Length} ký tự, vượt quá giới hạn {maxLength} ký tự.");
            return raw;
        }

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
        /// <item>no ".", "," or " " at all → already unambiguous (includes scientific
        /// notation like "1E+16", which ExcelReader emits for a large numeric cell and
        /// never combines with a thousands grouping) — passed through as-is;</item>
        /// <item>exactly one of ".", "," or " " used, and EVERY group after the first is
        /// exactly 3 digits ("1.500.000", "1,500,000", "1 500 000", "1.500") → a
        /// thousands separator; strip it;</item>
        /// <item>exactly one "." or "," used (never " " — a space is never a decimal
        /// mark), with a trailing group that is NOT exactly 3 digits ("1234.5", "1,5",
        /// "250000.25", "1,2345") → a decimal mark; only "." vs "," differs, so
        /// normalize to ".". A 3-digit trailing group is deliberately NOT read this way
        /// — see the accepted-ambiguity note below — so it falls into the thousands
        /// branch above instead, never here.</item>
        /// <item>anything else — more than one of ".", "," and " " present in the same
        /// cell, more than two groups with inconsistent grouping (e.g. "1.234.5"), or a
        /// space-separated trailing group that isn't a clean 3-digit thousands group
        /// (e.g. "1 5", "1 500.000") → genuinely ambiguous; the caller must reject
        /// rather than guess, exactly the failure mode this method exists to close off.
        /// </item>
        /// </list>
        /// Known, accepted ambiguity: a numeric Excel cell whose fractional part happens
        /// to be exactly 3 digits (e.g. a price of 1234.567, which
        /// double.ToString(InvariantCulture) renders as "1234.567") is indistinguishable
        /// from a thousands-grouped "1.234.567" under this rule and is read as 1234567.
        /// Accepted deliberately: the only numeric import columns are a VND price (VND
        /// has no sub-unit smaller than 1 đồng, so a three-decimal-digit price is
        /// never a real value) or one of two integer counts, so the case this would
        /// actually get wrong does not occur. Two- and four-digit fractions (and every
        /// other length except exactly 3) are NOT covered by this ambiguity — a price
        /// like 250000.25 or 1234.25 is common (decimal(18,2) allows exactly two
        /// fraction digits) and must round-trip through export→import, so those are read
        /// as decimals, not rejected.
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
            var hasSpace = s.Contains(' ');
            var separatorCount = (hasDot ? 1 : 0) + (hasComma ? 1 : 0) + (hasSpace ? 1 : 0);

            if (separatorCount == 0)
                return s.Length > 0 && s.All(char.IsDigit) ? sign + s : null;
            if (separatorCount > 1)
                return null; // more than one kind of separator in one cell — no way to read this without guessing

            var sep = hasDot ? '.' : hasComma ? ',' : ' ';
            var parts = s.Split(sep);
            if (parts.Length < 2 || parts.Any(p => p.Length == 0 || !p.All(char.IsDigit)))
                return null;

            if (parts.Skip(1).All(p => p.Length == 3))
                return sign + string.Concat(parts); // thousands grouping, e.g. 1.500.000 / 1 500 000 / 1.500

            // A space is only ever a thousands mark, never a decimal mark, so it gets no
            // decimal branch: "1 5" falls through to ambiguous. A trailing group of
            // exactly 3 digits already returned above (thousands grouping), so reaching
            // here with parts.Length == 2 means the trailing group is NOT 3 digits —
            // i.e. unambiguously a decimal fraction of some other length (1, 2, 4, ...).
            if (sep != ' ' && parts.Length == 2 && parts[1].Length != 3)
                return sign + parts[0] + "." + parts[1]; // a decimal mark, e.g. 1234.5 / 1,5 / 250000.25 / 1,2345

            return null; // e.g. "1 5", "1 500.000", "1.234.5" — genuinely ambiguous grouping
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

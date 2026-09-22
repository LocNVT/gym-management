using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace gym_management_server.Infrastructure.Enums
{
    public record EnumValueInfo(byte Value, string Name, string Label);

    /// <summary>
    /// Reads the [Description] label off enum members. Results are cached per enum type —
    /// this runs once per cell of an exported spreadsheet, so reflecting every time would show.
    /// </summary>
    public static class EnumLabel
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<EnumValueInfo>> Cache = new();

        public static IReadOnlyList<EnumValueInfo> Describe<TEnum>() where TEnum : struct, Enum =>
            Cache.GetOrAdd(typeof(TEnum), static type =>
                type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Select(f => new EnumValueInfo(
                        Value: Convert.ToByte(f.GetRawConstantValue()),
                        Name: f.Name,
                        Label: f.GetCustomAttribute<DescriptionAttribute>()?.Description ?? f.Name))
                    .OrderBy(v => v.Value)
                    .ToList());

        public static string ToLabel<TEnum>(this TEnum value) where TEnum : struct, Enum
        {
            var numeric = Convert.ToByte(value);
            return Describe<TEnum>().FirstOrDefault(v => v.Value == numeric)?.Label ?? numeric.ToString();
        }

        public static string ToLabel<TEnum>(this TEnum? value) where TEnum : struct, Enum =>
            value.HasValue ? value.Value.ToLabel() : string.Empty;

        public static bool TryParseLabel<TEnum>(string label, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrWhiteSpace(label)) return false;

            var needle = label.Trim();
            var match = Describe<TEnum>().FirstOrDefault(v =>
                string.Equals(v.Label, needle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Name, needle, StringComparison.OrdinalIgnoreCase));

            if (match is null) return false;
            result = (TEnum)Enum.ToObject(typeof(TEnum), match.Value);
            return true;
        }
    }
}

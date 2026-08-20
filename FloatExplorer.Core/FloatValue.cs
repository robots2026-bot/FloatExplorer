using System.Globalization;

namespace FloatExplorer.Core;

public sealed record FloatValue(
    FloatFormat Format,
    ulong RawBits,
    bool IsNegative,
    int ExponentField,
    ulong FractionField,
    FloatCategory Category,
    double Value,
    int? UnbiasedExponent,
    double Ulp)
{
    public string Hex => $"0x{RawBits.ToString($"X{Format.HexDigits}", CultureInfo.InvariantCulture)}";
    public string BitString => Convert.ToString((long)RawBits, 2).PadLeft(Format.TotalBits, '0');
    public string CategoryName => Category switch
    {
        FloatCategory.Zero => IsNegative ? "Negative Zero" : "Zero",
        FloatCategory.Subnormal => "Subnormal",
        FloatCategory.Normal => "Normal",
        FloatCategory.Infinity => IsNegative ? "Negative Infinity" : "Infinity",
        _ => "NaN"
    };
}

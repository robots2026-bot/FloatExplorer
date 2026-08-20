using System.Globalization;

namespace FloatExplorer.Core;

public static class FloatCodec
{
    public static FloatValue Quantize(double value, FloatFormat format, FloatRoundingMode roundingMode = FloatRoundingMode.ToNearestEven)
    {
        bool negative = IsNegative(value);
        ulong signBits = negative ? 1UL << format.SignShift : 0UL;

        if (double.IsNaN(value)) return Decode(signBits | ((ulong)format.ExponentMask << format.FractionBits) | format.FractionMask, format);
        if (double.IsInfinity(value)) return QuantizeInfinity(negative, format);
        if (value == 0d) return Decode(signBits, format);

        double magnitude = Math.Abs(value);
        int binaryExponent = Math.ILogB(magnitude);

        if (binaryExponent < format.MinimumNormalExponent)
        {
            double subnormalStep = Math.ScaleB(1d, format.MinimumNormalExponent - format.FractionBits);
            ulong fraction = RoundPositive(magnitude / subnormalStep, negative, roundingMode);
            if (fraction == 0) return Decode(signBits, format);
            if (fraction >= 1UL << format.FractionBits) return Decode(signBits | (1UL << format.FractionBits), format);
            return Decode(signBits | fraction, format);
        }

        double significand = Math.ScaleB(magnitude, -binaryExponent);
        ulong roundedFraction = RoundPositive((significand - 1d) * (1UL << format.FractionBits), negative, roundingMode);
        if (roundedFraction >= 1UL << format.FractionBits)
        {
            roundedFraction = 0;
            binaryExponent++;
        }

        int exponentField = binaryExponent + format.ExponentBias;
        if (exponentField > format.MaxFiniteExponentField || exponentField == format.MaxFiniteExponentField && roundedFraction > (ulong)format.MaxFiniteFractionField)
        {
            return Overflow(negative, format, roundingMode);
        }

        ulong raw = signBits | ((ulong)exponentField << format.FractionBits) | roundedFraction;
        return Decode(raw, format);
    }

    public static FloatValue Decode(ulong rawBits, FloatFormat format)
    {
        rawBits &= format.RawMask;
        bool negative = ((rawBits >> format.SignShift) & 1UL) != 0;
        int exponentField = (int)((rawBits >> format.FractionBits) & (ulong)format.ExponentMask);
        ulong fraction = rawBits & format.FractionMask;
        FloatCategory category;
        double magnitude;
        int? unbiasedExponent = null;
        double ulp;

        if (exponentField == 0)
        {
            category = fraction == 0 ? FloatCategory.Zero : FloatCategory.Subnormal;
            magnitude = Math.ScaleB((double)fraction, format.MinimumNormalExponent - format.FractionBits);
            ulp = Math.ScaleB(1d, format.MinimumNormalExponent - format.FractionBits);
        }
        else if (IsSpecial(exponentField, fraction, format, out category))
        {
            magnitude = category == FloatCategory.Infinity ? double.PositiveInfinity : double.NaN;
            ulp = double.NaN;
        }
        else
        {
            category = FloatCategory.Normal;
            unbiasedExponent = exponentField - format.ExponentBias;
            magnitude = Math.ScaleB(1d + fraction / (double)(1UL << format.FractionBits), unbiasedExponent.Value);
            ulp = Math.ScaleB(1d, unbiasedExponent.Value - format.FractionBits);
        }

        double value = negative ? -magnitude : magnitude;
        return new(format, rawBits, negative, exponentField, fraction, category, value, unbiasedExponent, ulp);
    }

    public static bool TryParseInput(string text, FloatInputKind kind, FloatFormat sourceFormat, out double value, out string? error)
    {
        text = text.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (kind == FloatInputKind.Decimal)
        {
            if (TryParseNamedValue(text, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                error = null;
                return true;
            }

            error = "请输入有效十进制数，例如 0.1、1e-8、Infinity 或 NaN。";
            return false;
        }

        if (TryParseRawBits(text, kind, sourceFormat, out ulong rawBits, out error))
        {
            value = Decode(rawBits, sourceFormat).Value;
            return true;
        }

        value = 0d;
        return false;
    }

    public static bool TryParseRawBits(string text, FloatInputKind kind, FloatFormat sourceFormat, out ulong rawBits, out string? error)
    {
        text = text.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (kind == FloatInputKind.Decimal)
        {
            rawBits = 0UL;
            error = "原始位串必须选择 Hex 或二进制输入解释。";
            return false;
        }

        string digits = kind == FloatInputKind.HexBits && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text;
        if (kind == FloatInputKind.BinaryBits && text.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) digits = text[2..];
        int numberBase = kind == FloatInputKind.HexBits ? 16 : 2;
        try
        {
            rawBits = Convert.ToUInt64(digits, numberBase);
            if ((rawBits & ~sourceFormat.RawMask) != 0)
            {
                rawBits = 0UL;
                error = $"位串超出 {sourceFormat.DisplayName} 的 {sourceFormat.TotalBits} 位范围。";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            rawBits = 0UL;
            error = kind == FloatInputKind.HexBits ? "请输入有效十六进制位串，例如 0x3F800000。" : "请输入仅由 0 和 1 组成的位串。";
            return false;
        }
    }

    public static double PreviousRepresentable(FloatValue value) => Adjacent(value, towardPositive: false).Value;
    public static double NextRepresentable(FloatValue value) => Adjacent(value, towardPositive: true).Value;

    public static FloatValue Adjacent(FloatValue value, bool towardPositive)
    {
        if (value.Category == FloatCategory.NaN) return value;
        ulong signMask = 1UL << value.Format.SignShift;
        if (value.Value == 0d)
        {
            ulong raw = towardPositive ? 1UL : signMask | 1UL;
            return Decode(raw, value.Format);
        }

        ulong nextRaw;
        if (value.IsNegative) nextRaw = towardPositive ? value.RawBits - 1UL : value.RawBits + 1UL;
        else nextRaw = towardPositive ? value.RawBits + 1UL : value.RawBits - 1UL;
        FloatValue candidate = Decode(nextRaw, value.Format);
        if (candidate.Category == FloatCategory.NaN) return value;
        return candidate;
    }

    private static FloatValue QuantizeInfinity(bool negative, FloatFormat format)
    {
        if (format.SupportsInfinity)
        {
            ulong sign = negative ? 1UL << format.SignShift : 0UL;
            return Decode(sign | ((ulong)format.ExponentMask << format.FractionBits), format);
        }

        return Decode(MaxFiniteRaw(negative, format), format);
    }

    private static FloatValue Overflow(bool negative, FloatFormat format, FloatRoundingMode roundingMode)
    {
        bool chooseInfinity = format.SupportsInfinity && roundingMode switch
        {
            FloatRoundingMode.ToNearestEven => true,
            FloatRoundingMode.TowardPositiveInfinity => !negative,
            FloatRoundingMode.TowardNegativeInfinity => negative,
            _ => false
        };
        return chooseInfinity ? QuantizeInfinity(negative, format) : Decode(MaxFiniteRaw(negative, format), format);
    }

    private static ulong MaxFiniteRaw(bool negative, FloatFormat format)
    {
        ulong sign = negative ? 1UL << format.SignShift : 0UL;
        ulong exponent = Convert.ToUInt64(format.MaxFiniteExponentField) << format.FractionBits;
        ulong fraction = Convert.ToUInt64(format.MaxFiniteFractionField);
        return sign | exponent | fraction;
    }

    private static bool IsSpecial(int exponentField, ulong fraction, FloatFormat format, out FloatCategory category)
    {
        if (format.Id == "fp8-e4m3fn" && exponentField == format.ExponentMask && fraction == format.FractionMask)
        {
            category = FloatCategory.NaN;
            return true;
        }

        if (format.SupportsInfinity && exponentField == format.ExponentMask)
        {
            category = fraction == 0 ? FloatCategory.Infinity : FloatCategory.NaN;
            return true;
        }

        category = FloatCategory.Normal;
        return false;
    }

    private static ulong RoundPositive(double value, bool negative, FloatRoundingMode mode)
    {
        return mode switch
        {
            FloatRoundingMode.ToNearestEven => (ulong)Math.Round(value, MidpointRounding.ToEven),
            FloatRoundingMode.TowardZero => (ulong)Math.Floor(value),
            FloatRoundingMode.TowardPositiveInfinity => (ulong)(negative ? Math.Floor(value) : Math.Ceiling(value)),
            FloatRoundingMode.TowardNegativeInfinity => (ulong)(negative ? Math.Ceiling(value) : Math.Floor(value)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static bool TryParseNamedValue(string text, out double value)
    {
        if (text.Equals("inf", StringComparison.OrdinalIgnoreCase) || text.Equals("+inf", StringComparison.OrdinalIgnoreCase) || text.Equals("infinity", StringComparison.OrdinalIgnoreCase) || text.Equals("+infinity", StringComparison.OrdinalIgnoreCase)) { value = double.PositiveInfinity; return true; }
        if (text.Equals("-inf", StringComparison.OrdinalIgnoreCase) || text.Equals("-infinity", StringComparison.OrdinalIgnoreCase)) { value = double.NegativeInfinity; return true; }
        if (text.Equals("nan", StringComparison.OrdinalIgnoreCase)) { value = double.NaN; return true; }
        value = 0d;
        return false;
    }

    private static bool IsNegative(double value) => BitConverter.DoubleToInt64Bits(value) < 0;
}

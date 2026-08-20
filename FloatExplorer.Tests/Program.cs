using FloatExplorer.Core;

int failures = 0;

CheckBits("FP32 1.0", "fp32", 1d, 0x3F800000);
CheckBits("FP32 0.1", "fp32", 0.1d, 0x3DCCCCCD);
CheckBits("FP16 0.1", "fp16", 0.1d, 0x2E66);
CheckBits("BF16 0.1", "bf16", 0.1d, 0x3DCD);
CheckBits("TF32 0.1", "tf32", 0.1d, 0x1EE66);
CheckBits("FP8 E4M3FN 0.1", "fp8-e4m3fn", 0.1d, 0x1D);
CheckBits("FP8 E5M2 0.1", "fp8-e5m2", 0.1d, 0x2E);
CheckValue("FP16 max", "fp16", 0x7BFF, 65504d);
CheckValue("E4M3FN max", "fp8-e4m3fn", 0x7E, 448d);
CheckCategory("FP32 +Infinity", "fp32", 0x7F800000, FloatCategory.Infinity);
CheckCategory("FP32 NaN", "fp32", 0x7FC00000, FloatCategory.NaN);
CheckCategory("FP16 minimum subnormal", "fp16", 0x0001, FloatCategory.Subnormal);
CheckCategory("E4M3FN NaN", "fp8-e4m3fn", 0x7F, FloatCategory.NaN);
CheckDirectedRounding();
CheckParsing();
CheckRawPayloadPreservation();
CheckAdjacent();

if (failures != 0)
{
    Console.Error.WriteLine($"FAILED: {failures} check(s)");
    return 1;
}

Console.WriteLine("PASS: all FloatExplorer core checks");
return 0;

void CheckBits(string name, string formatId, double input, ulong expected)
{
    FloatValue actual = FloatCodec.Quantize(input, Format(formatId));
    Check(name, actual.RawBits == expected, $"expected 0x{expected:X}, got {actual.Hex}");
}

void CheckValue(string name, string formatId, ulong raw, double expected)
{
    FloatValue actual = FloatCodec.Decode(raw, Format(formatId));
    Check(name, actual.Value == expected, $"expected {expected:R}, got {actual.Value:R}");
}

void CheckCategory(string name, string formatId, ulong raw, FloatCategory expected)
{
    FloatValue actual = FloatCodec.Decode(raw, Format(formatId));
    Check(name, actual.Category == expected, $"expected {expected}, got {actual.Category}");
}

void CheckDirectedRounding()
{
    FloatFormat fp16 = Format("fp16");
    double input = 1d + Math.ScaleB(1d, -11);
    Check("ties-to-even", FloatCodec.Quantize(input, fp16).RawBits == 0x3C00, "midpoint should round to even 1.0");
    Check("toward positive", FloatCodec.Quantize(input, fp16, FloatRoundingMode.TowardPositiveInfinity).RawBits == 0x3C01, "positive midpoint should round upward");
    Check("negative toward -infinity", FloatCodec.Quantize(-input, fp16, FloatRoundingMode.TowardNegativeInfinity).RawBits == 0xBC01, "negative midpoint should increase magnitude");
}

void CheckParsing()
{
    FloatFormat fp32 = Format("fp32");
    Check("parse decimal", FloatCodec.TryParseInput("0.1", FloatInputKind.Decimal, fp32, out double decimalValue, out _) && decimalValue == 0.1d, "decimal parse failed");
    Check("parse hex bits", FloatCodec.TryParseInput("0x3F800000", FloatInputKind.HexBits, fp32, out double hexValue, out _) && hexValue == 1d, "hex parse failed");
    Check("parse binary bits", FloatCodec.TryParseInput("00111111100000000000000000000000", FloatInputKind.BinaryBits, fp32, out double binaryValue, out _) && binaryValue == 1d, "binary parse failed");
    Check("reject oversized bits", !FloatCodec.TryParseInput("0x1FFFF", FloatInputKind.HexBits, Format("fp16"), out _, out _), "oversized raw value was accepted");
}

void CheckAdjacent()
{
    FloatValue one = FloatCodec.Quantize(1d, Format("fp16"));
    Check("FP16 next after 1", FloatCodec.NextRepresentable(one) == 1.0009765625d, "wrong next representable value");
    Check("FP16 previous before 1", FloatCodec.PreviousRepresentable(one) == 0.99951171875d, "wrong previous representable value");
}

void CheckRawPayloadPreservation()
{
    FloatFormat fp32 = Format("fp32");
    Check("parse raw NaN payload", FloatCodec.TryParseRawBits("0x7FC12345", FloatInputKind.HexBits, fp32, out ulong raw, out _) && FloatCodec.Decode(raw, fp32).RawBits == 0x7FC12345, "raw NaN payload was not preserved");
}

FloatFormat Format(string id) => FloatFormat.All.Single(format => format.Id == id);

void Check(string name, bool condition, string detail)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {name}");
        return;
    }

    failures++;
    Console.Error.WriteLine($"FAIL: {name}: {detail}");
}

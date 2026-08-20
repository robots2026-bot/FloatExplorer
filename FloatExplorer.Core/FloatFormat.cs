namespace FloatExplorer.Core;

public sealed record FloatFormat(
    string Id,
    string DisplayName,
    int ExponentBits,
    int FractionBits,
    int ExponentBias,
    bool SupportsInfinity,
    int MaxFiniteExponentField,
    int MaxFiniteFractionField,
    string Description)
{
    public int TotalBits => 1 + ExponentBits + FractionBits;
    public int SignShift => ExponentBits + FractionBits;
    public int ExponentMask => (1 << ExponentBits) - 1;
    public ulong FractionMask => (1UL << FractionBits) - 1UL;
    public ulong RawMask => (1UL << TotalBits) - 1UL;
    public int HexDigits => (TotalBits + 3) / 4;
    public int MinimumNormalExponent => 1 - ExponentBias;
    public int MaximumFiniteExponent => MaxFiniteExponentField - ExponentBias;

    public static IReadOnlyList<FloatFormat> All { get; } =
    [
        new("fp32", "FP32", 8, 23, 127, true, 254, 0x7FFFFF, "IEEE 754 binary32：通用单精度存储与计算格式。"),
        new("fp16", "FP16", 5, 10, 15, true, 30, 0x3FF, "IEEE 754 binary16：精度较高，但指数范围较窄。"),
        new("bf16", "BF16", 8, 7, 127, true, 254, 0x7F, "BFloat16：保留 FP32 的指数范围，以尾数精度换取吞吐。"),
        new("tf32", "TF32（计算视图）", 8, 10, 127, true, 254, 0x3FF, "TensorFloat-32：FP32 存储输入的 10 位尾数计算舍入视图，通常使用 FP32 累加。"),
        new("fp8-e4m3fn", "FP8 E4M3FN", 4, 3, 7, false, 15, 6, "FP8 E4M3 finite-only：最大有限值 448，无 Infinity，精度优先。"),
        new("fp8-e5m2", "FP8 E5M2", 5, 2, 15, true, 30, 3, "FP8 E5M2：更宽指数范围，精度低于 E4M3。")
    ];

    public override string ToString() => DisplayName;
}

namespace FloatExplorer.Core;

public sealed record FloatComparison(FloatValue A, FloatValue B, double AbsoluteError, double RelativeError, double ErrorInAUlps, double ErrorInBUlps)
{
    public static FloatComparison Create(FloatValue a, FloatValue b)
    {
        double absoluteError = ComputeAbsoluteError(a.Value, b.Value);
        double relativeError = double.IsFinite(absoluteError) && double.IsFinite(a.Value) && a.Value != 0d ? absoluteError / Math.Abs(a.Value) : absoluteError;
        return new(a, b, absoluteError, relativeError, DivideByUlp(absoluteError, a.Ulp), DivideByUlp(absoluteError, b.Ulp));
    }

    private static double ComputeAbsoluteError(double a, double b)
    {
        if (double.IsNaN(a) && double.IsNaN(b)) return 0d;
        if (a.Equals(b)) return 0d;
        return Math.Abs(a - b);
    }

    private static double DivideByUlp(double error, double ulp) => double.IsFinite(error) && double.IsFinite(ulp) && ulp > 0d ? error / ulp : double.NaN;
}
